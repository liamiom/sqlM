

-- Add column {columnName} to the {tableName} table.
 IF NOT EXISTS (
	SELECT * FROM dbo.syscolumns 
	INNER JOIN dbo.sysobjects ON dbo.syscolumns.id = dbo.sysobjects.id 
	WHERE 
		    dbo.syscolumns.name = '{columnName}' 
		AND dbo.sysobjects.name = '{tableName}' 
		AND OBJECTPROPERTY(dbo.sysobjects.id, N'IsTable') = 1) 
  ALTER TABLE {tableName} ADD {columnName} {dataType}{dataLength} {nullable}
GO