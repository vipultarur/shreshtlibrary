using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services.Caching
{
    /// <summary>
    /// L2 Persistent Disk Cache using SQLite.
    /// Stores serialized JSON payloads with expiration timestamps so cache entries survive app restarts and deploys.
    /// </summary>
    public class SqliteDiskCache : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteDiskCache> _logger;
        private bool _isInitialized;
        private readonly object _initLock = new();

        public SqliteDiskCache(ILogger<SqliteDiskCache> logger, string? dbPath = null)
        {
            _logger = logger;
            dbPath ??= Path.Combine(AppContext.BaseDirectory, "cache.db");
            _connectionString = $"Data Source={dbPath};Cache=Shared;";
            EnsureDatabaseCreated();
        }

        private void EnsureDatabaseCreated()
        {
            if (_isInitialized) return;
            lock (_initLock)
            {
                if (_isInitialized) return;
                try
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = NORMAL;
                        CREATE TABLE IF NOT EXISTS DiskCache (
                            Key TEXT PRIMARY KEY,
                            Value TEXT NOT NULL,
                            ExpiresAt INTEGER NOT NULL
                        );
                        CREATE INDEX IF NOT EXISTS IX_DiskCache_ExpiresAt ON DiskCache(ExpiresAt);
                    ";
                    command.ExecuteNonQuery();
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize SqliteDiskCache WAL database.");
                }
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                EnsureDatabaseCreated();
                var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Value FROM DiskCache WHERE Key = $key AND ExpiresAt > $now LIMIT 1;";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$now", nowUnix);

                var result = await command.ExecuteScalarAsync();
                if (result is string json && !string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SqliteDiskCache read error for key: {Key}", key);
            }
            return default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            try
            {
                EnsureDatabaseCreated();
                var json = JsonSerializer.Serialize(value);
                var expiresAt = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DiskCache (Key, Value, ExpiresAt)
                    VALUES ($key, $value, $expiresAt)
                    ON CONFLICT(Key) DO UPDATE SET Value = $value, ExpiresAt = $expiresAt;
                ";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$value", json);
                command.Parameters.AddWithValue("$expiresAt", expiresAt);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SqliteDiskCache write error for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                EnsureDatabaseCreated();
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM DiskCache WHERE Key = $key;";
                command.Parameters.AddWithValue("$key", key);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SqliteDiskCache delete error for key: {Key}", key);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
