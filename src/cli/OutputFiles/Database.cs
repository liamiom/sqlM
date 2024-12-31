
// ##########################################################################################
// #                                                                                        #
// #   This file has been generated with the sqlM Cli. Be careful making any                #
// #   changes as changes made to this file will be overridden next time the scaffold       #
// #   code is rebuilt by the Cli.                                                          #
// #                                                                                        #
// #   If you want to extend this class it is recommended to add your code to a partial     #
// #   class in a different folder.                                                         #
// #                                                                                        #
// ##########################################################################################


using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace sqlM;

public partial class Database
{
    private string _connectionString;
    public event EventHandler<UpdateScript> RunningScript;
    public event EventHandler<UpdateScript> ScriptError;

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Database()
    {
        _connectionString = GetConnectionString();
    }

    public virtual string GetConnectionString() =>
        throw new ApplicationException(
            $@"I can't find a ConnectionString, please pass the ConnectionString to the constructor or create a new " +
            "class that inherits from sqlM.Database and overrides the GetConnectionString method. " +
            "This is an example of a class that could work.\n\n" +
            "namespace test;\n\n" +
            "public class DB : sqlM.Database\n" +
            "{\n" +
            "      public override string GetConnectionString() =>\n" +
            "            \"Data Source=localhost;Initial Catalog=DatabaseName;Integrated Security=SSPI;\";\n" +
            "}\n");

    private SqlDataReader Generic_OpenReader(SqlParameter[] parameters, string script)
    {
        SqlConnection conn = new(_connectionString);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = script;
        cmd.Connection = conn;

        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        conn.Open();
        return cmd.ExecuteReader();
    }

    private int Generic_ExecuteNonQuery(SqlParameter[] parameters, string script)
    {
        SqlConnection conn = new(_connectionString);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = script;
        cmd.Connection = conn;

        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    private SqlDataReader Generic_StoredProcedureReader(SqlParameter[] parameters, string name)
    {
        SqlConnection conn = new(_connectionString);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = name;
        cmd.Connection = conn;
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        conn.Open();
        return cmd.ExecuteReader();
    }

    private int Generic_StoredProcedureNonQuery(SqlParameter[] parameters, string name)
    {
        SqlConnection conn = new(_connectionString);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = name;
        cmd.Connection = conn;
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    public bool Update()
    {
        UpdateScript[] updateScripts = Array.Empty<UpdateScript>(); // Database update scripts go here

        SqlConnection conn = new(_connectionString);
        conn.Open();
        SqlTransaction transaction = conn.BeginTransaction();

        foreach (UpdateScript script in updateScripts)
        {
            RunningScript?.Invoke(this, script);

            foreach (string sql in SplitOnGo(script.Content))
            {
                try
                {
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Transaction = transaction;

                    cmd.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    ScriptError?.Invoke(this, script);
                    if (ScriptError == null)
                    {
                        throw;
                    }

                    transaction.Rollback();
                    conn.Close();
                    return false;
                }
            }
        }

        transaction.Commit();
        conn.Close();
        return true;
    }

    private static string[] SplitOnGo(string sql) => 
        Regex.Replace(sql, @"^(\s*GO\s*)+$", "GO", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Split("GO", StringSplitOptions.RemoveEmptyEntries);

    private static SqlParameter NewParameter(string name, object value)
    {
        if (value == null)
        {
            return new SqlParameter(name, DBNull.Value);
        }

        return new SqlParameter(name, value);
    }
}

public static class TypeExtensions
{
    public static byte[] GetByteArray(this SqlDataReader dr, int i) => 
        (byte[])dr[i];
    public static int? GetNullableInt32(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetInt32(i);
    public static DateTime? GetNullableDateTime(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetDateTime(i);
    public static decimal? GetNullableDecimal(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetDecimal(i);
    public static double? GetNullableDouble(this SqlDataReader dr, int i) =>
        dr.IsDBNull(i) ? null : dr.GetDouble(i);
    public static bool? GetNullableBoolean(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetBoolean(i);
    public static byte[]? GetNullableByteArray(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetByteArray(i);
    public static Guid? GetNullableGuid(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetGuid(i);
    public static string? GetNullableString(this SqlDataReader dr, int i) => 
        dr.IsDBNull(i) ? null : dr.GetString(i);
}

public class UpdateScript : EventArgs
{
    public string Name { get; set; }
    public string Content { get; set; }
    public UpdateScript(string name, string content)
    {
        Name = name;
        Content = content;
    }
}