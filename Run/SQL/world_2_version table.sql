/****** Object:  Table [dbo].[ZepheusVersion]    Script Date: 07/13/2011 15:24:23 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ZepheusVersion](
	[Version] [int] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[ZepheusVersion] ADD  CONSTRAINT [DF_ZepheusVersion_Version]  DEFAULT ((0)) FOR [Version]
GO


