USE [kTVCSS]
GO

/****** Object:  Table [dbo].[Ranks]    Script Date: 06.12.2021 2:27:03 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Ranks](
	[NAME] [nvarchar](10) NOT NULL,
	[STARTMMR] [int] NOT NULL,
	[ENDMMR] [int] NOT NULL
) ON [PRIMARY]
GO


