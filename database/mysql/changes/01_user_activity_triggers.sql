-- Ændring 01: Log ratings og reviews i UserActivity (Mandatory 2).
--
-- Hver ny rating og hvert nyt review registreres i UserActivity, uanset om det
-- indsættes via API'et, proceduren AddMovieRating/AddSeriesRating eller direkte i SQL.
-- Login og Logout logges af backend (AuthService), fordi kun backend ved, når de sker.
--
-- ActivityType-værdier: 'Login', 'Logout', 'Rating', 'Review'.
-- Aktivitet ældre end 90 dage arkiveres af det eksisterende event ArchiveOldUserActivity.
--
-- Kør:  make db-apply FILE=database/mysql/changes/01_user_activity_triggers.sql
-- Kan køres igen uden at ændre data (triggerne genoprettes, ingen rækker slettes).

USE rotten_potatoes;

DROP TRIGGER IF EXISTS AI_Ratings_LogUserActivity;
DROP TRIGGER IF EXISTS AI_Reviews_LogUserActivity;

-- Ratings har selv UserId. ActivityAt = tidspunktet for ratingen.
CREATE TRIGGER AI_Ratings_LogUserActivity
AFTER INSERT ON Ratings
FOR EACH ROW
    INSERT INTO UserActivity (UserId, ActivityType, ActivityAt)
    VALUES (NEW.UserId, 'Rating', NEW.CreatedAt);

-- Reviews har ikke UserId; brugeren findes via den rating, reviewet hører til.
CREATE TRIGGER AI_Reviews_LogUserActivity
AFTER INSERT ON Reviews
FOR EACH ROW
    INSERT INTO UserActivity (UserId, ActivityType, ActivityAt)
    SELECT r.UserId, 'Review', NEW.CreatedAt
    FROM Ratings AS r
    WHERE r.Id = NEW.RatingId;
