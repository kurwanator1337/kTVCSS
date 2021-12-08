USE [kTVCSS]
GO

/****** Object:  StoredProcedure [dbo].[EndMatch]    Script Date: 08.12.2021 23:13:01 ******/
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
	--DELETE FROM [dbo].[MatchesLive] WHERE SERVERID = @SERVERID
	INSERT INTO [dbo].[Matches] (ID, ANAME, BNAME, ASCORE, BSCORE, MATCHDATE, MAP, SERVERID)
	VALUES (@MATCHID, @ANAME, @BNAME, @ASCORE, @BSCORE, SYSDATETIME(), @MAP, @SERVERID)
	RETURN @MATCHID
END
GO


