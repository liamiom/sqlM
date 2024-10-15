IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{Name}]'))
DROP FUNCTION [dbo].[{Name}]
GO
CREATE FUNCTION [dbo].[{Name}]
(
)
RETURNS int
AS
BEGIN
	RETURN 1
END