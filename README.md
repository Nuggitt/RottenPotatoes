# Rotten Potatoes

Rotten Potatoes er en webapplikation til film og serier, hvor brugere kan give ratings og skrive anmeldelser.
Projektet laves i faget *Databases for Developers* på EK:

- **Mandatory 1:** relationel MySQL-database med testdata.
- **Mandatory 2:** ASP.NET Core Web API med EF Core (Database First), samt en migrator fra MySQL til MongoDB og Neo4j.

| Del | Teknologi |
|---|---|
| API | ASP.NET Core Web API (.NET 10), Swagger UI |
| ORM | EF Core 9.0.23 + Pomelo.EntityFrameworkCore.MySql 9.0.0 |
| Migrator | .NET 10 console app, MongoDB.Driver 3.14.0, Neo4j.Driver 6.5.1 |
| Databaser | MySQL 8.4 LTS, MongoDB 8.0, Neo4j 2026.10 Community |
| Værktøjer | Docker Compose V2, GNU Make (kører i dev-containeren) |

```text
src/RottenPotatoes.Api        Web API (Controllers, DTOs, Services, Interfaces)
src/RottenPotatoes.Data       EF Core DbContext + scaffoldede entities
src/RottenPotatoes.Migrator   Migrator (MySQL -> MongoDB og Neo4j)
database/mysql/init           MySQL-dump (importeres ved første start)
database/mysql/security       SQL-scripts til brugere og rettigheder
database/mongodb/design       Eksisterende MongoDB-design
database/neo4j/design         Eksisterende Neo4j-design
docs/database-mapping.md      Mapping MySQL -> MongoDB/Neo4j
```

Alle kommandoer nedenfor er ens på Windows 11 (PowerShell), Linux og macOS.
Kun **Docker** og **Git** skal være installeret. .NET SDK, EF CLI, MySQL-klient og Make ligger i dev-containeren.

## 1. Installér Docker

