﻿using AppBodegona.Services;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using System.Threading.Tasks;
using System.Linq;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IPConfig : ContentPage
    {

        // Sobrescribe el método OnBackButtonPressed
        protected override bool OnBackButtonPressed()
        {
            // Verificar si hay popups abiertos
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                // Deja que el popup maneje el evento
                return base.OnBackButtonPressed();
            }

            // Si no hay popups, ejecuta la lógica personalizada para la página
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

            // Indicar que hemos manejado el evento
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

        private void Save_Clicked(object sender, EventArgs e)
        {
            Guardar();
        }

        public async void Guardar()
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio
                if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
                }

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

                    // Ejecutar TipoSucursal
                    await TipoSucursal();
                }
                else
                {
                    // Ocultar el spinner antes de mostrar el mensaje
                    await CerrarPopupSiEstaAbierto();
                    await DisplayAlert("Error", "No se pudo conectar a la base de datos. Por favor, verifique los datos ingresados.", "OK");
                }
            }
            catch (Exception ex)
            {
                await CerrarPopupSiEstaAbierto();
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        public async Task TipoSucursal()
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

                                if (string.IsNullOrWhiteSpace(tipoSucursalStr))
                                {
                                    await CerrarPopupSiEstaAbierto();
                                    await DisplayAlert("Error", "El campo Rotulacion está vacío.", "OK");
                                    return;
                                }

                                int tipoSucursal = Convert.ToInt32(tipoSucursalStr);

                                GlobalValues.IDSucursal = tipoSucursal;
                                Preferences.Set("ID_Sucursal", tipoSucursal);

                                // Ocultar spinner antes del DisplayAlert
                                await CerrarPopupSiEstaAbierto();

                                await DisplayAlert("Éxito", "Conexión exitosa a la sucursal: " + nombreSucursal, "Aceptar");

                                await Shell.Current.GoToAsync("///Existencia");

                                var existenciaPage = (Existencia)Shell.Current.CurrentPage;
                                existenciaPage.UpdateImage();

                                var precioPage = (Precio)Shell.Current.CurrentPage;
                                precioPage.UpdateImage1();

                                var familiaPage = (Familias)Shell.Current.CurrentPage;
                                familiaPage.UpdateImage2();
                            }
                            catch (FormatException)
                            {
                                await CerrarPopupSiEstaAbierto();
                                await DisplayAlert("Error", "El tipo de sucursal no es un número válido.", "OK");
                            }
                            catch (InvalidCastException)
                            {
                                await CerrarPopupSiEstaAbierto();
                                await DisplayAlert("Error", "Error en la conversión de tipo.", "OK");
                            }
                        }
                        else
                        {
                            await CerrarPopupSiEstaAbierto();
                            await DisplayAlert("Alerta", "Sucursal no encontrada.", "Aceptar");
                        }
                    }
                }
            }
        }

        // Método helper para cerrar el popup si está abierto
        private async Task CerrarPopupSiEstaAbierto()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
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

        private void Text_Completed(object sender, EventArgs e)
        {
            // Verificar si algún campo está vacío
            if (string.IsNullOrWhiteSpace(Server.Text))
            {
                Server.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Port.Text))
            {
                Port.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Database.Text))
            {
                Database.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Text))
            {
                User.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Pass.Text))
            {
                Pass.Focus();
                return;
            }

            // Si todos los campos están llenos, ejecutar la función Guardar()
            Guardar();
        }
    }
}
