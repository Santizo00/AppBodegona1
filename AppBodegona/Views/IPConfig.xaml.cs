using AppBodegona.Services;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IPConfig : ContentPage
    {

        // Sobrescribe el método OnBackButtonPressed
        protected override bool OnBackButtonPressed()
        {
            // Muestra un mensaje de confirmación
            Device.BeginInvokeOnMainThread(async () =>
            {
                bool result = await this.DisplayAlert(
                    "Confirmar",
                    "¿Desea salir?",
                    "Sí",
                    "No");

                if (result)
                {
                    // Si el usuario elige 'Sí', cierra la aplicación
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                }
            });

            // Devuelve true para indicar que hemos manejado el evento
            // y evitar que la aplicación cierre automáticamente
            return true;
        }

        // Variables públicas para almacenar los valores
        public static string ServerAddress { get; private set; }
        public static string PortNumber { get; private set; }
        public static string DatabaseName { get; private set; }
        public static string Username { get; private set; }
        public static string Password { get; private set; }

        public static class GlobalValues
        {
            public static int IDSucursal { get; set; }
        }

        public IPConfig()
        {
            InitializeComponent();

            // Recuperar las preferencias al iniciar
            Server.Text = Preferences.Get("ServerAddress", string.Empty);
            Port.Text = Preferences.Get("PortNumber", string.Empty);
            Database.Text = Preferences.Get("DatabaseName", string.Empty);
            User.Text = Preferences.Get("Username", string.Empty);
            Pass.Text = Preferences.Get("Password", string.Empty);

            Save.Clicked += Save_Clicked;
        }

        private async void Save_Clicked(object sender, EventArgs e)
        {
            // Asigna los valores de las entradas a las variables públicas
            ServerAddress = Server.Text;
            PortNumber = Port.Text;
            DatabaseName = Database.Text;
            Username = User.Text;
            Password = Pass.Text;

            // Probar la conexión antes de guardar
            var connectionString = $"Server={ServerAddress};Port={PortNumber};Database={DatabaseName};Uid={Username};Pwd={Password};";
            if (DatabaseConnection.TestConnection(connectionString))
            {
                // Actualiza la cadena de conexión globalmente
                DatabaseConnection.UpdateConnectionString(ServerAddress, PortNumber, DatabaseName, Username, Password);

                // Guarda las preferencias
                SavePreferences();

                TipoSucursal();
            }
            else
            {
                // Muestra un mensaje de error
                await DisplayAlert("Error", "No se pudo conectar a la base de datos. Por favor, verifique los datos ingresados.", "OK");
            }
        }

        public async void TipoSucursal()
        {
            string query = "SELECT NombreSucursal, Rotulacion FROM sucursales_creditos WHERE TipoSucursal = 1";

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string tipoSucursalStr = reader["Rotulacion"].ToString();
                            string nombreSucursal = reader["NombreSucursal"].ToString();

                            try
                            {
                                // Imprimir el valor de tipoSucursalStr para depuración
                                System.Diagnostics.Debug.WriteLine($"Rotulacion: {tipoSucursalStr}");

                                // Verificar si tipoSucursalStr no es nulo o vacío
                                if (string.IsNullOrWhiteSpace(tipoSucursalStr))
                                {
                                    await DisplayAlert("Error", "El campo Rotulacion está vacío.", "OK");
                                    return;
                                }

                                // Convertir Rotulacion a entero
                                int tipoSucursal = Convert.ToInt32(tipoSucursalStr);

                                // Asigna el valor a la propiedad estática
                                GlobalValues.IDSucursal = tipoSucursal;

                                // Guardar en las preferencias
                                Preferences.Set("ID_Sucursal", tipoSucursal);

                                await DisplayAlert("Éxito", "Conexión exitosa a la sucursal: " + nombreSucursal, "Aceptar");

                                // Navegar a la página Existencia
                                await Shell.Current.GoToAsync("///Existencia");

                                // Obtener la página Existencia actual y actualizar la imagen
                                var existenciaPage = (Existencia)Shell.Current.CurrentPage;
                                existenciaPage.UpdateImage();

                                var precioPage = (Precio)Shell.Current.CurrentPage;
                                precioPage.UpdateImage1();

                                var familiaPage = (Familias)Shell.Current.CurrentPage;
                                familiaPage.UpdateImage2();
                            }
                            catch (FormatException)
                            {
                                await DisplayAlert("Error", "El tipo de sucursal no es un número válido.", "OK");
                            }
                            catch (InvalidCastException)
                            {
                            }
                        }
                        else
                        {
                            await DisplayAlert("Alerta", "Sucursal no encontrada.", "Aceptar");
                        }
                    }
                }
            }
        }


        private void SavePreferences()
        {
            // Guarda las preferencias usando Xamarin.Essentials
            Preferences.Set("ServerAddress", ServerAddress);
            Preferences.Set("PortNumber", PortNumber);
            Preferences.Set("DatabaseName", DatabaseName);
            Preferences.Set("Username", Username);
            Preferences.Set("Password", Password);
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///Existencia");
        }
    }
}
