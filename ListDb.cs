using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.crrfhaaqeainuqzkmged;Password=shreshtlibrary;Pooling=true;";
        using (var conn = new NpgsqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT key, value FROM core_globalsetting;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Key: {reader["key"]}");
                }
            }
        }
    }
}
