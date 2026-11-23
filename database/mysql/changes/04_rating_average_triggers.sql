-- Ændring 04: Movies.AverageRating holdes opdateret, når ratings rettes eller slettes (Mandatory 2).
--
-- AI_Ratings_UpdateMovieAverage (Mandatory 1) opdaterer kun gennemsnittet ved INSERT.
-- Blev en rating rettet eller slettet, passede Movies.AverageRating ikke længere.
-- To nye triggers genberegner gennemsnittet med GetAverageMovieRating:
--   AU_Ratings_UpdateMovieAverage  efter UPDATE (ny score, eller ratingen flyttet til en anden film)
--   AD_Ratings_UpdateMovieAverage  efter DELETE
--
-- Begrænsning: MySQL kører ikke triggers ved ON DELETE CASCADE. Slettes en bruger, slettes
-- brugerens ratings via FK_Ratings_Users uden at AD-triggeren kører, så gennemsnittet for de
-- film, brugeren havde ratet, bliver ikke genberegnet.
--
-- Kør:  make db-apply FILE=database/mysql/changes/04_rating_average_triggers.sql
-- Kan køres igen uden at ændre data (triggerne genoprettes, ingen rækker ændres).

USE rotten_potatoes;

DROP TRIGGER IF EXISTS AU_Ratings_UpdateMovieAverage;
DROP TRIGGER IF EXISTS AD_Ratings_UpdateMovieAverage;

DELIMITER $$

CREATE TRIGGER AU_Ratings_UpdateMovieAverage
AFTER UPDATE ON Ratings
FOR EACH ROW
BEGIN
    -- Kun hvis score eller film er ændret (fx ikke ved en ændring af UpdatedAt alene).
    -- <=> sammenligner også NULL korrekt (serie-ratings har MovieId = NULL).
    IF NOT (OLD.Score <=> NEW.Score AND OLD.MovieId <=> NEW.MovieId) THEN

        IF NEW.MovieId IS NOT NULL THEN
            UPDATE Movies
            SET AverageRating = GetAverageMovieRating(NEW.MovieId)
            WHERE Id = NEW.MovieId;
        END IF;

        -- Ratingen er flyttet fra en anden film: den film skal også genberegnes.
        IF OLD.MovieId IS NOT NULL AND NOT (OLD.MovieId <=> NEW.MovieId) THEN
            UPDATE Movies
            SET AverageRating = GetAverageMovieRating(OLD.MovieId)
            WHERE Id = OLD.MovieId;
        END IF;

    END IF;
END$$

CREATE TRIGGER AD_Ratings_UpdateMovieAverage
AFTER DELETE ON Ratings
FOR EACH ROW
BEGIN
    IF OLD.MovieId IS NOT NULL THEN
        UPDATE Movies
        SET AverageRating = GetAverageMovieRating(OLD.MovieId)
        WHERE Id = OLD.MovieId;
    END IF;
END$$

DELIMITER ;
