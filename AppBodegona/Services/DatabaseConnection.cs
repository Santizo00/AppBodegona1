using System;
using Xamarin.Essentials;
using MySqlConnector;

namespace AppBodegona.Services
{
    public static class DatabaseConnection
    {
        public static string ConnectionString { get; private set; }
        public static string ConnectedServer { get; private set; }

        static DatabaseConnection()
        {
            // Configura el ConnectionString usando la configuración almacenada en preferencias
            InitializeConnectionString();
        }

        public static void InitializeConnectionString()
        {
            var server = Preferences.Get("ServerAddress", string.Empty);
            var port = Preferences.Get("PortNumber", string.Empty);
            var database = Preferences.Get("DatabaseName", string.Empty);
            var username = Preferences.Get("Username", string.Empty);
            var password = Preferences.Get("Password", string.Empty);

            if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
                if (TestConnection(connectionString))
                {
                    ConnectionString = connectionString;
                    ConnectedServer = server;
                    return;
                }
            }

            // Si la configuración almacenada falla, dejar ConnectionString vacío o algún valor por defecto
            ConnectionString = string.Empty;
        }

        public static MySqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                InitializeConnectionString();
            }
            return new MySqlConnection(ConnectionString);
        }

        public static bool TestConnection(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void UpdateConnectionString(string server, string port, string database, string username, string password)
        {
            ConnectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            ConnectedServer = server;
        }

        public class ConnectionConfig
        {
            public string Server { get; set; }
            public string Port { get; set; }
            public string Database { get; set; }
            public string Uid { get; set; }
            public string Password { get; set; }
        }
    }
}
