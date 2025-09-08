# Query scripts

Query scripts allow for database queries to be written in as a native SQL file and tested against the database. Then once it is working the query is then converted into a type safe parameterised C# method.

Query scripts can be added with the Cli like so.

``` CMD

sqlM Add Query

```

The query script is broken into four sections.

### Params

This is where you declare any parameters that need to be passed in from the application layer. These can be added using the normal SQL `DECLARE` statement. But must be left as the default values for now.

### Names

This is where the object names can be overridden. The most common one being the name of the type generated to hold the query output. That can be changed by adding in a typename variable line as a comment.

``` SQL

-- typename = NewTypeName

```

### Test

This is where the variables can be set to test values while writing the script. The parameters can be set to any value to help with debugging the script. Any content in this section will be stripped out as part of the scaffolding process.

### Main

This is where the main body of the query goes. Everything here will be included in the generated parameterised SQL query string.

Finally the once the query is complete it can then be converted into C# from the Cli like so.

``` CMD

sqlM Scaffold

```
