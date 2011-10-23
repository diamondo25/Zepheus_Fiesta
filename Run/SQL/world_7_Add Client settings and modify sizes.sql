ALTER TABLE dbo.[Character] ALTER COLUMN QuickBar binary(1030) NULL
GO
ALTER TABLE dbo.[Character] ALTER COLUMN QuickBarState binary(30) NULL
GO
ALTER TABLE dbo.[Character] ALTER COLUMN ShortCuts binary(310) NULL
GO
ALTER TABLE dbo.[Character] ALTER COLUMN GameSettings binary(1030) NULL
GO
ALTER TABLE dbo.[Character] ADD ClientSettings binary(395) NULL
GO