CREATE TABLE [dbo].[Item](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Owner] [int] NOT NULL,
	[ObjectID] [int] NOT NULL,
	[Slot] [smallint] NOT NULL,
	[Amount] [smallint] NOT NULL,
 CONSTRAINT [PK_Item] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[Item]  WITH CHECK ADD  CONSTRAINT [FK_Item_Character] FOREIGN KEY([Owner])
REFERENCES [dbo].[Character] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Item] CHECK CONSTRAINT [FK_Item_Character]
GO