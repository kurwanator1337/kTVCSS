USE [kTVCSS]
GO

/****** Object:  UserDefinedFunction [dbo].[GetRankName]    Script Date: 06.12.2021 2:28:32 ******/
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