- **Windows 11:** Installér [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) med WSL 2-backend. Klon gerne projektet ind i WSL-filsystemet, så builds er hurtigere.
- **macOS (Apple Silicon og Intel):** Installér [Docker Desktop](https://docs.docker.com/desktop/setup/install/mac-install/). Alle images findes i både arm64- og amd64-udgaver.
- **Aurora Linux (Fedora Atomic):** Docker er forinstalleret i Aurora-DX. Ellers følg [Docker Engine til Fedora](https://docs.docker.com/engine/install/fedora/). Tilføj dig selv til `docker`-gruppen.

Tjek at Compose V2 virker:

```bash
docker compose version
```

## 2. Klon projektet

```bash
git clone <repo-url> RottenPotatoes
cd RottenPotatoes
```

`.gitattributes` sørger for LF-linjeskift, også på Windows. Det er nødvendigt, fordi Makefile og scripts køres i Linux-containere.

## 3. Opret `.env`

```bash
cp .env.example .env
```

Skift passwords i `.env`. Filen er git-ignoreret og må aldrig committes.

- **Linux:** sæt `DEV_UID`/`DEV_GID` til outputtet af `id -u` og `id -g`, så filer i projektmappen ejes af dig. Med rootless Docker eller Podman sættes begge til `0`.
- **Porte:** databaserne bindes kun til `127.0.0.1`. Er en port optaget, fx af en anden Neo4j-container på 7474/7687, så skift den i `.env`.

## 4. Start databaserne

```bash
docker compose up -d --wait mysql mongo neo4j
docker compose run --rm dev make db-status
```

| Service | Fra værten | Fra containere |
|---|---|---|
| MySQL | `127.0.0.1:3306` | `mysql:3306` |
| MongoDB | `127.0.0.1:27017` | `mongo:27017` |
| Neo4j Browser | http://localhost:7474 (bruger `neo4j`) | `neo4j:7474` |
| Neo4j Bolt | `bolt://localhost:7687` | `bolt://neo4j:7687` |

Første `docker compose run` bygger dev-imaget. Efter ændringer i `Dockerfile.dev`: `docker compose build dev`.

## 5. Importér MySQL-data

Kopiér jeres dump fra Mandatory 1 til `database/mysql/init/rotten_potatoes_full_dump.sql`.
Dumpet er testet på MySQL 8.4: 22 tabeller, 2 views, 2 functions, 4 procedures, 1 trigger og 1 event.

**Ny database (anbefalet):** Når volumen er tom, importerer MySQL automatisk `*.sql` fra `database/mysql/init/` ved første start (ca. 30 sekunder). `--wait` venter, til importen er færdig.

**Eksisterende database:** Den automatiske import kører ikke igen. Importér manuelt:

```bash
docker compose run --rm dev make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql
```

Hvis databasen allerede har tabeller, stopper kommandoen. Dumpet indeholder `DROP TABLE IF EXISTS` og overskriver derfor eksisterende tabeller. Tag backup (afsnit 13) og bekræft eksplicit:

```bash
docker compose run --rm dev make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql CONFIRM=yes
```

## 6. Byg solution

```bash
docker compose run --rm dev make restore
docker compose run --rm dev make build
docker compose run --rm dev make help
```

## 7. EF Core scaffold (Database First)

Kræver at MySQL kører og at dumpet er importeret.

```bash
docker compose run --rm dev make scaffold
```

Kommandoen genererer entities i `src/RottenPotatoes.Data/Entities/` og `RottenPotatoesDbContext` i `src/RottenPotatoes.Data/Context/`.
Den bruger `Name=ConnectionStrings:RottenPotatoes` og `--no-onconfiguring`, så der aldrig skrives passwords ind i den genererede kode.
Connection string sættes af `compose.yaml` ud fra `.env`.

Scaffold kører med `--force` og overskriver de genererede filer. Læg egen kode i separate `partial`-klasser.
Vi bruger ikke EF Core migrations, fordi databasen allerede findes.

## 8. Start API'et

```bash
docker compose run --rm --service-ports dev make api
```

`--service-ports` er nødvendig, ellers publiceres port 8080 ikke.
Swagger UI: http://localhost:8080/swagger. Stop med `Ctrl+C`.

## 9. Kør migratoren

```bash
docker compose run --rm dev make migrator
```

Migratoren er endnu ikke implementeret. Den skriver det og afslutter med fejlkode 2.
Den sletter eller genopretter aldrig eksisterende MongoDB- eller Neo4j-data.
Designet, der skal implementeres, står i `docs/database-mapping.md`.

## 10. Stop containerne

```bash
docker compose stop      # stopper containerne
docker compose down      # fjerner containere og netværk; data bevares
```

## 11. Undgå at slette data

Data ligger i named volumes: `rotten-potatoes_mysql-data`, `rotten-potatoes_mongo-data` og `rotten-potatoes_neo4j-data`.

| Kommando | Data |
|---|---|
| `docker compose stop` / `down` | bevares |
| `docker compose down -v` | **SLETTES permanent** (alle databaser) |
| `docker volume rm` / `docker volume prune` | **SLETTES** |

Brug kun `down -v`, hvis du bevidst vil starte forfra. MySQL importerer så dumpet igen ved næste start.

## 12. Sikkerhed

- Passwords ligger kun i `.env` og når .NET via miljøvariabler (`ConnectionStrings__RottenPotatoes` osv.). `appsettings.json` indeholder ingen hemmeligheder.
- Databaseportene bindes kun til `127.0.0.1`.
- SQL injection: brug EF Core LINQ eller `FromSql`/`ExecuteSql` med interpolerede parametre. Byg aldrig SQL med strengsammensætning. I Neo4j bruges Cypher-parametre (`$id`).
- Brugere og rettigheder skrives i `database/mysql/security/01_users_and_privileges.sql` og køres manuelt som root:

```bash
docker compose run --rm dev sh -c 'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql -h mysql -u root < database/mysql/security/01_users_and_privileges.sql'
```

## 13. Backup af MySQL

Fuld backup inklusive routines, triggers og events til `backups/` (git-ignoreret):

```bash
docker compose run --rm dev sh -c 'mkdir -p backups && MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysqldump -h mysql -u root --single-transaction --routines --triggers --events --set-gtid-purged=OFF --databases rotten_potatoes > backups/rotten_potatoes_$(date +%F).sql'
```

Gendan fra en backup:

```bash
docker compose run --rm dev make db-import DUMP=backups/rotten_potatoes_<dato>.sql CONFIRM=yes
```

## Fælles kommandoer

| Kommando | Funktion |
|---|---|
| `docker compose up -d --wait mysql mongo neo4j` | Start databaser |
| `docker compose run --rm dev make help` | Vis Make-targets |
| `docker compose run --rm dev make restore` / `build` / `clean` | Restore, byg, ryd op |
| `docker compose run --rm dev make db-status` | Tjek databaseforbindelser |
| `docker compose run --rm dev make db-import DUMP=<fil> [CONFIRM=yes]` | Importér MySQL-dump |
| `docker compose run --rm dev make scaffold` | Generér EF Core entities |
| `docker compose run --rm --service-ports dev make api` | Start API'et |
| `docker compose run --rm dev make migrator` | Kør migratoren |
| `docker compose down` | Stop (data bevares) |
