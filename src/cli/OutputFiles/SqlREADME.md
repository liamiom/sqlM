# SQL Source Folder

This is the sqlM source folder. sqlM will use the SQL files in this folder to generate data access layer classes in the output folder. Each SQL file will be converted into one C# file along with a reference in Database.cs.

New SQL script files can be added with the Cli using the Add option and then selecting the script type you would like to add.

``` CMD

sqlM Add

```

Once you have the scripts how you want them, the C# code can be generated with the Scaffold option.

``` CMD

sqlM Scaffold

```

Read the README.md file in each type folder for more information on how that type of script works.