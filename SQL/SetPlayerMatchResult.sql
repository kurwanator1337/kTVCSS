-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE SetPlayerMatchResult
	-- Add the parameters for the stored procedure here
	@STEAMID nvarchar(30),
	@WIN tinyint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	IF (@WIN = 1)
	BEGIN
	UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESWINS = MATCHESWINS + 1, MMR = MMR + 25 WHERE STEAMID = @STEAMID
	END
	ELSE 
	BEGIN
	UPDATE [dbo].[Players] SET MATCHESPLAYED = MATCHESPLAYED + 1, MATCHESLOOSES = MATCHESLOOSES + 1, MMR = MMR - 25 WHERE STEAMID = @STEAMID
	END
END
GO
