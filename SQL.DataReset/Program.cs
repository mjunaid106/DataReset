using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SQL.DataReset
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
            {
                var conn = new SqlConnection(css.ConnectionString);
                using (conn)
                {
                    Console.WriteLine($"Deleting data for connection: {css.Name}{Environment.NewLine}");
                    try
                    {
                        conn.Open();
                        var tables = GetAllTableNames(conn).ToList();
                        DisableAllContraints(tables, conn);
                        DeleteData(tables, conn);
                        EnableAllConstaints(tables, conn);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to access the database");
                        Console.WriteLine(ex.Message);
                    }
                    Console.WriteLine($"{Environment.NewLine}Data deleted");
                    Console.WriteLine($"-------------------------{Environment.NewLine}");

                }
            }
            Console.Read();
        }

        private static void EnableAllConstaints(IEnumerable<string> tables, SqlConnection conn)
        {
            foreach (var table in tables)
            {
                var commandText = $"begin declare @sql nvarchar(2000) SELECT TOP 1 @sql=(\'ALTER TABLE {table} CHECK CONSTRAINT ALL\') FROM information_schema.tables exec (@sql) end";
                var cmd = new SqlCommand(commandText) { CommandType = CommandType.Text, Connection = conn };
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteData(IEnumerable<string> tables, SqlConnection conn)
        {
            foreach (var table in tables)
            {
                var commandText = $"begin declare @sql nvarchar(2000) SELECT TOP 1 @sql=(\'DELETE FROM {table}\') FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME != \'__MigrationHistory\' exec (@sql) end";
                var cmd = new SqlCommand(commandText) { CommandType = CommandType.Text, Connection = conn };
                cmd.ExecuteNonQuery();
                Console.WriteLine($"Deleted table: {table}");
            }
        }

        private static void DisableAllContraints(IEnumerable<string> tables, SqlConnection conn)
        {
            foreach (var table in tables)
            {
                var commandText = $"begin declare @sql nvarchar(2000) SELECT TOP 1 @sql=(\'ALTER TABLE {table} NOCHECK CONSTRAINT ALL\') FROM information_schema.tables exec (@sql) end";
                var cmd = new SqlCommand(commandText) { CommandType = CommandType.Text, Connection = conn };
                cmd.ExecuteNonQuery();
            }
        }

        private static IEnumerable<string> GetAllTableNames(SqlConnection conn)
        {
            var commandText = "select table_schema + '.' + table_name from INFORMATION_SCHEMA.TABLES where TABLE_NAME != '__MigrationHistory' and table_schema != 'sys'";
            var cmd = new SqlCommand(commandText) { CommandType = CommandType.Text, Connection = conn };
            var reader = cmd.ExecuteReader();
            var tables = new List<string>();
            while (reader.Read())
            {
                tables.Add(reader[0].ToString());
            }
            reader.Close();
            return tables;
        }
    }
}
