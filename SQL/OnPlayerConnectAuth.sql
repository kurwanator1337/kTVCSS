USE [kTVCSS]
GO

/****** Object:  StoredProcedure [dbo].[OnPlayerConnectAuth]    Script Date: 06.12.2021 2:27:53 ******/
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
	-- Add the parameters for the stored procedure here
	@NAME nvarchar(30),
	@STEAMID nvarchar(20)
AS
BEGIN
	DECLARE @COUNT int
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


