# Køres inde i dev-containeren:  docker compose run --rm dev make <target>
# Kræver ikke Docker CLI i containeren; databaserne nås via Compose-servicenavne.

SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := help

SOLUTION := RottenPotatoes.sln
API      := src/RottenPotatoes.Api
DATA     := src/RottenPotatoes.Data
MIGRATOR := src/RottenPotatoes.Migrator

MYSQL_HOST ?= mysql
MYSQL_DB   := rotten_potatoes
MONGO_HOST ?= mongo
NEO4J_HOST ?= neo4j

# Antal tabeller i rotten_potatoes (tom streng hvis MySQL ikke kan nås)
COUNT_TABLES = MYSQL_PWD="$$MYSQL_ROOT_PASSWORD" mysql -h $(MYSQL_HOST) -u root -N -B \
	-e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$(MYSQL_DB)' AND table_type='BASE TABLE'"

.PHONY: help restore build clean db-status db-import scaffold api migrator

help: ## Viser tilgængelige kommandoer
	@echo "Brug: docker compose run --rm dev make <target>"
	@echo
	@grep -E '^[a-z-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "} {printf "  %-10s %s\n", $$1, $$2}'

restore: ## Restore NuGet dependencies
	dotnet restore $(SOLUTION)

build: ## Bygger solution
	dotnet build $(SOLUTION)

clean: ## Rydder buildfiler (bin/ og obj/)
	dotnet clean $(SOLUTION) --nologo -v quiet || true
	find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

db-status: ## Kontrollerer forbindelsen til MySQL, MongoDB og Neo4j
	@ok=1; \
	if tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "MySQL:   OK  ($(MYSQL_DB): $$tables tabeller)"; \
	else echo "MySQL:   FEJL (kan ikke logge ind på $(MYSQL_HOST):3306)"; ok=0; fi; \
	if MYSQL_PWD="$$MYSQL_PASSWORD" mysql -h $(MYSQL_HOST) -u "$$MYSQL_USER" -N -B -e "SELECT 1" $(MYSQL_DB) >/dev/null 2>&1; then \
		echo "MySQL:   OK  (app-bruger $$MYSQL_USER)"; \
	else echo "MySQL:   FEJL (app-bruger $$MYSQL_USER kan ikke logge ind)"; ok=0; fi; \
	if timeout 3 bash -c 'exec 3<>/dev/tcp/$(MONGO_HOST)/27017' 2>/dev/null; then \
		echo "MongoDB: OK  ($(MONGO_HOST):27017 svarer, login testes af migratoren)"; \
	else echo "MongoDB: FEJL ($(MONGO_HOST):27017 svarer ikke)"; ok=0; fi; \
	if curl -fsS -m 5 -u "neo4j:$$NEO4J_PASSWORD" -H 'Content-Type: application/json' \
		-d '{"statements":[{"statement":"RETURN 1"}]}' http://$(NEO4J_HOST):7474/db/neo4j/tx/commit \
		| grep -q '"errors":\[\]'; then \
		echo "Neo4j:   OK  (login + RETURN 1 på databasen neo4j)"; \
	else echo "Neo4j:   FEJL (kan ikke logge ind på $(NEO4J_HOST):7474)"; ok=0; fi; \
	if [ $$ok -eq 0 ]; then echo; echo "Start databaserne: docker compose up -d --wait mysql mongo neo4j"; exit 1; fi

db-import: ## Importerer MySQL-dump: make db-import DUMP=database/mysql/init/<fil>.sql [CONFIRM=yes]
	@if [ -z "$(DUMP)" ]; then \
		echo "Angiv dump-filen, fx: make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql"; exit 2; fi
	@if [ ! -f "$(DUMP)" ]; then echo "Filen findes ikke: $(DUMP)"; exit 2; fi
	@if ! tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "Kan ikke forbinde til MySQL. Start den: docker compose up -d --wait mysql"; exit 2; fi; \
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

scaffold: ## Genererer EF Core entities og DbContext fra MySQL (Database First)
	@if ! tables=$$($(COUNT_TABLES) 2>/dev/null); then \
		echo "Kan ikke forbinde til MySQL. Start den: docker compose up -d --wait mysql"; exit 2; fi; \
	if [ "$$tables" -eq 0 ]; then \
		echo "Databasen $(MYSQL_DB) har ingen tabeller endnu. Importér dumpet først:"; \
		echo "  docker compose run --rm dev make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql"; \
		exit 2; \
	fi
	dotnet ef dbcontext scaffold "Name=ConnectionStrings:RottenPotatoes" Pomelo.EntityFrameworkCore.MySql \
		--project $(DATA) --startup-project $(API) \
		--context RottenPotatoesDbContext \
		--context-dir Context --context-namespace RottenPotatoes.Data.Context \
		--output-dir Entities --namespace RottenPotatoes.Data.Entities \
		--no-onconfiguring --force

api: ## Starter API'et (kræver --service-ports, se README)
	@# .NET SDK-imaget sætter ASPNETCORE_HTTP_PORTS=8080, så Kestrel lytter på alle interfaces.
	dotnet run --project $(API) --no-launch-profile

migrator: ## Kører migrator-programmet (MySQL -> MongoDB og Neo4j)
	dotnet run --project $(MIGRATOR)
