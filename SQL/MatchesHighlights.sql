USE [kTVCSS]
GO

/****** Object:  Table [dbo].[MatchesHighlights]    Script Date: 22.02.2022 1:25:42 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MatchesHighlights](
	[ID] [int] NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[TRIPPLES] [int] NOT NULL,
	[QUADROS] [int] NOT NULL,
	[RAMPAGES] [int] NOT NULL,
	[OPENFRAGS] [int] NOT NULL
) ON [PRIMARY]
GO


