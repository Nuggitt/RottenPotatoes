# Brug fra værten:  make <target>
#
# På værten kalder Makefile selv docker compose og kører targetet i dev-containeren
# (som har .NET SDK, EF CLI og MySQL-klient). Inde i containeren (RP_DEV_CONTAINER=1,
# sat i compose.yaml) køres kommandoerne direkte. Databaserne nås via Compose-servicenavne.

.DEFAULT_GOAL := help

ifeq ($(RP_DEV_CONTAINER),1)
# ---------------------------------------------------------------------------
# Inde i dev-containeren
# ---------------------------------------------------------------------------

SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c

SOLUTION := RottenPotatoes.sln
API      := src/RottenPotatoes.Api
DATA     := src/RottenPotatoes.Data
MIGRATOR := src/RottenPotatoes.Migrator
USERS_SQL := database/mysql/security/01_users_and_privileges.sql

MYSQL_HOST ?= mysql
MYSQL_DB   := rotten_potatoes
MONGO_HOST ?= mongo
NEO4J_HOST ?= neo4j

# Antal tabeller i rotten_potatoes (tom streng hvis MySQL ikke kan nås)
# Tidsgrænser, så kommandoer melder fejl i stedet for at hænge, hvis MySQL ikke svarer.
COUNT_TABLES = MYSQL_PWD="$$MYSQL_ROOT_PASSWORD" timeout 20 mysql --connect-timeout=5 -h $(MYSQL_HOST) -u root -N -B \
	-e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$(MYSQL_DB)' AND table_type='BASE TABLE'"

.PHONY: help up down restore build clean db-status db-import db-apply db-users db-backup scaffold api migrator

help: ## Viser tilgængelige kommandoer
	@echo "Brug: make <target>"
	@echo
	@grep -E '^[a-z-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "} {printf "  %-10s %s\n", $$1, $$2}'

up: ## Starter MySQL, MongoDB og Neo4j og venter til de er klar
	@echo "'make $@' skal køres på værten, ikke inde i dev-containeren."; exit 2

down: ## Stopper og fjerner containerne (data i volumes bevares)
	@echo "'make $@' skal køres på værten, ikke inde i dev-containeren."; exit 2

restore: ## Restore NuGet dependencies
	dotnet restore $(SOLUTION)

build: ## Bygger solution
	dotnet build $(SOLUTION)

clean: ## Rydder buildfiler (.container-build/ samt bin/ og obj/)
	rm -rf .container-build
	find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

