USE [kTVCSS]
GO

/****** Object:  Table [dbo].[Players]    Script Date: 27.02.2022 13:09:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MatchesMVP](
	[ID] [int] NOT NULL,
	[MVP]  AS ([dbo].[GetMatchMVP]([ID])),
) ON [PRIMARY]
GO


