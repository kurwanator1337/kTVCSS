USE [kTVCSS]
GO

/****** Object:  StoredProcedure [dbo].[InsertDemoName]    Script Date: 22.02.2022 1:26:10 ******/
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


