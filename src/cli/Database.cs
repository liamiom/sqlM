using Microsoft.Data.SqlClient;

namespace sqlM;
public partial class Database
{
    public static bool TestConnectionString(string connectionString)
    {
        try
        {
            SqlConnection dbConnection = new(connectionString);
            dbConnection.Open();

            return true;
        }
        catch
        {
            Console.WriteLine($"Unable to connect using \"{connectionString}\"");
            return false;
        }
    }

    public static List<string> GetDatabaseNames(string connectionString)
    {
        string script = "SELECT name FROM master.dbo.sysdatabases";
        List<string> output = new();
        SqlConnection dbConnection = new(connectionString);
        dbConnection.Open();

        SqlCommand command = new(script, dbConnection);
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            output.Add(reader.GetString(0));
        }

        reader.Close();

        return output;
    }
}
