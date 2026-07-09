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
            using (var cmd = new NpgsqlCommand("SELECT key, value FROM core_globalsetting WHERE key LIKE '%brevo%';", conn))
            using (var reader = cmd.ExecuteReader())
            {
                bool found = false;
                while (reader.Read())
                {
                    found = true;
                    Console.WriteLine($"Key: {reader["key"]}, Value: {reader["value"]}");
                }
                if (!found) Console.WriteLine("No brevo keys found in DB.");
            }
        }
    }
}
