USE [kTVCSS]
GO

/****** Object:  Table [dbo].[PlayersWeaponKills]    Script Date: 12.12.2021 21:39:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlayersWeaponKills](
	[STEAMID] [nvarchar](30) NOT NULL,
	[WEAPON] [nvarchar](50) NOT NULL,
	[COUNT] [int] NOT NULL
) ON [PRIMARY]
GO


