-- Ændring 02: Roller til login og adgangskontrol (Mandatory 2).
--
-- Roller er normaliseret i en opslagstabel (som Genres, Awards og StreamingServices).
-- Hver bruger har præcis én rolle: Users.RoleId -> Roles.Id (mange-til-én).
-- Backend skriver rollens navn ind i JWT'en ved login, og [Authorize(Roles = "Admin")]
-- afgør adgangen til API'et.
--
--   1 = User   (standard; alle eksisterende og nye brugere)
--   2 = Admin  (må oprette, rette og slette film og serier)
--
-- Kør:  make db-apply FILE=database/mysql/changes/02_roles.sql
-- Kan køres igen uden at ændre data: tabel, kolonne og admin oprettes kun, hvis de mangler.

USE rotten_potatoes;

CREATE TABLE IF NOT EXISTS Roles (
  Id   tinyint unsigned NOT NULL,
  Name varchar(20) NOT NULL,
  PRIMARY KEY (Id),
  UNIQUE KEY UQ_Roles_Name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO Roles (Id, Name) VALUES (1, 'User'), (2, 'Admin');

-- Users.RoleId. MySQL har ikke "ADD COLUMN IF NOT EXISTS", så kolonnen tilføjes kun,
-- hvis den mangler (opslag i information_schema + faste SQL-strenge, intet brugerinput).
-- DEFAULT 1 giver alle eksisterende brugere rollen User.
-- ON DELETE RESTRICT (standard): en rolle kan ikke slettes, så længe brugere har den.
SET @has_role_column := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = 'rotten_potatoes' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'RoleId'
);
SET @add_role_column := IF(@has_role_column = 0,
  'ALTER TABLE Users
     ADD COLUMN RoleId tinyint unsigned NOT NULL DEFAULT 1 AFTER PasswordHash,
     ADD KEY IX_Users_RoleId (RoleId),
     ADD CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles (Id)',
  'DO 0');
PREPARE stmt FROM @add_role_column;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Admin-bruger til udvikling og test.
--   Brugernavn: admin
--   Password:   RottenAdmin123
-- Hashet er lavet med ASP.NET Core's PasswordHasher (PBKDF2), samme algoritme som API'et
-- bruger ved login. Passwordet er fælles for alle udviklere og står i Git (som hash):
-- skift det, før systemet kommer i drift.
-- Oprettes kun, hvis brugernavnet ikke findes; en eksisterende admin overskrives aldrig.
INSERT INTO Users (Username, Email, PasswordHash, RoleId)
SELECT 'admin', 'admin@rottenpotatoes.local',
       'AQAAAAIAAYagAAAAEFeebBRwl76U5W26Oz2qDmhywZc0VYe7xdZ9NAfxb/nUQ5jfpceUF6Z8CJDyNY/YbQ==', 2
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin');
