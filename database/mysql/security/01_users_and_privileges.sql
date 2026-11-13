-- Brugere og rettigheder for rotten_potatoes (Mandatory 2: "users and privileges").
-- Køres IKKE automatisk. Kør manuelt som root (se README, afsnittet "Sikkerhed").
--
-- Compose opretter allerede app-brugeren fra .env (MYSQL_USER) med ALL PRIVILEGES på
-- rotten_potatoes.*. Det er kun til udvikling og bør strammes op her.
--
-- Brug aldrig rigtige passwords i denne fil; den ligger i Git.

USE rotten_potatoes;

-- TODO: Opret roller, fx en læse-rolle og en skrive-rolle.
-- CREATE ROLE IF NOT EXISTS ...;

-- TODO: Tildel mindst mulige rettigheder til hver rolle (GRANT SELECT/INSERT/UPDATE/DELETE/EXECUTE ...).

-- TODO: Opret brugere (fx api-bruger, migrator-bruger med kun SELECT, backup-bruger) og tildel roller.

-- TODO: Fjern unødvendige rettigheder fra app-brugeren (REVOKE ...).

-- Kontrol:
-- SHOW GRANTS FOR '<bruger>'@'%';
