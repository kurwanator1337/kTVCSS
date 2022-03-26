-- --------------------------------------------------------
-- Хост:                         db.ktvcss.ru
-- Версия сервера:               Microsoft SQL Server 2019 (RTM) - 15.0.2000.5
-- Операционная система:         Windows Server 2019 Standard 10.0 <X64> (Build 17763: ) (Hypervisor)
-- HeidiSQL Версия:              11.3.0.6295
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES  */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Дамп структуры базы данных kTVCSS
CREATE DATABASE IF NOT EXISTS "kTVCSS";
USE "kTVCSS";

-- Дамп структуры для таблица kTVCSS.BattleCupList
CREATE TABLE IF NOT EXISTS "BattleCupList" (
	"ID" INT NOT NULL,
	"NAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STATUS" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.BattleCupMatches
CREATE TABLE IF NOT EXISTS "BattleCupMatches" (
	"ID" INT NOT NULL,
	"MATCHID" INT NULL DEFAULT NULL,
	"MATCHPLAYED" INT NULL DEFAULT NULL,
	"MATCHWINNERNAME" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"ANAME" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"BNAME" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"BCID" INT NULL DEFAULT NULL,
	"SERVERID" INT NULL DEFAULT NULL,
	"DTSTART" DATETIME NULL DEFAULT NULL,
	"DTEND" DATETIME NULL DEFAULT NULL,
	"BRACKET" NVARCHAR(10) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.BattleCupRecovery
CREATE TABLE IF NOT EXISTS "BattleCupRecovery" (
	"BOARDID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"BCID" INT NULL DEFAULT NULL,
	"RTEAMS" NVARCHAR(max) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"TEAMS" NVARCHAR(max) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"POSTID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"PASSWORD" INT NULL DEFAULT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.ChatHistory
CREATE TABLE IF NOT EXISTS "ChatHistory" (
	"ID" INT NOT NULL,
	"STEAMID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MESSAGE" NVARCHAR(max) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NULL DEFAULT NULL,
	"DATETIME" DATETIME NULL DEFAULT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура kTVCSS.CreateMatch
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[CreateMatch]
	-- Add the parameters for the stored procedure here
	@SERVERID int,
	@MAP nvarchar(20)
AS
BEGIN
	INSERT INTO [dbo].[MatchesLive] (ASCORE, BSCORE, SERVERID, MAP, FINISHED) VALUES (0, 0, @SERVERID, @MAP, 0);
	SELECT ID FROM [dbo].[MatchesLive] WHERE SERVERID = @SERVERID ORDER BY ID DESC
	UPDATE [dbo].[GameServers] SET BUSY = 1 WHERE ID = @SERVERID
	DECLARE @MATCHID int
	SELECT TOP(1) @MATCHID = ID FROM [dbo].[MatchesLive] WHERE SERVERID = @SERVERID AND FINISHED = 0;
	INSERT INTO [dbo].[MatchesMVP] (ID) VALUES (@MATCHID)
END
//
DELIMITER ;

-- Дамп структуры для таблица kTVCSS.CupWorkerMatches
CREATE TABLE IF NOT EXISTS "CupWorkerMatches" (
	"ID" INT NOT NULL,
	"ATEAM" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"BTEAM" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NOT NULL,
	"DTSTART" DATETIME NOT NULL,
	"DTEND" DATETIME NOT NULL,
	"STATUS" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.CupWorkerTeams
CREATE TABLE IF NOT EXISTS "CupWorkerTeams" (
	"ID" INT NOT NULL,
	"NAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STAFF" NVARCHAR(max) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура kTVCSS.EndMatch
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[EndMatch]
	-- Add the parameters for the stored procedure here
	@SERVERID int,
	@ANAME nvarchar(20),
	@BNAME nvarchar(20),
	@ASCORE nvarchar(20),
	@BSCORE nvarchar(20),
	@MAP nvarchar(20)
AS
BEGIN
	DECLARE @MATCHID int
	SELECT @MATCHID = ID FROM [dbo].[MatchesLive] WHERE SERVERID = @SERVERID
	UPDATE [dbo].[MatchesLive] SET FINISHED = 1 WHERE SERVERID = @SERVERID AND FINISHED = 0
	UPDATE [dbo].[GameServers] SET BUSY = 0 WHERE ID = @SERVERID
	INSERT INTO [dbo].[Matches] (ID, ANAME, BNAME, ASCORE, BSCORE, MATCHDATE, MAP, SERVERID)
	VALUES (@MATCHID, @ANAME, @BNAME, @ASCORE, @BSCORE, SYSDATETIME(), @MAP, @SERVERID)
	--DELETE FROM [dbo].[MatchesLive] WHERE ID = @MATCHID AND FINISHED = 1
	RETURN @MATCHID
END
//
DELIMITER ;

-- Дамп структуры для таблица kTVCSS.GameServers
CREATE TABLE IF NOT EXISTS "GameServers" (
	"ID" INT NOT NULL,
	"HOST" NVARCHAR(15) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"USERNAME" NVARCHAR(20) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"USERPASSWORD" NVARCHAR(32) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"PORT" SMALLINT NULL DEFAULT NULL,
	"GAMEPORT" SMALLINT NOT NULL,
	"RCONPASSWORD" NVARCHAR(32) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"ENABLED" TINYINT NOT NULL,
	"BUSY" TINYINT NOT NULL,
	"NODEHOST" NVARCHAR(15) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"NODEPORT" SMALLINT NOT NULL,
	PRIMARY KEY ("ID")
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для функция kTVCSS.GetMatchMVP
DELIMITER //
CREATE FUNCTION [dbo].[GetMatchMVP](@ID int)
RETURNS nvarchar(50)
AS
BEGIN
	DECLARE @STEAMID nvarchar(50)
	SELECT TOP(1) @STEAMID = STEAMID FROM MatchesHighlights WHERE ID = @ID GROUP BY STEAMID ORDER BY SUM(OPENFRAGS + TRIPPLES * 2 + QUADROS * 2 + RAMPAGES * 3) DESC
	return @STEAMID;
END



//
DELIMITER ;

-- Дамп структуры для функция kTVCSS.GetPlayerAVG
DELIMITER //

CREATE FUNCTION [dbo].[GetPlayerAVG](@STEAMID nvarchar(30))
RETURNS float
AS
BEGIN
	DECLARE @KILLTABLE TABLE (KILLS INT)
	INSERT INTO @KILLTABLE SELECT TOP 20(KILLS) FROM [dbo].[MatchesResults] WHERE STEAMID = @STEAMID ORDER BY ID DESC

    DECLARE @AVG FLOAT
	SELECT @AVG = AVG(CAST(KILLS AS FLOAT)) FROM @KILLTABLE
	return @AVG;
END



//
DELIMITER ;

-- Дамп структуры для функция kTVCSS.GetRankName
DELIMITER //
CREATE FUNCTION dbo.GetRankName(@MMR int)
RETURNS nvarchar(10)
AS
BEGIN
    DECLARE @NAME nvarchar(10)
	SELECT @NAME = NAME FROM [dbo].[Ranks] AS RANKS WHERE @MMR >= RANKS.STARTMMR AND @MMR <= RANKS.ENDMMR;
	return @NAME;
END



//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertChatMessage
DELIMITER //
CREATE PROCEDURE [dbo].[InsertChatMessage] 
	@STEAMID nvarchar(50),
	@MESSAGE nvarchar(max),
	@SERVERID int
AS
BEGIN

	INSERT INTO [dbo].[ChatHistory] (STEAMID, MESSAGE, SERVERID, DATETIME) VALUES (@STEAMID, @MESSAGE, @SERVERID, GETDATE())
	
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertDemoName
DELIMITER //
CREATE PROCEDURE [dbo].[InsertDemoName] 
	@MATCHID int,
	@DEMONAME nvarchar(100)
AS
BEGIN

	INSERT INTO [dbo].[MatchesDemos] (ID, DEMONAME) VALUES (@MATCHID, @DEMONAME)
	
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertMatchBackupRecord
DELIMITER //
CREATE PROCEDURE [dbo].[InsertMatchBackupRecord] 
  @ID int,
  @STEAMID nvarchar(50),
  @MONEY int,
  @PW nvarchar(100),
  @SW nvarchar(100),
  @HE int,
  @FLASHBANGS int,
  @SMOKEGRENADES int,
  @HELM int,
  @ARMOR int,
  @DEFUSEKIT int,
  @FRAGS int,
  @DEATHS int
AS
BEGIN
	DECLARE @COUNT int

	SELECT @COUNT = COUNT(STEAMID) FROM [dbo].[MatchesBackups] WHERE STEAMID = @STEAMID AND ID = @ID

	IF (@COUNT = 0)
	BEGIN
		INSERT INTO [kTVCSS].[dbo].[MatchesBackups] 
		VALUES (@ID, @STEAMID, @MONEY, @PW, @SW, @HE, @FLASHBANGS, @SMOKEGRENADES, @HELM, @ARMOR, @DEFUSEKIT, @FRAGS, @DEATHS)
	END
	ELSE 
	BEGIN
		UPDATE [dbo].[MatchesBackups] SET MONEY = @MONEY, PRIMARYWEAPON = @PW, SECONDARYWEAPON = @SW, FRAGGRENADES = @HE, FLASHBANGS = @FLASHBANGS,
		SMOKEGRENADES = @SMOKEGRENADES, HELM = @HELM, ARMOR = @ARMOR, DEFUSEKIT = @DEFUSEKIT, FRAGS = @FRAGS, DEATHS = @DEATHS WHERE STEAMID = @STEAMID AND ID = @ID
	END
	
END//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertMatchLogRecord
DELIMITER //
CREATE PROCEDURE [dbo].[InsertMatchLogRecord] 
	@MATCHID int,
	@MESSAGE nvarchar(1000),
	@MAP nvarchar(30),
	@SERVERID int
AS
BEGIN
	INSERT INTO [kTVCSS].[dbo].MatchesLogs (MATCHID, DATETIME, MESSAGE, MAP, SERVERID) VALUES 
	(@MATCHID, GETDATE(), @MESSAGE, @MAP, @SERVERID)
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertMatchResult
DELIMITER //
CREATE PROCEDURE [dbo].[InsertMatchResult] 
	@MATCHID int,
	@TEAMNAME nvarchar(30),
	@PLAYERNAME nvarchar(50),
	@STEAMID nvarchar(50),
	@KILLS int,
	@DEATHS int,
	@HEADSHOTS int,
	@SERVERID int
AS
BEGIN
	INSERT INTO [kTVCSS].[dbo].[MatchesResults] (ID, TEAMNAME, NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, SERVERID) 
    VALUES (@MATCHID, @TEAMNAME, @PLAYERNAME, @STEAMID, @KILLS, @DEATHS, @HEADSHOTS, @SERVERID)
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.InsertRatingProgress
DELIMITER //
CREATE PROCEDURE [dbo].[InsertRatingProgress] 
	@STEAMID nvarchar(50),
	@MMR int
AS
BEGIN

	IF (@MMR != 0)
	BEGIN
		INSERT INTO [dbo].[PlayersRatingProgress] (STEAMID, MMR, DATETIME) VALUES (@STEAMID, @MMR, GETDATE())
	END
	
END
//
DELIMITER ;

-- Дамп структуры для таблица kTVCSS.MapPool
CREATE TABLE IF NOT EXISTS "MapPool" (
	"MAP" NCHAR(20) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MapQueue
CREATE TABLE IF NOT EXISTS "MapQueue" (
	"MAPNAME" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.Matches
CREATE TABLE IF NOT EXISTS "Matches" (
	"ID" INT NOT NULL,
	"ANAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"BNAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"ASCORE" INT NOT NULL,
	"BSCORE" INT NOT NULL,
	"MATCHDATE" DATETIME NOT NULL,
	"MAP" NVARCHAR(20) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NOT NULL,
	PRIMARY KEY ("ID")
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesBackups
CREATE TABLE IF NOT EXISTS "MatchesBackups" (
	"ID" INT NOT NULL,
	"STEAMID" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MONEY" NVARCHAR(10) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"PRIMARYWEAPON" NVARCHAR(100) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SECONDARYWEAPON" NVARCHAR(100) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"FRAGGRENADES" INT NOT NULL,
	"FLASHBANGS" INT NOT NULL,
	"SMOKEGRENADES" INT NOT NULL,
	"HELM" INT NOT NULL,
	"ARMOR" INT NOT NULL,
	"DEFUSEKIT" INT NOT NULL,
	"FRAGS" INT NOT NULL,
	"DEATHS" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesDemos
CREATE TABLE IF NOT EXISTS "MatchesDemos" (
	"ID" INT NOT NULL,
	"DEMONAME" NVARCHAR(100) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesHighlights
CREATE TABLE IF NOT EXISTS "MatchesHighlights" (
	"ID" INT NOT NULL,
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"TRIPPLES" INT NOT NULL,
	"QUADROS" INT NOT NULL,
	"RAMPAGES" INT NOT NULL,
	"OPENFRAGS" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesLive
CREATE TABLE IF NOT EXISTS "MatchesLive" (
	"ID" INT NOT NULL,
	"ASCORE" INT NOT NULL,
	"BSCORE" INT NOT NULL,
	"SERVERID" INT NOT NULL,
	"MAP" NVARCHAR(20) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"FINISHED" TINYINT NOT NULL,
	PRIMARY KEY ("ID")
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesLogs
CREATE TABLE IF NOT EXISTS "MatchesLogs" (
	"MATCHID" INT NOT NULL,
	"DATETIME" DATETIME NOT NULL,
	"MESSAGE" NVARCHAR(1000) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MAP" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesMVP
CREATE TABLE IF NOT EXISTS "MatchesMVP" (
	"ID" INT NOT NULL,
	"MVP" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesResults
CREATE TABLE IF NOT EXISTS "MatchesResults" (
	"ID" INT NOT NULL,
	"TEAMNAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"NAME" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"KILLS" INT NOT NULL,
	"DEATHS" INT NOT NULL,
	"HEADSHOTS" INT NOT NULL,
	"SERVERID" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.MatchesResultsLive
CREATE TABLE IF NOT EXISTS "MatchesResultsLive" (
	"ID" INT NOT NULL,
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"KILLS" INT NOT NULL,
	"DEATHS" INT NOT NULL,
	"HEADSHOTS" INT NOT NULL,
	"SERVERID" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура kTVCSS.MatchLiveCheckExists
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[MatchLiveCheckExists]
	-- Add the parameters for the stored procedure here
	@SERVERID int
AS
BEGIN
	DECLARE @MATCHID int
	SELECT TOP(1) @MATCHID = ID FROM [dbo].[MatchesLive] WHERE SERVERID = @SERVERID AND FINISHED = 0;
	IF (@MATCHID != 0)
	BEGIN
	SELECT @MATCHID;
	END
	ELSE
	BEGIN
	SELECT 0
	END
END
//
DELIMITER ;

-- Дамп структуры для таблица kTVCSS.NewsCups
CREATE TABLE IF NOT EXISTS "NewsCups" (
	"ID" INT NOT NULL,
	"TITLE" NVARCHAR(100) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"TEXT" NVARCHAR(max) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"DATETIME" DATETIME NULL DEFAULT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.NewsProject
CREATE TABLE IF NOT EXISTS "NewsProject" (
	"ID" INT NOT NULL,
	"TITLE" NVARCHAR(100) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"TEXT" NVARCHAR(max) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"DATETIME" DATETIME NULL DEFAULT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура kTVCSS.OnMatchHighlight
DELIMITER //
CREATE PROCEDURE [dbo].[OnMatchHighlight] 
	@MATCHID int,
	@KILLS int,
	@OPENFRAG int,
	@STEAMID nvarchar(30)
AS
BEGIN
	DECLARE @COUNTHIGHLIGHTS int
	SELECT @COUNTHIGHLIGHTS = COUNT(STEAMID) FROM [dbo].[MatchesHighlights] WHERE STEAMID = @STEAMID AND ID = @MATCHID
	IF (@COUNTHIGHLIGHTS = 0)
	BEGIN
		INSERT INTO [dbo].[MatchesHighlights] (ID, STEAMID, TRIPPLES, QUADROS, RAMPAGES, OPENFRAGS) VALUES (@MATCHID, @STEAMID, 0, 0, 0, 0);
	END

	IF @OPENFRAG = 1
	BEGIN
		UPDATE [kTVCSS].[dbo].[MatchesHighlights] SET OPENFRAGS = OPENFRAGS + 1 WHERE STEAMID = @STEAMID AND ID = @MATCHID
	END

	IF @KILLS = 3
	BEGIN
		UPDATE [kTVCSS].[dbo].[MatchesHighlights] SET TRIPPLES = TRIPPLES + 1 WHERE STEAMID = @STEAMID AND ID = @MATCHID
	END
	IF @KILLS = 4
	BEGIN
		UPDATE [kTVCSS].[dbo].[MatchesHighlights] SET QUADROS = QUADROS + 1 WHERE STEAMID = @STEAMID AND ID = @MATCHID
	END
	IF @KILLS = 5
	BEGIN
		UPDATE [kTVCSS].[dbo].[MatchesHighlights] SET RAMPAGES = RAMPAGES + 1 WHERE STEAMID = @STEAMID AND ID = @MATCHID
	END
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.OnPlayerConnectAuth
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[OnPlayerConnectAuth] 
	@NAME nvarchar(30),
	@STEAMID nvarchar(20)
AS
BEGIN
	DECLARE @COUNT int
	DECLARE @COUNTHIGHLIGHTS int

	SELECT @COUNTHIGHLIGHTS = COUNT(STEAMID) FROM [dbo].[PlayersHighlights] WHERE STEAMID = @STEAMID
	IF (@COUNTHIGHLIGHTS = 0)
		BEGIN
		INSERT INTO [dbo].[PlayersHighlights] (STEAMID, TRIPPLES, QUADROS, RAMPAGES, OPENFRAGS) VALUES (@STEAMID, 0, 0, 0, 0);
		END

	SELECT @COUNT = COUNT(STEAMID) FROM [dbo].[Players] WHERE STEAMID = @STEAMID
	IF (@COUNT = 0)
		BEGIN
		INSERT INTO kTVCSS.dbo.Players (NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, MMR, MATCHESPLAYED, MATCHESWINS, MATCHESLOOSES, ISCALIBRATION) VALUES (@NAME, @STEAMID, '0', '0', '0', '0', '0', '0', '0', '1');
		SELECT 0;
		END;
	ELSE 
	BEGIN
	SELECT @COUNT;
	END
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.OnPlayerHighlight
DELIMITER //
CREATE PROCEDURE [dbo].[OnPlayerHighlight] 
	@KILLS int,
	@STEAMID nvarchar(30)
AS
BEGIN
	IF @KILLS = 3
	BEGIN
	UPDATE [kTVCSS].[dbo].[PlayersHighlights] SET TRIPPLES = TRIPPLES + 1 WHERE STEAMID = @STEAMID
	END
	IF @KILLS = 4
	BEGIN
	UPDATE [kTVCSS].[dbo].[PlayersHighlights] SET QUADROS = QUADROS + 1 WHERE STEAMID = @STEAMID
	END
	IF @KILLS = 5
	BEGIN
	UPDATE [kTVCSS].[dbo].[PlayersHighlights] SET RAMPAGES = RAMPAGES + 1 WHERE STEAMID = @STEAMID
	END
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.OnPlayerKill
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[OnPlayerKill] 
	@KILLERNAME nvarchar(30),
	@KILLEDNAME nvarchar(30),
	@KILLERSTEAM nvarchar(30),
	@KILLEDSTEAM nvarchar(30),
	@KILLERHS tinyint,
	@SERVERID tinyint,
	@ID int
AS
BEGIN
	DECLARE @KILLEREXISTS tinyint;
	DECLARE @KILLEDEXISTS tinyint;
	SELECT @KILLEREXISTS = COUNT(@KILLERSTEAM) FROM [dbo].[MatchesResultsLive] WHERE ID = @ID AND STEAMID = @KILLERSTEAM;
	SELECT @KILLEDEXISTS = COUNT(@KILLEDSTEAM) FROM [dbo].[MatchesResultsLive] WHERE ID = @ID AND STEAMID = @KILLEDSTEAM;

	IF (@KILLEREXISTS = 0)
	BEGIN
		INSERT INTO [dbo].[MatchesResultsLive] (STEAMID, KILLS, DEATHS, HEADSHOTS, SERVERID, ID) VALUES (@KILLERSTEAM, 0, 0, 0, @SERVERID, @ID);
	END

	IF (@KILLEDEXISTS = 0)
	BEGIN
		INSERT INTO [dbo].[MatchesResultsLive] (STEAMID, KILLS, DEATHS, HEADSHOTS, SERVERID, ID) VALUES (@KILLEDSTEAM, 0, 0, 0, @SERVERID, @ID);
	END

	UPDATE [dbo].[MatchesResultsLive] SET KILLS = 1 + KILLS, HEADSHOTS = @KILLERHS + HEADSHOTS WHERE STEAMID = @KILLERSTEAM AND ID = @ID;
	UPDATE [dbo].[MatchesResultsLive] SET DEATHS = 1 + DEATHS WHERE STEAMID = @KILLEDSTEAM AND ID = @ID;

	UPDATE [dbo].[Players] SET NAME = @KILLERNAME, KILLS = 1 + KILLS, HEADSHOTS = @KILLERHS + HEADSHOTS WHERE STEAMID = @KILLERSTEAM;
	UPDATE [dbo].[Players] SET NAME = @KILLEDNAME, DEATHS = 1 + DEATHS WHERE STEAMID = @KILLEDSTEAM;
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.OnPlayerKillByWeapon
DELIMITER //
CREATE PROCEDURE OnPlayerKillByWeapon
	@STEAMID nvarchar(30),
	@WEAPON nvarchar(50)
AS
BEGIN
	DECLARE @EXISTS int
	SELECT @EXISTS = COUNT(WEAPON) FROM [dbo].[PlayersWeaponKills] WHERE STEAMID = @STEAMID AND WEAPON = @WEAPON
	IF (@EXISTS = 0)
	BEGIN
		INSERT INTO kTVCSS.dbo.PlayersWeaponKills (STEAMID, WEAPON, COUNT) VALUES (@STEAMID, @WEAPON, 1)
	END
	ELSE 
	BEGIN
		UPDATE kTVCSS.dbo.PlayersWeaponKills SET COUNT = COUNT + 1 WHERE STEAMID = @STEAMID AND WEAPON = @WEAPON
	END
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.OnPlayerOpenFrag
DELIMITER //
CREATE PROCEDURE [dbo].[OnPlayerOpenFrag] 
	@STEAMID nvarchar(30)
AS
BEGIN

	UPDATE [kTVCSS].[dbo].[PlayersHighlights] SET OPENFRAGS = OPENFRAGS + 1 WHERE STEAMID = @STEAMID
	
END
//
DELIMITER ;

-- Дамп структуры для таблица kTVCSS.Players
CREATE TABLE IF NOT EXISTS "Players" (
	"ID" INT NOT NULL,
	"NAME" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"KILLS" FLOAT NULL DEFAULT NULL,
	"DEATHS" FLOAT NULL DEFAULT NULL,
	"HEADSHOTS" FLOAT NULL DEFAULT NULL,
	"KDR" FLOAT NULL DEFAULT NULL,
	"HSR" FLOAT NULL DEFAULT NULL,
	"MMR" INT NULL DEFAULT NULL,
	"AVG" FLOAT NULL DEFAULT NULL,
	"RANKNAME" NVARCHAR(10) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MATCHESPLAYED" FLOAT NULL DEFAULT NULL,
	"MATCHESWINS" FLOAT NULL DEFAULT NULL,
	"MATCHESLOOSES" FLOAT NULL DEFAULT NULL,
	"ISCALIBRATION" TINYINT NULL DEFAULT NULL,
	"LASTMATCH" DATETIME NULL DEFAULT NULL,
	"VKID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"ANOUNCE" TINYINT NULL DEFAULT NULL,
	"WINRATE" FLOAT NULL DEFAULT NULL,
	"BLOCK" TINYINT NULL DEFAULT NULL,
	"BLOCKREASON" NVARCHAR(250) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	PRIMARY KEY ("ID")
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.PlayersHighlights
CREATE TABLE IF NOT EXISTS "PlayersHighlights" (
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"TRIPPLES" INT NOT NULL,
	"QUADROS" INT NOT NULL,
	"RAMPAGES" INT NOT NULL,
	"OPENFRAGS" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.PlayersJoinHistory
CREATE TABLE IF NOT EXISTS "PlayersJoinHistory" (
	"ID" INT NOT NULL,
	"STEAMID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"IP" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"DATETIME" DATETIME NULL DEFAULT NULL,
	"TYPE" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"SERVERID" INT NULL DEFAULT NULL,
	"REASON" NVARCHAR(255) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8'
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.PlayersRatingProgress
CREATE TABLE IF NOT EXISTS "PlayersRatingProgress" (
	"STEAMID" NVARCHAR(50) NULL DEFAULT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MMR" INT NULL DEFAULT NULL,
	"DATETIME" DATETIME NULL DEFAULT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.PlayersWeaponKills
CREATE TABLE IF NOT EXISTS "PlayersWeaponKills" (
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"WEAPON" NVARCHAR(50) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"COUNT" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица kTVCSS.Ranks
CREATE TABLE IF NOT EXISTS "Ranks" (
	"NAME" NVARCHAR(10) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STARTMMR" INT NOT NULL,
	"ENDMMR" INT NOT NULL
);

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура kTVCSS.ResetMatch
DELIMITER //
CREATE PROCEDURE [dbo].[ResetMatch]
	@MATCHID int,
	@SERVERID int
AS
BEGIN
	DELETE FROM [dbo].[MatchesLive] WHERE ID = @MATCHID
	UPDATE [dbo].[GameServers] SET BUSY = 0 WHERE ID = @SERVERID
END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.SetPlayerMatchResult
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetPlayerMatchResult]
	-- Add the parameters for the stored procedure here
	@STEAMID nvarchar(30),
	@WIN tinyint,
	@PTS int
AS
BEGIN
	DECLARE @CLB tinyint
	DECLARE @MC int
	SELECT @CLB = ISCALIBRATION FROM [dbo].[Players] WHERE STEAMID = @STEAMID
	IF (@CLB = 1)
	BEGIN
		IF (@WIN = 1)
	BEGIN
		UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESWINS = MATCHESWINS + 1, MMR = 0 WHERE STEAMID = @STEAMID
	END
	ELSE 
	BEGIN
		UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESLOOSES = MATCHESLOOSES + 1, MMR = 0 WHERE STEAMID = @STEAMID
	END
	END
	ELSE
	BEGIN
	IF (@WIN = 1)
	BEGIN
		UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESWINS = MATCHESWINS + 1, MMR = MMR + @PTS WHERE STEAMID = @STEAMID
	END
	ELSE 
	BEGIN
		UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESLOOSES = MATCHESLOOSES + 1, MMR = MMR + @PTS WHERE STEAMID = @STEAMID
	END
	END
	UPDATE [dbo].[Players] SET LASTMATCH = SYSDATETIME() WHERE STEAMID = @STEAMID

	--Калибровка
	SELECT @MC = MATCHESPLAYED FROM [dbo].[Players] WHERE STEAMID = @STEAMID
	IF (@MC = 10)
	BEGIN
		DECLARE @PLAYERKILLS int
		DECLARE @PLAYERWINS int
		DECLARE @PLAYERHS int
		DECLARE @PLAYEROF int
		DECLARE @PLAYERTRIPPLES int
		DECLARE @PLAYERQUADROS int
		DECLARE @PLAYERRAMPAGES int
		DECLARE @PLAYERKD float
		DECLARE @PLAYERAVG float

		SELECT @PLAYERKILLS = KILLS, @PLAYERWINS = MATCHESWINS, @PLAYERHS = HSR, @PLAYERKD = KDR, @PLAYERAVG = AVG FROM [dbo].[Players] WHERE STEAMID = @STEAMID
		SELECT @PLAYEROF = OPENFRAGS, @PLAYERTRIPPLES = TRIPPLES, @PLAYERQUADROS = QUADROS, @PLAYERRAMPAGES = RAMPAGES FROM [dbo].[PlayersHighlights] WHERE STEAMID = @STEAMID

		UPDATE [dbo].[Players] SET MMR = (100 * @PLAYERWINS) + (2 * @PLAYERKILLS) + (2 * @PLAYERHS) + 
		(@PLAYEROF) + (10 * @PLAYERTRIPPLES) + (25 * @PLAYERQUADROS) + (50 * @PLAYERRAMPAGES) + (300 * @PLAYERKD) + (5 * @PLAYERAVG),
		ISCALIBRATION = 0 WHERE STEAMID = @STEAMID
	END

END
//
DELIMITER ;

-- Дамп структуры для процедура kTVCSS.UpdateMatchScore
DELIMITER //
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[UpdateMatchScore] 
	-- Add the parameters for the stored procedure here
	@ASCORE int,
	@BSCORE int,
	@SERVERID int,
	@ID int
AS
BEGIN
	UPDATE [dbo].[MatchesLive] SET ASCORE = @ASCORE, BSCORE = @BSCORE WHERE SERVERID = @SERVERID AND ID = @ID AND FINISHED = 0
END
//
DELIMITER ;

-- Дамп структуры для представление kTVCSS.ViewPlayersGeneral
-- Создание временной таблицы для обработки ошибок зависимостей представлений
CREATE TABLE "ViewPlayersGeneral" (
	"NAME" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"STEAMID" NVARCHAR(30) NOT NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"KILLS" FLOAT NULL,
	"DEATHS" FLOAT NULL,
	"HEADSHOTS" FLOAT NULL,
	"KDR" FLOAT NULL,
	"HSR" FLOAT NULL,
	"MMR" INT NULL,
	"AVG" FLOAT NULL,
	"RANKNAME" NVARCHAR(10) NULL COLLATE 'Cyrillic_General_100_CI_AI_SC_UTF8',
	"MATCHESPLAYED" FLOAT NULL,
	"MATCHESWINS" FLOAT NULL,
	"MATCHESLOOSES" FLOAT NULL,
	"LASTMATCH" DATETIME NULL,
	"WINRATE" FLOAT NULL
) ENGINE=MyISAM;

-- Дамп структуры для представление kTVCSS.ViewPlayersGeneral
-- Удаление временной таблицы и создание окончательной структуры представления
DROP TABLE IF EXISTS "ViewPlayersGeneral";
CREATE VIEW dbo.ViewPlayersGeneral
AS
SELECT        TOP (100) PERCENT NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, KDR, HSR, MMR, AVG, RANKNAME, MATCHESPLAYED, MATCHESWINS, MATCHESLOOSES, LASTMATCH, WINRATE
FROM            dbo.Players
ORDER BY MMR DESC
;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