db-status: ## Kontrollerer forbindelsen til MySQL, MongoDB og Neo4j
	@ok=1; mysql_up=1; \
	if tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "MySQL:   OK  ($(MYSQL_DB): $$tables tabeller)"; \
	else echo "MySQL:   FEJL (svarer ikke på $(MYSQL_HOST):3306; kører den, eller importerer den stadig dumpet? Se: docker compose ps)"; ok=0; mysql_up=0; fi; \
	if [ $$mysql_up -eq 1 ]; then \
		for login in "$$MYSQL_USER:$$MYSQL_PASSWORD" "rp_api:$$RP_API_PASSWORD" "rp_migrator:$$RP_MIGRATOR_PASSWORD" "rp_backup:$$RP_BACKUP_PASSWORD"; do \
			user=$${login%%:*}; password=$${login#*:}; \
			if MYSQL_PWD="$$password" timeout 20 mysql --connect-timeout=5 -h $(MYSQL_HOST) -u "$$user" -N -B -e "SELECT 1" $(MYSQL_DB) >/dev/null 2>&1; then \
				echo "MySQL:   OK  (bruger $$user)"; \
			else echo "MySQL:   FEJL (bruger $$user kan ikke logge ind; mangler den, så kør: make db-users)"; ok=0; fi; \
		done; \
	fi; \
	if timeout 3 bash -c 'exec 3<>/dev/tcp/$(MONGO_HOST)/27017' 2>/dev/null; then \
		echo "MongoDB: OK  ($(MONGO_HOST):27017 svarer, login testes af migratoren)"; \
	else echo "MongoDB: FEJL ($(MONGO_HOST):27017 svarer ikke)"; ok=0; fi; \
	if curl -fsS -m 5 -u "neo4j:$$NEO4J_PASSWORD" -H 'Content-Type: application/json' \
		-d '{"statements":[{"statement":"RETURN 1"}]}' http://$(NEO4J_HOST):7474/db/neo4j/tx/commit \
		| grep -q '"errors":\[\]'; then \
		echo "Neo4j:   OK  (login + RETURN 1 på databasen neo4j)"; \
	else echo "Neo4j:   FEJL (kan ikke logge ind på $(NEO4J_HOST):7474)"; ok=0; fi; \
	if [ $$ok -eq 0 ]; then echo; echo "Start databaserne: make up"; exit 1; fi

db-import: ## Importerer MySQL-dump: make db-import DUMP=database/mysql/init/<fil>.sql [CONFIRM=yes]
	@if [ -z "$(DUMP)" ]; then \
		echo "Angiv dump-filen, fx: make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql"; exit 2; fi
	@if [ ! -f "$(DUMP)" ]; then echo "Filen findes ikke: $(DUMP)"; exit 2; fi
	@if ! tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "Kan ikke forbinde til MySQL. Start databaserne: make up"; exit 2; fi; \
	if [ "$$tables" -gt 0 ] && [ "$(CONFIRM)" != "yes" ]; then \
		echo "$(MYSQL_DB) indeholder allerede $$tables tabeller."; \
		echo "Dumpet kan droppe og genskabe tabeller (DROP TABLE IF EXISTS), så eksisterende data kan gå tabt."; \
		echo "Tag evt. backup først (se README). Bekræft eksplicit med:"; \
		echo "  make db-import DUMP=$(DUMP) CONFIRM=yes"; \
		exit 3; \
	fi; \
	echo "Importerer $(DUMP) i $(MYSQL_DB) som root ..."; \
	MYSQL_PWD="$$MYSQL_ROOT_PASSWORD" mysql -h $(MYSQL_HOST) -u root --default-character-set=utf8mb4 $(MYSQL_DB) < "$(DUMP)"; \
	echo "Import færdig: $$($(COUNT_TABLES)) tabeller i $(MYSQL_DB)."

db-apply: ## Kører et SQL-ændringsscript som root: make db-apply FILE=database/mysql/changes/<fil>.sql
	@if [ -z "$(FILE)" ]; then \
		echo "Angiv scriptet, fx: make db-apply FILE=database/mysql/changes/01_user_activity_triggers.sql"; exit 2; fi
	@if [ ! -f "$(FILE)" ]; then echo "Filen findes ikke: $(FILE)"; exit 2; fi
	@if grep -q '{{' "$(FILE)"; then \
		echo "$(FILE) indeholder {{...}}-pladsholdere. Brugere og rettigheder oprettes med: make db-users"; exit 2; fi
	@echo "Kører $(FILE) som root ..."
	@MYSQL_PWD="$$MYSQL_ROOT_PASSWORD" mysql -h $(MYSQL_HOST) -u root --default-character-set=utf8mb4 $(MYSQL_DB) < "$(FILE)"
	@echo "Færdig."

db-users: ## Opretter/opdaterer MySQL-brugerne rp_api, rp_migrator og rp_backup (passwords fra .env)
	@echo "Opretter/opdaterer brugere fra $(USERS_SQL) som root ..."
	@sed -e "s|{{RP_API_PASSWORD}}|$$RP_API_PASSWORD|g" \
		-e "s|{{RP_MIGRATOR_PASSWORD}}|$$RP_MIGRATOR_PASSWORD|g" \
		-e "s|{{RP_BACKUP_PASSWORD}}|$$RP_BACKUP_PASSWORD|g" "$(USERS_SQL)" \
		| MYSQL_PWD="$$MYSQL_ROOT_PASSWORD" mysql -h $(MYSQL_HOST) -u root
	@echo "Færdig. Kontrollér med: make db-status"

db-backup: ## Fuld backup af rotten_potatoes til backups/ (som rp_backup). Gendan: make db-import DUMP=<fil> CONFIRM=yes
	@mkdir -p backups
	@file="backups/$(MYSQL_DB)_$$(date +%Y-%m-%d_%H%M%S).sql"; \
	echo "Backup af $(MYSQL_DB) til $$file som rp_backup ..."; \
	MYSQL_PWD="$$RP_BACKUP_PASSWORD" mysqldump -h $(MYSQL_HOST) -u rp_backup \
		--single-transaction --routines --triggers --events --no-tablespaces --set-gtid-purged=OFF \
		--default-character-set=utf8mb4 --databases $(MYSQL_DB) > "$$file" \
		|| { rm -f "$$file"; echo "Backup fejlede (findes rp_backup? kør: make db-users)"; exit 1; }; \
	echo "Færdig: $$file ($$(du -h "$$file" | cut -f1))"

scaffold: ## Genererer EF Core entities og DbContext fra MySQL (Database First)
	@if ! tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "Kan ikke forbinde til MySQL. Start databaserne: make up"; exit 2; fi; \
	if [ "$$tables" -eq 0 ]; then \
		echo "Databasen $(MYSQL_DB) har ingen tabeller endnu. Importér dumpet først:"; \
		echo "  make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql"; \
		exit 2; \
	fi
	@# Data er både mål- og startup-projekt; connection string tages fra miljøvariablen
	@# (compose.yaml/.env) og skrives ikke ud her. --no-onconfiguring: den kommer heller
	@# ikke ind i den genererede kode. --msbuildprojectextensionspath: se Directory.Build.props.
	@echo "dotnet ef dbcontext scaffold \$$SCAFFOLD_CONNECTION_STRING Pomelo.EntityFrameworkCore.MySql --project $(DATA) ..."
	@dotnet ef dbcontext scaffold "$$SCAFFOLD_CONNECTION_STRING" Pomelo.EntityFrameworkCore.MySql \
		--project $(DATA) \
		--msbuildprojectextensionspath .container-build/obj/RottenPotatoes.Data \
		--context RottenPotatoesDbContext \
		--context-dir Context --context-namespace RottenPotatoes.Data.Context \
		--output-dir Entities --namespace RottenPotatoes.Data.Entities \
		--no-onconfiguring --force

api: ## Starter API'et på http://localhost:8080/swagger
	@# .NET SDK-imaget sætter ASPNETCORE_HTTP_PORTS=8080, så Kestrel lytter på alle interfaces.
	dotnet run --project $(API) --no-launch-profile

migrator: ## Kører migratoren (MySQL -> MongoDB og Neo4j). Kun én: make migrator TARGET=mongo|neo4j
	dotnet run --project $(MIGRATOR) -- $(TARGET)

else
# ---------------------------------------------------------------------------
# På værten (Windows, macOS, Linux): kun simple docker compose-kald, så
# recipes virker i både sh og cmd.exe.
# ---------------------------------------------------------------------------

# Compose læser passwords fra .env. Stop med en klar besked, hvis filen mangler.
ifeq ($(wildcard .env),)
$(error .env mangler i projektmappen. Opret den med: cp .env.example .env)
endif

# .env er KEY=VALUE og kan læses direkte af make (bruges kun til API_HOST_PORT her).
# Som i Compose vinder en miljøvariabel i shellen over værdien i .env.
SHELL_API_HOST_PORT := $(API_HOST_PORT)
include .env
API_HOST_PORT := $(or $(SHELL_API_HOST_PORT),$(API_HOST_PORT),8080)

CONTAINER_TARGETS := help restore build clean db-status db-import db-apply db-users db-backup scaffold migrator

.PHONY: up down api $(CONTAINER_TARGETS)

up:
	docker compose up -d --wait mysql mongo neo4j

# Uden -v: named volumes (databasedata) bevares.
down:
	docker compose down

# --service-ports publicerer containerens port 8080 på værtens API_HOST_PORT (.env).
# Loggen inde fra containeren viser "http://[::]:8080" – det er containerens port, ikke værtens.
api:
	@echo Swagger UI: http://localhost:$(API_HOST_PORT)/swagger - stop med Ctrl+C
	docker compose run --rm --service-ports dev make api

$(CONTAINER_TARGETS):
	docker compose run --rm dev make $@ $(if $(DUMP),DUMP=$(DUMP)) $(if $(CONFIRM),CONFIRM=$(CONFIRM)) $(if $(TARGET),TARGET=$(TARGET)) $(if $(FILE),FILE=$(FILE))

endif
