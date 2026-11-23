-- Ændring 03: ArchiveOldUserActivity flytter aktivitet i én transaktion (Mandatory 2).
--
-- Eventet flytter aktivitet ældre end 90 dage fra UserActivity til UserActivityArchive
-- med to sætninger: INSERT i arkivet og DELETE fra UserActivity. Før denne ændring kørte
-- de hver for sig (autocommit). Fejlede DELETE, lå rækkerne i begge tabeller til næste kørsel.
-- Nu kører begge i én transaktion: enten flyttes alle rækkerne, eller ingen af dem.
--
-- Skæringstidspunktet beregnes én gang (v_Cutoff), så INSERT og DELETE arbejder på
-- præcis de samme rækker. Før beregnede hver sætning sit eget CURRENT_TIMESTAMP.
--
-- Kør:  make db-apply FILE=database/mysql/changes/03_archive_event_transaction.sql
-- Kan køres igen uden at ændre data (eventet genoprettes med samme tidsplan).

USE rotten_potatoes;

DROP EVENT IF EXISTS ArchiveOldUserActivity;

DELIMITER $$

CREATE EVENT ArchiveOldUserActivity
ON SCHEDULE EVERY 1 DAY STARTS '2026-10-09 01:00:00'
ON COMPLETION PRESERVE
ENABLE
DO
BEGIN
    DECLARE v_Cutoff DATETIME DEFAULT CURRENT_TIMESTAMP - INTERVAL 90 DAY;

    -- Ved enhver fejl: rul hele flytningen tilbage og send fejlen videre
    -- (så den står i MySQL's fejllog i stedet for at blive skjult).
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- Kopiér gammel aktivitet til arkivet
    INSERT IGNORE INTO UserActivityArchive (
        Id,
        UserId,
        ActivityType,
        ActivityAt,
        ArchivedAt
    )
    SELECT
        Id,
        UserId,
        ActivityType,
        ActivityAt,
        CURRENT_TIMESTAMP
    FROM UserActivity
    WHERE ActivityAt < v_Cutoff;

    -- Slet kun rækker, der faktisk ligger i arkivet
    DELETE ua
    FROM UserActivity AS ua
    WHERE ua.ActivityAt < v_Cutoff
      AND EXISTS (
          SELECT 1
          FROM UserActivityArchive AS archive
          WHERE archive.Id = ua.Id
      );

    COMMIT;
END$$

DELIMITER ;
