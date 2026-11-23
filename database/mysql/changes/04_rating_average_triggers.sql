-- Ændring 04: Movies.AverageRating holdes opdateret, når ratings rettes eller slettes (Mandatory 2).
--
-- AI_Ratings_UpdateMovieAverage (Mandatory 1) opdaterer kun gennemsnittet ved INSERT.
-- De to triggers her genberegner det med GetAverageMovieRating efter UPDATE og DELETE.
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

-- Efter en rettet rating genberegnes både den nye og den gamle film. De er den samme film,
-- medmindre ratingen er flyttet. Serie-ratings har MovieId = NULL og matcher ingen film.
CREATE TRIGGER AU_Ratings_UpdateMovieAverage
AFTER UPDATE ON Ratings
FOR EACH ROW
    UPDATE Movies
    SET AverageRating = GetAverageMovieRating(Id)
    WHERE Id IN (OLD.MovieId, NEW.MovieId);

-- Efter en slettet rating genberegnes filmen, den hørte til (ingen, hvis det var en serie-rating).
CREATE TRIGGER AD_Ratings_UpdateMovieAverage
AFTER DELETE ON Ratings
FOR EACH ROW
    UPDATE Movies
    SET AverageRating = GetAverageMovieRating(Id)
    WHERE Id = OLD.MovieId;
