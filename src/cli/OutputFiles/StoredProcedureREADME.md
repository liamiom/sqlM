# Stored procedure scripts

Stored procedure scripts are designed to handle the create and update of stored procedures in the database.

A stored procedure script can be added with the Cli like so.
``` CMD
sqlM Add StoredProcedure
```

Scaffolding stored procedure script will generate a new type that matches the stored procedure output and a method that takes in the same parameters as the stored procedure and returns a new generic list of the new type.

The method and type are both named to match the script file name. But you can override the type name by adding in a typename variable to a comment at the the topof the script. Like so.
``` SQL
-- TypeName = NewTypeName
```

If the stored procedure return the contents of a table and you would like to use the type generated from the table script simply set the typename to the same name as the table type.