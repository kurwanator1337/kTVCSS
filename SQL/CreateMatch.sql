USE [kTVCSS]
GO
/****** Object:  StoredProcedure [dbo].[CreateMatch]    Script Date: 27.02.2022 13:17:25 ******/
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
