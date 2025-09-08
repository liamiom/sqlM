# Table scripts

Table scripts are designed to handle the creation of the database tables as well as addition and modification of columns. A new table script can be added with the Cli like so.
``` CMD
sqlM Add Table 
```

By default the script will generate an entity type with the same name as the table. You can change the name by updating the typename variable in the heading comment. If you don’t want an entity type just remove the typename variable line. 

Likewise sqlM can generate CRUD methods for the table automatically. To enable this just set the CrudMethods variable to `true` 

The variable lines at the top of the script will normally look something like this.
``` SQL
-- TypeName = TableEntityTypeName
-- CrudMethods = false
```