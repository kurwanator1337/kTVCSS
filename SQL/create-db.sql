USE [kTVCSS]
GO
/****** Object:  UserDefinedFunction [dbo].[GetMatchMVP]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[GetMatchMVP](@ID int)
RETURNS nvarchar(50)
AS
BEGIN
	DECLARE @STEAMID nvarchar(50)
	SELECT TOP(1) @STEAMID = STEAMID FROM MatchesHighlights WHERE ID = @ID GROUP BY STEAMID ORDER BY SUM(OPENFRAGS + TRIPPLES * 2 + QUADROS * 2 + RAMPAGES * 3) DESC
	return @STEAMID;
END



GO
/****** Object:  UserDefinedFunction [dbo].[GetPlayerAVG]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

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



GO
/****** Object:  UserDefinedFunction [dbo].[GetRankName]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[GetRankName](@MMR int)
RETURNS nvarchar(10)
AS
BEGIN
    DECLARE @NAME nvarchar(10)
	SELECT @NAME = NAME FROM [dbo].[Ranks] AS RANKS WHERE @MMR >= RANKS.STARTMMR AND @MMR <= RANKS.ENDMMR;
	return @NAME;
END



GO
/****** Object:  Table [dbo].[Players]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Players](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[NAME] [nvarchar](30) NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[KILLS] [float] NULL,
	[DEATHS] [float] NULL,
	[HEADSHOTS] [float] NULL,
	[KDR]  AS ([KILLS]/nullif([DEATHS],(0))) PERSISTED,
	[HSR]  AS ([HEADSHOTS]/nullif([KILLS],(0))) PERSISTED,
	[MMR] [int] NULL,
	[AVG]  AS ([dbo].[GetPlayerAVG]([STEAMID])),
	[RANKNAME]  AS ([dbo].[GetRankName]([MMR])),
	[MATCHESPLAYED] [float] NULL,
	[MATCHESWINS] [float] NULL,
	[MATCHESLOOSES] [float] NULL,
	[ISCALIBRATION] [tinyint] NULL,
	[LASTMATCH] [datetime] NULL,
	[VKID] [nvarchar](50) NULL,
	[ANOUNCE] [tinyint] NULL,
	[WINRATE]  AS (([MATCHESWINS]/nullif([MATCHESPLAYED],(0)))*(100)) PERSISTED,
	[BLOCK] [tinyint] NULL,
	[BLOCKREASON] [nvarchar](250) NULL,
 CONSTRAINT [PK_Players] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[ViewPlayersGeneral]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[ViewPlayersGeneral]
AS
SELECT        TOP (100) PERCENT NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, KDR, HSR, MMR, AVG, RANKNAME, MATCHESPLAYED, MATCHESWINS, MATCHESLOOSES, LASTMATCH, WINRATE
FROM            dbo.Players
ORDER BY MMR DESC
GO
/****** Object:  Table [dbo].[Admins]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Admins](
	[NAME] [nvarchar](50) NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BattleCupList]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BattleCupList](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[NAME] [nvarchar](50) NOT NULL,
	[STATUS] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BattleCupMatches]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BattleCupMatches](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MATCHID] [int] NULL,
	[MATCHPLAYED] [int] NULL,
	[MATCHWINNERNAME] [nvarchar](50) NULL,
	[ANAME] [nvarchar](50) NULL,
	[BNAME] [nvarchar](50) NULL,
	[BCID] [int] NULL,
	[SERVERID] [int] NULL,
	[DTSTART] [datetime] NULL,
	[DTEND] [datetime] NULL,
	[BRACKET] [nvarchar](10) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BattleCupRecovery]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BattleCupRecovery](
	[BOARDID] [nvarchar](50) NULL,
	[BCID] [int] NULL,
	[RTEAMS] [nvarchar](max) NULL,
	[TEAMS] [nvarchar](max) NULL,
	[POSTID] [nvarchar](50) NULL,
	[PASSWORD] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChatHistory]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChatHistory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[STEAMID] [nvarchar](50) NULL,
	[MESSAGE] [nvarchar](max) NULL,
	[SERVERID] [int] NULL,
	[DATETIME] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CmsVersion]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CmsVersion](
	[VERSION] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CupWorkerMatches]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CupWorkerMatches](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ATEAM] [nvarchar](50) NOT NULL,
	[BTEAM] [nvarchar](50) NOT NULL,
	[SERVERID] [int] NOT NULL,
	[DTSTART] [datetime] NOT NULL,
	[DTEND] [datetime] NOT NULL,
	[STATUS] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CupWorkerTeams]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CupWorkerTeams](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[NAME] [nvarchar](50) NOT NULL,
	[STAFF] [nvarchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GameServers]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GameServers](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[HOST] [nvarchar](15) NOT NULL,
	[USERNAME] [nvarchar](20) NULL,
	[USERPASSWORD] [nvarchar](32) NULL,
	[PORT] [smallint] NULL,
	[GAMEPORT] [smallint] NOT NULL,
	[RCONPASSWORD] [nvarchar](32) NOT NULL,
	[ENABLED] [tinyint] NOT NULL,
	[BUSY] [tinyint] NOT NULL,
	[NODEHOST] [nvarchar](15) NOT NULL,
	[NODEPORT] [smallint] NOT NULL,
 CONSTRAINT [PK_GameServers] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MapPool]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MapPool](
	[MAP] [nchar](20) NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MapQueue]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MapQueue](
	[MAPNAME] [nvarchar](30) NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Matches]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Matches](
	[ID] [int] NOT NULL,
	[ANAME] [nvarchar](50) NOT NULL,
	[BNAME] [nvarchar](50) NOT NULL,
	[ASCORE] [int] NOT NULL,
	[BSCORE] [int] NOT NULL,
	[MATCHDATE] [datetime] NOT NULL,
	[MAP] [nvarchar](20) NOT NULL,
	[SERVERID] [int] NOT NULL,
 CONSTRAINT [PK_Matches] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesBackups]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesBackups](
	[ID] [int] NOT NULL,
	[STEAMID] [nvarchar](50) NOT NULL,
	[MONEY] [nvarchar](10) NOT NULL,
	[PRIMARYWEAPON] [nvarchar](100) NOT NULL,
	[SECONDARYWEAPON] [nvarchar](100) NOT NULL,
	[FRAGGRENADES] [int] NOT NULL,
	[FLASHBANGS] [int] NOT NULL,
	[SMOKEGRENADES] [int] NOT NULL,
	[HELM] [int] NOT NULL,
	[ARMOR] [int] NOT NULL,
	[DEFUSEKIT] [int] NOT NULL,
	[FRAGS] [int] NOT NULL,
	[DEATHS] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesDemos]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesDemos](
	[ID] [int] NOT NULL,
	[DEMONAME] [nvarchar](100) NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesHighlights]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesHighlights](
	[ID] [int] NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[TRIPPLES] [int] NOT NULL,
	[QUADROS] [int] NOT NULL,
	[RAMPAGES] [int] NOT NULL,
	[OPENFRAGS] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesLive]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesLive](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ASCORE] [int] NOT NULL,
	[BSCORE] [int] NOT NULL,
	[SERVERID] [int] NOT NULL,
	[MAP] [nvarchar](20) NULL,
	[FINISHED] [tinyint] NOT NULL,
 CONSTRAINT [PK_MatchesLive] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesLogs]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesLogs](
	[MATCHID] [int] NOT NULL,
	[DATETIME] [datetime] NOT NULL,
	[MESSAGE] [nvarchar](1000) NULL,
	[MAP] [nvarchar](30) NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesMVP]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesMVP](
	[ID] [int] NOT NULL,
	[MVP]  AS ([dbo].[GetMatchMVP]([ID]))
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesResults]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesResults](
	[ID] [int] NOT NULL,
	[TEAMNAME] [nvarchar](50) NOT NULL,
	[NAME] [nvarchar](50) NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[KILLS] [int] NOT NULL,
	[DEATHS] [int] NOT NULL,
	[HEADSHOTS] [int] NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MatchesResultsLive]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesResultsLive](
	[ID] [int] NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[KILLS] [int] NOT NULL,
	[DEATHS] [int] NOT NULL,
	[HEADSHOTS] [int] NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NewsCups]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewsCups](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TITLE] [nvarchar](100) NULL,
	[TEXT] [nvarchar](max) NULL,
	[DATETIME] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NewsProject]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewsProject](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TITLE] [nvarchar](100) NULL,
	[TEXT] [nvarchar](max) NULL,
	[DATETIME] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlayersHighlights]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayersHighlights](
	[STEAMID] [nvarchar](30) NOT NULL,
	[TRIPPLES] [int] NOT NULL,
	[QUADROS] [int] NOT NULL,
	[RAMPAGES] [int] NOT NULL,
	[OPENFRAGS] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlayersJoinHistory]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayersJoinHistory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[STEAMID] [nvarchar](50) NULL,
	[IP] [nvarchar](50) NULL,
	[DATETIME] [datetime] NULL,
	[TYPE] [nvarchar](50) NULL,
	[SERVERID] [int] NULL,
	[REASON] [nvarchar](255) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlayersRatingProgress]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayersRatingProgress](
	[STEAMID] [nvarchar](50) NULL,
	[MMR] [int] NULL,
	[DATETIME] [datetime] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlayersWeaponKills]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayersWeaponKills](
	[STEAMID] [nvarchar](30) NOT NULL,
	[WEAPON] [nvarchar](50) NOT NULL,
	[COUNT] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Ranks]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ranks](
	[NAME] [nvarchar](10) NOT NULL,
	[STARTMMR] [int] NOT NULL,
	[ENDMMR] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[BKSetTP]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[BKSetTP]
	@TEAMA nvarchar(max),
	@TEAMB nvarchar(max),
	@DATETIME nvarchar(max)
AS
BEGIN
	INSERT INTO [dbo].[MatchesLive] (ASCORE, BSCORE, SERVERID, MAP, FINISHED) VALUES (0, 0, 100, 'TP', 3);
	SELECT ID FROM [dbo].[MatchesLive] WHERE SERVERID = 100 ORDER BY ID DESC
	DECLARE @MATCHID int
	SELECT TOP(1) @MATCHID = ID FROM [dbo].[MatchesLive] WHERE SERVERID = 100 AND FINISHED = 3 ORDER BY ID DESC;
	INSERT INTO "kTVCSS"."dbo"."Matches" ("ID", "ANAME", "BNAME", "ASCORE", "BSCORE", "MATCHDATE", "MAP", "SERVERID") VALUES (@MATCHID, @TEAMA, @TEAMB, '16', '0', @DATETIME, N'ТП', '100');
END
GO
/****** Object:  StoredProcedure [dbo].[CreateMatch]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[DeletePlayer]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[DeletePlayer]
	-- Add the parameters for the stored procedure here
	@STEAMID nvarchar(50)
AS
BEGIN
    DELETE FROM [kTVCSS].[dbo].ChatHistory WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].MatchesHighlights WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].MatchesResults WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].MatchesMVP WHERE MVP = @STEAMID
    DELETE FROM [kTVCSS].[dbo].MatchesResultsLive WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].Players WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].PlayersHighlights WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].PlayersJoinHistory WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].PlayersRatingProgress WHERE STEAMID = @STEAMID
    DELETE FROM [kTVCSS].[dbo].PlayersWeaponKills WHERE STEAMID = @STEAMID
END
GO
/****** Object:  StoredProcedure [dbo].[EndMatch]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[GetPlayerInfoForPicture]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GetPlayerInfoForPicture]
	@STEAMID nvarchar(50),
	@MATCHID int
AS
BEGIN
	DECLARE @KILLS float
	DECLARE @DEATHS float
	DECLARE @HEADSHOTS float
	DECLARE @MMR int
	DECLARE @RANKNAME nvarchar(50)
	DECLARE @MT int
	DECLARE @MW int
	DECLARE @ML int
	DECLARE @KDR float
	DECLARE @AVG float
	DECLARE @ACES int
	DECLARE @QUADRA int
	DECLARE @TRIPLE int
	DECLARE @OPENS int

	SELECT @KILLS = KILLS, @DEATHS = DEATHS, @HEADSHOTS = HEADSHOTS / @KILLS FROM MatchesResults WHERE STEAMID = @STEAMID AND ID = @MATCHID

	SELECT @MMR = MMR, @MT = MATCHESPLAYED, @MW = MATCHESWINS, @ML = MATCHESLOOSES FROM Players WHERE STEAMID = @STEAMID
	SELECT @KDR = KDR, @AVG = AVG, @RANKNAME = RANKNAME FROM Players WHERE STEAMID = @STEAMID
	SELECT @ACES = RAMPAGES, @QUADRA = QUADROS, @TRIPLE = TRIPPLES, @OPENS = OPENFRAGS FROM PlayersHighlights WHERE STEAMID = @STEAMID

	SELECT @KILLS AS KILLS, @DEATHS AS DEATHS, ROUND(@HEADSHOTS, 2) AS HEADSHOTS, @MMR AS MMR, @RANKNAME AS RANKNAME, @MT AS TOTAL, 
	@MW AS WON, @ML AS LOST, ROUND(@KDR, 2) AS KDR, ROUND(@AVG, 2) AS AVG, @ACES AS ACES, @QUADRA AS QUADRA, @TRIPLE AS TRIPLE, @OPENS AS OPENS
END
GO
/****** Object:  StoredProcedure [dbo].[GetPlayersOfTheDayAuto]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GetPlayersOfTheDayAuto]
	
AS
BEGIN
	DECLARE @TempPlayerResults TABLE (STEAMID NVARCHAR(50), RATING FLOAT, HSR FLOAT)

	INSERT INTO @TempPlayerResults SELECT [STEAMID], CAST(SUM([KILLS]) AS FLOAT) / CAST(SUM([DEATHS]) AS FLOAT) AS RATING, 
	CAST(SUM(HEADSHOTS) AS FLOAT) / CAST(SUM(KILLS) AS FLOAT) AS HSR FROM [kTVCSS].[dbo].[MatchesResults] 
	  INNER JOIN Matches ON Matches.ID = MatchesResults.ID
	  WHERE DATEPART(DAYOFYEAR, MATCHDATE) = DATEPART(DAYOFYEAR, GETDATE())
	  GROUP BY [STEAMID]
	  ORDER BY RATING DESC

	SELECT Players.NAME, MatchesResults.TEAMNAME, RATING, T.HSR, VKID FROM Players 
	INNER JOIN @TempPlayerResults AS T ON T.STEAMID = Players.STEAMID 
	CROSS APPLY (SELECT TOP 1 TEAMNAME FROM MatchesResults WHERE Players.STEAMID = MatchesResults.STEAMID ORDER BY ID DESC) MatchesResults
	ORDER BY RATING DESC
END
GO
/****** Object:  StoredProcedure [dbo].[InsertBattleCupMatch]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[InsertBattleCupMatch] 
	@ANAME nvarchar(100),
	@BNAME nvarchar(100),
	@SERVERID int,
	@DTSTART datetime,
	@DTEND datetime,
	@BRACKET nvarchar(100)
AS
BEGIN
	INSERT INTO BattleCupMatches (ANAME, BNAME, SERVERID, DTSTART, DTEND, BRACKET) VALUES 
	(@ANAME, @BNAME, @SERVERID, @DTSTART, @DTEND, @BRACKET)
END
GO
/****** Object:  StoredProcedure [dbo].[InsertChatMessage]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertChatMessage] 
	@STEAMID nvarchar(50),
	@MESSAGE nvarchar(max),
	@SERVERID int
AS
BEGIN

	INSERT INTO [dbo].[ChatHistory] (STEAMID, MESSAGE, SERVERID, DATETIME) VALUES (@STEAMID, @MESSAGE, @SERVERID, GETDATE())
	
END
GO
/****** Object:  StoredProcedure [dbo].[InsertDemoName]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertDemoName] 
	@MATCHID int,
	@DEMONAME nvarchar(100)
AS
BEGIN

	INSERT INTO [dbo].[MatchesDemos] (ID, DEMONAME) VALUES (@MATCHID, @DEMONAME)
	
END
GO
/****** Object:  StoredProcedure [dbo].[InsertMatchBackupRecord]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
	
END
GO
/****** Object:  StoredProcedure [dbo].[InsertMatchLogRecord]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[InsertMatchResult]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
	IF (@KILLS != 0 AND @DEATHS != 0)
	BEGIN
		INSERT INTO [kTVCSS].[dbo].[MatchesResults] (ID, TEAMNAME, NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, SERVERID) 
		VALUES (@MATCHID, @TEAMNAME, @PLAYERNAME, @STEAMID, @KILLS, @DEATHS, @HEADSHOTS, @SERVERID)
	END
END
GO
/****** Object:  StoredProcedure [dbo].[InsertRatingProgress]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[MatchLiveCheckExists]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[OnMatchHighlight]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[OnPlayerConnectAuth]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[OnPlayerHighlight]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[OnPlayerKill]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[OnPlayerKillByWeapon]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[OnPlayerKillByWeapon]
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
GO
/****** Object:  StoredProcedure [dbo].[OnPlayerOpenFrag]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[OnPlayerOpenFrag] 
	@STEAMID nvarchar(30)
AS
BEGIN

	UPDATE [kTVCSS].[dbo].[PlayersHighlights] SET OPENFRAGS = OPENFRAGS + 1 WHERE STEAMID = @STEAMID
	
END
GO
/****** Object:  StoredProcedure [dbo].[ResetMatch]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ResetMatch]
	@MATCHID int,
	@SERVERID int
AS
BEGIN
	--DELETE FROM [dbo].[MatchesLive] WHERE ID = @MATCHID
	UPDATE [dbo].[MatchesLive] SET FINISHED = 2 WHERE ID = @MATCHID
	UPDATE [dbo].[GameServers] SET BUSY = 0 WHERE ID = @SERVERID
END
GO
/****** Object:  StoredProcedure [dbo].[SetPlayerMatchResult]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
/****** Object:  StoredProcedure [dbo].[UpdateMatchScore]    Script Date: 30.09.2022 13:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
	UPDATE [dbo].[MatchesLive] SET ASCORE = @ASCORE, BSCORE = @BSCORE WHERE SERVERID = @SERVERID AND ID = @ID
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Players"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 277
               Right = 530
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'ViewPlayersGeneral'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'ViewPlayersGeneral'
GO
