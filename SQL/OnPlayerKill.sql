USE [kTVCSS]
GO

/****** Object:  StoredProcedure [dbo].[OnPlayerKill]    Script Date: 08.12.2021 23:13:37 ******/
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
	-- Add the parameters for the stored procedure here
	@KILLERSTEAM nvarchar(30),
	@KILLEDSTEAM nvarchar(30),
	@KILLERHS tinyint,
	@SERVERID tinyint,
	@ID int
AS
BEGIN
	DECLARE @KILLEREXISTS tinyint;
	DECLARE @KILLEDEXISTS tinyint;
	SELECT @KILLEREXISTS = COUNT(@KILLERSTEAM) FROM [dbo].[MatchesResultsLive] WHERE ID = @ID;
	SELECT @KILLEDEXISTS = COUNT(@KILLEDSTEAM) FROM [dbo].[MatchesResultsLive] WHERE ID = @ID;

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

	UPDATE [dbo].[Players] SET KILLS = 1 + KILLS, HEADSHOTS = @KILLERHS + HEADSHOTS WHERE STEAMID = @KILLERSTEAM;
	UPDATE [dbo].[Players] SET DEATHS = 1 + DEATHS WHERE STEAMID = @KILLEDSTEAM;
END
GO


