-- Ændring 03: ArchiveOldUserActivity flytter aktivitet i én transaktion (Mandatory 2).
--
-- Før kørte eventets INSERT og DELETE hver for sig (autocommit). Fejlede DELETE, lå rækkerne
-- i begge tabeller til næste kørsel. Nu kører de i én transaktion: alt eller intet flyttes.
--
-- Kør:  make db-apply FILE=database/mysql/changes/03_archive_event_transaction.sql
-- Kan køres igen uden at ændre data (eventet genoprettes med samme tidsplan).

USE rotten_potatoes;

DROP EVENT IF EXISTS ArchiveOldUserActivity;

DELIMITER $$

-- Kører én gang i døgnet og flytter aktivitet ældre end 90 dage til UserActivityArchive:
--   1. v_Cutoff beregnes én gang, så kopiering og sletning arbejder på de samme rækker.
--   2. Fejler en sætning, ruller handleren hele transaktionen tilbage og sender fejlen videre
--      til MySQL's fejllog (RESIGNAL) i stedet for at skjule den.
--   3. Gammel aktivitet kopieres til arkivet.
--   4. Kun rækker, der faktisk ligger i arkivet, slettes fra UserActivity.
--   5. COMMIT gemmer kopiering og sletning samlet.
CREATE EVENT ArchiveOldUserActivity
ON SCHEDULE EVERY 1 DAY STARTS '2026-10-09 01:00:00'
ON COMPLETION PRESERVE
ENABLE
DO
BEGIN
    DECLARE v_Cutoff DATETIME DEFAULT CURRENT_TIMESTAMP - INTERVAL 90 DAY;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

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
