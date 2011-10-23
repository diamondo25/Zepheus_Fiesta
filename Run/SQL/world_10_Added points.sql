ALTER TABLE dbo.Character ADD
	UsablePoints tinyint NOT NULL CONSTRAINT DF_Character_UsablePoints DEFAULT 0