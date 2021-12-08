USE [kTVCSS]
GO

/****** Object:  StoredProcedure [dbo].[UpdateMatchScore]    Script Date: 08.12.2021 23:13:46 ******/
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
	UPDATE [dbo].[MatchesLive] SET ASCORE = @ASCORE, BSCORE = @BSCORE WHERE SERVERID = @SERVERID AND ID = @ID AND FINISHED = 0
END
GO


