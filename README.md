# sqlM
sqlM is an opinionated C# relational database tool. It simplifies interaction with a SQL database by automatically generating the boilerplate integration and mapping code. This is done in a declarative way to simplify SQL migrations and allow real source control of database changes.

This is done using simple SQL files on disk as a template. So, if you wanted to query the customer table in your database you would create a new SQL file in the query folder and add in a simple SQL query, something like this.

``` SQL
SELECT
	 ID
	,Email
	,PostalAddress
FROM Customer
```

Then once the SQL query is working you call sqlM with the scaffold parameter to generate the C# boilerplate code.

``` CMD
sqlm scaffold
```

This will generate a new class that allows database access. The new class will contain a method named the same as the SQL file you created. To run the query from your code, create a new instance of the database class, passing in a connection string, and then call the query method. Like this.

``` C#
using sqlM;

Database db = new Database("Data Source=localhost;Initial Catalog=MyDatabase;Integrated Security=SSPI;TrustServerCertificate=True;");

List<Customer_Result> customers = db.Customer();

foreach (Customer_Result customer in customers)
{
	Console.WriteLine($"{customer.Email} {customer.PostalAddress}");
}
```

That will then print out your customer list to the console.

## Parameters 

SQL queries are not all that useful without the ability to input data. Data input is done through parameters. Lets change our customer query to take in an ID parameter to filter the results down to a specific customer. To do this we will need to add some section headers to let sqlM know which parts of the query are declaring the parameters and which parts are the main query.

``` SQL
--- PARAMS ---
DECLARE @ID INT
--- MAIN ---
SELECT
	 ID
	,Email
	,PostalAddress
FROM Customer
WHERE 
	ID = @ID
```

If we rerun `sqlm scaffold` our `Customer()` method will now take an integer parameter called ID.

## Testing

It can be helpful to test a script a little before adding it to the application. Sometimes leaving the test code there can help next time you update it. Don't worry about commenting the test code out just put it under a test header and sqlM will ignore it during the scaffolding process.

This SQL file will set a test value for @ID to make sure the query returns the data you are expecting from your database, but will generate the same C# code as the parameter example above.

``` SQL
--- PARAMS ---
DECLARE @ID INT
--- TEST ---
SET @ID = 3
--- MAIN ---
SELECT
	 ID
	,Email
	,PostalAddress
FROM Customer
WHERE 
	ID = @ID
```

## Naming

Sometimes you want the result class to be named differently to the file, this can by done by setting the typename value in a SQL comment. So if we want our method to be called `Customer_Get` and the return type to be called `Customer` we just need to rename the query file to `Customer_Get.sql` and add `Customer` typename under the names header.

``` SQL
--- PARAMS ---
DECLARE @ID INT
--- NAMES ---
-- typename = Customer
--- TEST ---
SET @ID = 3
--- MAIN ---
SELECT
	 ID
	,Email
	,PostalAddress
FROM Customer
WHERE 
	ID = @ID
```



