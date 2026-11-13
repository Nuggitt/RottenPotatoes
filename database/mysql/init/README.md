# MySQL-dump

Læg jeres dump her, fx `rotten_potatoes_full_dump.sql` fra Mandatory 1.
`*.sql` i denne mappe er git-ignoreret (dumpet er ca. 96 MB).

- **Ny database:** MySQL-containeren importerer automatisk alle `*.sql` herfra, men kun første gang, når volumen `rotten-potatoes_mysql-data` er tom.
- **Eksisterende database:** `docker compose run --rm dev make db-import DUMP=database/mysql/init/rotten_potatoes_full_dump.sql`

Læg kun ét fuldt dump her. De opdelte scripts (`01_schema.sql` … `07_events.sql`) indeholder de samme objekter.
