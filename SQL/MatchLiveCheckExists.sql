USE [kTVCSS]
GO
/****** Object:  StoredProcedure [dbo].[MatchLiveCheckExists]    Script Date: 27.02.2022 13:18:30 ******/
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
