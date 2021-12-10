USE [kTVCSS]
GO

/****** Object:  Table [dbo].[MatchesResults]    Script Date: 10.12.2021 21:35:14 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MatchesResults](
	[ID] [int] NOT NULL,
	[TEAMNAME] [nvarchar](20) NOT NULL,
	[NAME] [nvarchar](30) NOT NULL,
	[STEAMID] [nvarchar](30) NOT NULL,
	[KILLS] [int] NOT NULL,
	[DEATHS] [int] NOT NULL,
	[HEADSHOTS] [int] NOT NULL,
	[SERVERID] [int] NOT NULL
) ON [PRIMARY]
GO


