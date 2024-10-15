IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{Name}]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[{Name}]
GO
CREATE PROCEDURE [dbo].[{Name}] 
	@ID int
AS
SELECT 
	ID
FROM {Name}
WHERE ID = ISNULL(@ID, ID)