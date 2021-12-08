USE [kTVCSS]
GO

/****** Object:  Table [dbo].[MatchesResultsLive]    Script Date: 08.12.2021 23:12:16 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MatchesResultsLive](
	[ID] [int] NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[KILLS] [int] NOT NULL,
	[DEATHS] [int] NOT NULL,
	[HEADSHOTS] [int] NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO


