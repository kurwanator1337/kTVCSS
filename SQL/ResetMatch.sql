USE [kTVCSS]
GO
/****** Object:  StoredProcedure [dbo].[ResetMatch]    Script Date: 27.02.2022 12:45:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ResetMatch]
	@MATCHID int,
	@SERVERID int
AS
BEGIN
	DELETE FROM [dbo].[MatchesLive] WHERE ID = @MATCHID
	UPDATE [dbo].[GameServers] SET BUSY = 0 WHERE ID = @SERVERID
END
