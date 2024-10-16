using System;
using Xamarin.Essentials;
using MySqlConnector;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;
using Rg.Plugins.Popup.Services;

public class LoadingPopup : PopupPage
{
    public LoadingPopup()
    {
        // Configurar el contenido del Popup
        Content = new Frame
        {
            Padding = 20,
            CornerRadius = 10,
            BackgroundColor = Color.White, // Fondo blanco
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HasShadow = true, // Sombra para darle visibilidad
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        Color = Color.Black, // Spinner negro
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Cargando...",
                        FontSize = 18,
                        TextColor = Color.Black, // Texto negro
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        // Fondo semitransparente alrededor del popup
        BackgroundColor = Color.FromRgba(0, 0, 0, 0.3);
    }

    // Sobrescribir los eventos para evitar el cierre accidental
    protected override bool OnBackgroundClicked()
    {
        // Retorna true para evitar que el popup se cierre al tocar fuera de él
        return false;
    }

    protected override bool OnBackButtonPressed()
    {
        // Evita que el popup se cierre al presionar el botón de retroceso
        return true;
    }

}


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
