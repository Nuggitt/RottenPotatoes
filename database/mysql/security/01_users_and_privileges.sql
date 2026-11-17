-- Brugere og rettigheder for rotten_potatoes (Mandatory 2: "users and privileges").
--
-- Princip: mindst mulige rettigheder. Hvert program får sin egen MySQL-bruger med kun
-- de rettigheder, det har brug for. Bliver API'et kompromitteret (fx via SQL injection),
-- kan angriberen ikke slette tabeller eller ændre skemaet.
--
-- Kør:  make db-users
--   Make erstatter {{...}}-pladsholderne med passwords fra .env og kører scriptet som root.
--   Passwords står derfor aldrig i denne fil (den ligger i Git).
--
-- Kan køres igen: brugerne oprettes kun, hvis de mangler; password og rettigheder sættes
-- forfra hver gang (REVOKE + GRANT), så resultatet altid svarer præcis til denne fil.
--
-- '%' som host: programmerne forbinder fra andre containere i Docker-netværket.
-- MySQL-porten er kun bundet til 127.0.0.1 på værten (compose.yaml).
--
-- Brugere, der ikke oprettes her:
--   root    Administration: import, ændringsscripts, oprettelse af brugere.
--   rp_app  Oprettes af Docker (MYSQL_USER i .env) med alle rettigheder på databasen.
--           Bruges kun til udvikling (make scaffold), ikke af API eller migrator.


-- 1. API'et: læse og skrive data, kalde procedurer og functions.
--    Ingen CREATE, ALTER, DROP, INDEX, TRIGGER eller GRANT.
CREATE USER IF NOT EXISTS 'rp_api'@'%' IDENTIFIED BY '{{RP_API_PASSWORD}}';
ALTER USER 'rp_api'@'%' IDENTIFIED BY '{{RP_API_PASSWORD}}';
REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'rp_api'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON rotten_potatoes.* TO 'rp_api'@'%';


-- 2. Migratoren: læser alt fra MySQL og skriver kun til MongoDB og Neo4j.
CREATE USER IF NOT EXISTS 'rp_migrator'@'%' IDENTIFIED BY '{{RP_MIGRATOR_PASSWORD}}';
ALTER USER 'rp_migrator'@'%' IDENTIFIED BY '{{RP_MIGRATOR_PASSWORD}}';
REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'rp_migrator'@'%';
GRANT SELECT ON rotten_potatoes.* TO 'rp_migrator'@'%';


-- 3. Backup: det, mysqldump kræver for at dumpe tabeller, views, triggers, events
--    og procedures/functions (make db-backup). Kan ikke ændre data.
--    SHOW_ROUTINE er en global rettighed, der kun giver lov til at LÆSE definitionen
--    af procedures/functions (de er oprettet af root og ellers skjult for andre).
CREATE USER IF NOT EXISTS 'rp_backup'@'%' IDENTIFIED BY '{{RP_BACKUP_PASSWORD}}';
ALTER USER 'rp_backup'@'%' IDENTIFIED BY '{{RP_BACKUP_PASSWORD}}';
REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'rp_backup'@'%';
GRANT SELECT, SHOW VIEW, TRIGGER, EVENT, LOCK TABLES ON rotten_potatoes.* TO 'rp_backup'@'%';
GRANT SHOW_ROUTINE ON *.* TO 'rp_backup'@'%';


-- Kontrol (kør som root):
--   SHOW GRANTS FOR 'rp_api'@'%';
--   SHOW GRANTS FOR 'rp_migrator'@'%';
--   SHOW GRANTS FOR 'rp_backup'@'%';
