IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{Name}]') AND type in (N'V', N'VIEW'))
DROP VIEW [dbo].[{Name}]
GO
CREATE VIEW [dbo].[{Name}]
AS
SELECT
	*
FROM {Name}