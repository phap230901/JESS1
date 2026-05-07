using AndroidAutomationSuite.WinForms.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace AndroidAutomationSuite.WinForms.Infrastructure.Data;

public sealed class SqliteDatabaseService : IDatabaseService
{
    private readonly string _connString = "Data Source=automation.db";

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS devices(id INTEGER PRIMARY KEY, serial TEXT UNIQUE, state TEXT, last_seen_utc TEXT);
CREATE TABLE IF NOT EXISTS logs(id INTEGER PRIMARY KEY, level TEXT, message TEXT, created_utc TEXT);
CREATE TABLE IF NOT EXISTS workflows(id INTEGER PRIMARY KEY, name TEXT, json TEXT, created_utc TEXT);
CREATE TABLE IF NOT EXISTS profiles(id INTEGER PRIMARY KEY, name TEXT, payload TEXT, created_utc TEXT);";
        cmd.ExecuteNonQuery();
    }
}
