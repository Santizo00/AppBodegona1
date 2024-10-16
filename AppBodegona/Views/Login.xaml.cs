using AppBodegona.Services;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Login : ContentPage
    {
        public Entry Entry { get; set; }
        public Button Button { get; set; }

        public Login()
        {
            InitializeComponent();
            try
            {
                bool isConnected = DatabaseConnection.TestConnection(DatabaseConnection.ConnectionString);
                if (isConnected)
                {
                }
                else
                {
                    DisplayAlert("Error de Conexión", "No se pudo conectar a la base de datos.", "OK");
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
        }

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

        private async void OnIngresarClicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio de la operación
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                string usertext = Usuario.Text;
                string passtext = Contraseña.Text;

                if (string.IsNullOrEmpty(usertext) && string.IsNullOrEmpty(passtext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                    await DisplayAlert("Error", "Por favor ingrese su usuario y contraseña.", "OK");
                    return;
                }
                else if (string.IsNullOrEmpty(usertext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                    await DisplayAlert("Error", "Por favor ingrese su usuario.", "OK");
                    Usuario.Focus();
                    return;
                }
                else if (string.IsNullOrEmpty(passtext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                    await DisplayAlert("Error", "Por favor ingrese su contraseña.", "OK");
                    Contraseña.Focus();
                    return;
                }

                string queryUserCheck = "SELECT COUNT(*) FROM usuarios WHERE Usuario = @usuario";
                string query = $"SELECT Id, IdNivel, NombreCompleto FROM usuarios WHERE Usuario = @usuario AND Password = @contraseña";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand commandUserCheck = new MySqlCommand(queryUserCheck, connection))
                    {
                        commandUserCheck.Parameters.AddWithValue("@usuario", usertext);
                        int userCount = Convert.ToInt32(commandUserCheck.ExecuteScalar());

                        if (userCount == 0)
                        {
                            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                            await DisplayAlert("Error", "Usuario incorrecto.", "OK");
                            Usuario.Focus();
                            return;
                        }
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@usuario", usertext);
                        command.Parameters.AddWithValue("@contraseña", passtext);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idNivel = Convert.ToInt32(reader["IdNivel"]);
                                if (idNivel == 11)
                                {
                                    string Nombre = reader["NombreCompleto"].ToString();
                                    string IdUsuario = reader["Id"].ToString();

                                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                                    await DisplayAlert("Bienvenido", $"Hola {Nombre}", "OK");

                                    if (Application.Current.MainPage is AppShell appShell)
                                    {
                                        appShell.Usuario = Nombre;
                                        appShell.Id = IdUsuario;
                                        appShell.IsLoggedIn = true;

                                        if (!string.IsNullOrEmpty(NavigationService.DestinationPage))
                                        {
                                            await Shell.Current.GoToAsync($"///{NavigationService.DestinationPage}");
                                        }
                                        else
                                        {
                                            await Shell.Current.GoToAsync("///Existencia");
                                        }
                                    }

                                    Usuario.Text = string.Empty;
                                    Contraseña.Text = string.Empty;
                                }
                                else if (idNivel < 11)
                                {
                                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                                    await DisplayAlert("Error", "No tiene permiso para ingresar.", "OK");
                                }
                            }
                            else
                            {
                                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                                await DisplayAlert("Error", "Contraseña incorrecta", "OK");
                                Contraseña.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }


        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            Usuario.Text = string.Empty;
            Contraseña.Text = string.Empty;

            await Shell.Current.GoToAsync($"///{NavigationService.DestinationPage}");
        }

        private void Button_Focused(object sender, FocusEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (sender is Entry entry)
            {
                // Selecciona todo el texto
                Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                if (entry.Text != null)
                {
                    Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                }
            }
        }

        private void Usuario_Completed(object sender, EventArgs e)
        {
            VerificarCampos();
        }

        private void Contraseña_Completed(object sender, EventArgs e)
        {
            VerificarCampos();
        }

        private void VerificarCampos()
        {
            if (string.IsNullOrEmpty(Usuario.Text))
            {
                Usuario.Focus();
            }
            else if (string.IsNullOrEmpty(Contraseña.Text))
            {
                Contraseña.Focus();
            }
            else
            {
                OnIngresarClicked(this, EventArgs.Empty);
            }
        }
    }
}
