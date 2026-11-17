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
| Værktøjer | Docker Compose V2, GNU Make |

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

Alle kommandoer er ens på Windows 11 (PowerShell), Linux og macOS: `make <target>`.
Lokalt skal kun **Docker**, **Git** og **GNU Make** være installeret.
Makefile kalder selv `docker compose` og kører arbejdet i en dev-container, som har .NET SDK, EF CLI og MySQL-klient.
I skal ikke installere .NET lokalt.

## 1. Installér Docker og Make

| OS | Docker | GNU Make |
|---|---|---|
| Windows 11 | [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) med WSL 2-backend | `winget install ezwinports.make`, og genstart derefter terminalen |
| macOS (Apple Silicon og Intel) | [Docker Desktop](https://docs.docker.com/desktop/setup/install/mac-install/) | `xcode-select --install` |
| Aurora Linux (Fedora Atomic) | Forinstalleret i Aurora-DX, ellers [Docker Engine til Fedora](https://docs.docker.com/engine/install/fedora/). Tilføj dig selv til `docker`-gruppen | Forinstalleret |

Tjek at begge dele virker:

```bash
docker compose version
make --version
```

## 2. Klon projektet

Repoet er privat. Du skal være tilføjet som collaborator og logget ind på GitHub.

```bash
git clone https://github.com/Nuggitt/RottenPotatoes.git
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
make up
make db-status
```

| Service | Fra værten | Fra containere |
|---|---|---|
| MySQL | `127.0.0.1:3306` | `mysql:3306` |
| MongoDB | `127.0.0.1:27017` | `mongo:27017` |
| Neo4j Browser | http://localhost:7474 (bruger `neo4j`) | `neo4j:7474` |
| Neo4j Bolt | `bolt://localhost:7687` | `bolt://neo4j:7687` |

Første kørsel bygger dev-imaget. Efter ændringer i `Dockerfile.dev`: `docker compose build dev`.

Bruger I også en editor med .NET på jeres maskine (VS Code, Rider, Visual Studio), deler den ikke build-filer med dev-containeren. Editoren bruger `bin/` og `obj/`, mens containeren bygger til `.container-build/`. Det styres af `Directory.Build.props`. Får editoren fejlen `NETSDK1064`, så kør `make clean` og genindlæs projektet.

## 5. Importér MySQL-data

Kopiér jeres dump fra Mandatory 1 til `database/mysql/init/rotten_potatoes_full_dump.sql`.
Dumpet er testet på MySQL 8.4: 22 tabeller, 2 views, 2 functions, 4 procedures, 1 trigger og 1 event.

**Ny database (anbefalet):** Når volumen er tom, importerer MySQL automatisk `*.sql` fra `database/mysql/init/` ved første `make up` (ca. 30 sekunder). `make up` venter, til importen er færdig.

**Eksisterende database:** Den automatiske import kører ikke igen. Importér manuelt:

```bash
make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql
```

Hvis databasen allerede har tabeller, stopper kommandoen. Dumpet indeholder `DROP TABLE IF EXISTS` og overskriver derfor eksisterende tabeller. Tag backup (afsnit 13) og bekræft eksplicit:

```bash
make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql CONFIRM=yes
```

### Databaseændringer fra Mandatory 2

Ændringer til databasen efter dumpet ligger som scripts i `database/mysql/changes/`. Kør dem i nummerorden efter hver import eller nulstilling. De kan køres igen uden at ændre data:

```bash
make db-apply FILE=database/mysql/changes/01_user_activity_triggers.sql
```

| Script | Ændring |
|---|---|
| `01_user_activity_triggers.sql` | Triggers, der logger hver ny rating og hvert nyt review i `UserActivity` (`Rating`, `Review`) |

## 6. Byg solution

```bash
make restore
make build
make help
```

## 7. EF Core scaffold (Database First)

Kræver at databaserne kører, og at dumpet er importeret.

```bash
make scaffold
```

Kommandoen genererer entities i `src/RottenPotatoes.Data/Entities/` og `RottenPotatoesDbContext` i `src/RottenPotatoes.Data/Context/`.
Connection string tages fra miljøvariablen `ConnectionStrings__RottenPotatoes`, som `compose.yaml` sætter ud fra `.env`.
Med `--no-onconfiguring` skrives den aldrig ind i den genererede kode, så der ikke kommer passwords i Git.

Scaffold kører med `--force` og overskriver de genererede filer. Læg egen kode i separate `partial`-klasser.
Vi bruger ikke EF Core migrations, fordi databasen allerede findes.

## 8. Start API'et

```bash
make api
```

`make api` skriver adressen først: `Swagger UI: http://localhost:8080/swagger`. Stop med `Ctrl+C`.

Loggen viser bagefter `Now listening on: http://[::]:8080`. Det er porten inde i containeren, ikke den, du skal bruge i browseren.
Porten på din maskine ændres med `API_HOST_PORT` i `.env`, fx `API_HOST_PORT=5000` giver http://localhost:5000/swagger.

## 9. Kør migratoren

```bash
make migrator
```

| Kommando | Gør |
|---|---|
| `make migrator TARGET=mongo` | MySQL → MongoDB (ca. 30 sekunder) |
| `make migrator TARGET=neo4j` | MySQL → Neo4j (ca. 2 minutter) |
| `make migrator` | Begge, først MongoDB |

Migratoren sletter eller genopretter aldrig data. Har MongoDB eller Neo4j allerede data fra en tidligere migrering, stopper den, før der skrives noget.
Vil du migrere igen, skal du selv nulstille databasen først. Det sletter **alle** data i den:

```bash
# MongoDB
docker compose exec mongo sh -c 'mongosh --quiet -u "$MONGO_INITDB_ROOT_USERNAME" -p "$MONGO_INITDB_ROOT_PASSWORD" --authenticationDatabase admin RottenPotatoes --eval "db.dropDatabase()"'

# Neo4j (fjerner kun Neo4j-containeren og dens data-volume, derefter startes den tom igen)
docker compose rm -sf neo4j
docker volume rm rotten-potatoes_neo4j-data
make up
```

Mapping og designbeslutninger: `docs/database-mapping.md`. Se resultatet:

- **MongoDB Compass:** `mongodb://rp_admin:skift_mig_mongo@localhost:27017/?authSource=admin`, databasen `RottenPotatoes`
- **Neo4j Browser:** http://localhost:7474 (bruger `neo4j`, password `NEO4J_PASSWORD` fra `.env`). Eksempel-queries ligger i `database/neo4j/design/example-queries.cypher`

## 10. Stop containerne

```bash
make down
```

`make down` fjerner containere og netværk. Data i volumes bevares.

## 11. Undgå at slette data

Data ligger i named volumes: `rotten-potatoes_mysql-data`, `rotten-potatoes_mongo-data` og `rotten-potatoes_neo4j-data`.

| Kommando | Data |
|---|---|
| `make down` / `docker compose stop` / `docker compose down` | bevares |
| `docker compose down -v` | **SLETTES permanent** (alle databaser) |
| `docker volume rm` / `docker volume prune` | **SLETTES** |

Der er bevidst ingen make-kommando, som sletter volumes. Brug kun `docker compose down -v`, hvis du vil starte forfra. MySQL importerer så dumpet igen ved næste `make up`.

## 12. Sikkerhed

- Passwords ligger kun i `.env` og når .NET via miljøvariabler (`ConnectionStrings__RottenPotatoes` osv.). `appsettings.json` indeholder ingen hemmeligheder.
- Databaseportene bindes kun til `127.0.0.1`.
- SQL injection: brug EF Core LINQ eller `FromSql`/`ExecuteSql` med interpolerede parametre. Byg aldrig SQL med strengsammensætning. I Neo4j bruges Cypher-parametre (`$id`).
- Brugere og rettigheder skrives i `database/mysql/security/01_users_and_privileges.sql` og køres som root:

```bash
make db-apply FILE=database/mysql/security/01_users_and_privileges.sql
```

## 13. Backup af MySQL

Fuld backup inklusive routines, triggers og events til `backups/` (git-ignoreret):

```bash
docker compose run --rm dev sh -c 'mkdir -p backups && MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysqldump -h mysql -u root --single-transaction --routines --triggers --events --set-gtid-purged=OFF --databases rotten_potatoes > backups/rotten_potatoes_$(date +%F).sql'
```

Gendan fra en backup:

```bash
make db-import DUMP=backups/rotten_potatoes_<dato>.sql CONFIRM=yes
```

## Fælles kommandoer

| Kommando | Funktion |
|---|---|
| `make up` | Start MySQL, MongoDB og Neo4j |
| `make down` | Stop containerne (data bevares) |
| `make help` | Vis alle targets |
| `make restore` / `make build` / `make clean` | Restore, byg, ryd op |
| `make db-status` | Tjek databaseforbindelser |
| `make db-import DUMP=<fil> [CONFIRM=yes]` | Importér MySQL-dump |
| `make scaffold` | Generér EF Core entities |
| `make api` | Start API'et (http://localhost:8080/swagger) |
| `make migrator` | Kør migratoren |

`docker compose run --rm dev make <target>` virker også og giver samme resultat.
