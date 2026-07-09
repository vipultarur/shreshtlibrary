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
            using (var cmd = new NpgsqlCommand("DELETE FROM core_globalsetting WHERE key IN ('smtp_host', 'smtp_port', 'smtp_user', 'smtp_pass', 'smtp_from_name', 'smtp_from_email');", conn))
            {
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"Deleted {rows} old SMTP rows.");
            }
        }
    }
}
