using AppBodegona.Services;
using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Essentials;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Precio : ContentPage
    {
        public ObservableCollection<Producto> Productos { get; set; }
        public List<string> UpcList { get; private set; }

        public Precio()
        {
            InitializeComponent();
            UpdateImage1();

            UpcList = new List<string>();

            try
            {
                bool isConnected = DatabaseConnection.TestConnection(DatabaseConnection.ConnectionString);
                if (isConnected)
                {
                    // Connection successful
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

            Productos = new ObservableCollection<Producto>();
            ResultadosListView.ItemsSource = Productos;

            // Llama a la función para resetear el temporizador de inactividad
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
            };
            MainContentView.GestureRecognizers.Add(tapGestureRecognizer);
        }
        public void UpdateImage1()
        {
            // Leer el valor de ID_Sucursal desde las preferencias
            int idSucursal = Preferences.Get("ID_Sucursal", 0); // 0 es el valor predeterminado

            if (idSucursal == 4)
            {
                Img.Source = "supermercadon.png";
            }
            else
            {
                Img.Source = "bodegona.png";
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

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (string.IsNullOrEmpty(appShell.Usuario))
            {
                UpdateImage1();
                bool loginistrue = await DisplayAlert("Advertencia", "Para continuar, inicie sesión", "Aceptar", "Cancelar");
                if (!loginistrue)
                {
                    await Shell.Current.GoToAsync($"//{nameof(Existencia)}");
                }
                else
                {
                    NavigationService.DestinationPage = "Precio";
                    await Shell.Current.GoToAsync("Login");
                }
            }
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

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Existencia { get; set; }
            public string Precio { get; set; }
        }

        private void Cambio_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Productos.Clear();
            eupc.Text = "";
            edescripcion.Text = "";

            if (eupc.IsVisible)
            {
                eupc.IsVisible = false;
                edescripcion.IsVisible = true;
            }
            else
            {
                eupc.IsVisible = true;
                edescripcion.IsVisible = false;
            }
            ResultadosListView.IsVisible = false;
        }

        private async void BProducto(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio de la operación
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                if (!string.IsNullOrEmpty(eupc.Text))
                {
                    string entryupcText = eupc.Text;

                    if (entryupcText.Length < 13)
                    {
                        int cerosToAdd = 13 - entryupcText.Length;
                        entryupcText = new string('0', cerosToAdd) + entryupcText;
                    }

                    string query = $"SELECT Upc, Precio, DescLarga, Nivel1, PrecioMaxNivel1, Existencia FROM productos WHERE Upc = '{entryupcText}'";

                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                        {
                            connection.Open();
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        Productos.Add(new Producto
                                        {
                                            Upc = reader["Upc"].ToString(),
                                            DescLarga = reader["DescLarga"].ToString(),
                                            Existencia = reader["Existencia"].ToString(),
                                            Precio = reader["Precio"].ToString()
                                        });
                                    }
                                    else
                                    {
                                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                                        await DisplayAlert("Alerta", "Producto no encontrado.", "Aceptar");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                        await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
                        return;
                    }
                }
                else if (!string.IsNullOrEmpty(edescripcion.Text))
                {
                    string entryDescText = edescripcion.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Upc, Precio, Nivel1, PrecioMaxNivel1, DescLarga, Existencia FROM productos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"DescLarga LIKE @searchTerm{i}";
                        if (i < searchTerms.Length - 1)
                        {
                            query += " AND ";
                        }
                    }
                    query += @"
                ORDER BY 
                    CASE 
                        WHEN Existencia > 0 THEN 1
                        WHEN Existencia = 0 THEN 2
                        ELSE 3
                    END, 
                    Existencia DESC";

                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                        {
                            connection.Open();
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                for (int i = 0; i < searchTerms.Length; i++)
                                {
                                    command.Parameters.AddWithValue($"@searchTerm{i}", "%" + searchTerms[i] + "%");
                                }
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    Productos.Clear();
                                    while (reader.Read())
                                    {
                                        Productos.Add(new Producto
                                        {
                                            Upc = reader["Upc"].ToString(),
                                            DescLarga = reader["DescLarga"].ToString(),
                                            Existencia = reader["Existencia"].ToString(),
                                            Precio = reader["Precio"].ToString()
                                        });
                                    }
                                    if (Productos.Count == 0)
                                    {
                                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                                        await DisplayAlert("Alerta", "No se encontraron productos con esa descripción.", "Aceptar");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                        await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
                        return;
                    }
                }
                else
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Ocultar el spinner
                    await DisplayAlert("Alerta", "No hay datos para buscar.", "Aceptar");
                }
            }
            finally
            {
                // Asegurarse de ocultar el spinner en todos los casos
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private void Buscar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Img.IsVisible = false;
            ResultadosListView.IsVisible = true;
            Productos.Clear();
            BProducto(sender, e);
        }

        private async void Escanear_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Img.IsVisible = false;
            eupc.Text = null;
            edescripcion.Text = null;
            Productos.Clear();

            try
            {
                var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                var options = new ZXing.Mobile.MobileBarcodeScanningOptions
                {
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.All_1D }
                };

                // Texto superior e inferior del escáner
                // scanner.TopText = "Escanea el código QR";
                // scanner.BottomText = "Por favor, coloca el código QR en el área de escaneo";    

                var result = await scanner.Scan(options);

                if (result != null)
                {
                    eupc.Text = result.Text;

                    BProducto(sender, e);

                    ResultadosListView.IsVisible = true;

                }
                else
                {
                    eupc.Text = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void Limpiar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Productos.Clear();
            Img.IsVisible = true;
            ResultadosListView.IsVisible = false;
            eupc.Text = "";
            edescripcion.Text = "";
        }

        private async void VerProducto(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            try
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                if (sender is Button button && button.BindingContext is Producto producto)
                {
                    string upc = producto.Upc;

                    VistaBusqueda.IsVisible = false;
                    VistaCambio.IsVisible = true;

                    string query = $"SELECT DescLarga, Precio, Costo, Margen, Nivel1, PrecioMaxNivel1, Margen1, Nivel2, PrecioMaxNivel2, Margen2, GruposProductosId FROM productos WHERE Upc = '{upc}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    upcc.Text = upc;
                                    descripcionc.Text = reader["DescLarga"].ToString();
                                    costoc.Text = Convert.ToDecimal(reader["Costo"]).ToString("F2");
                                    precioc.Text = Convert.ToDecimal(reader["Precio"]).ToString("F2");
                                    margenc.Text = Convert.ToDecimal(reader["Margen"]).ToString("F2");

                                    N1.Text = reader["Nivel1"].ToString();
                                    P1.Text = Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2");
                                    M1.Text = Convert.ToDecimal(reader["Margen1"]).ToString("F2");

                                    N2.Text = reader["Nivel2"].ToString();
                                    P2.Text = Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2");
                                    M2.Text = Convert.ToDecimal(reader["Margen2"]).ToString("F2");

                                    Img.IsVisible = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
            }
            finally
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private async void VerFamilia(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            try
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
                Img.IsVisible = false;

                if (sender is Button button && button.BindingContext is Producto producto)
                {
                    string upc = producto.Upc;
                    string query = $"SELECT GruposProductosId FROM productos WHERE Upc = '{upc}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int gruposProductosId = reader.GetInt32("GruposProductosId");

                                    if (gruposProductosId > 0)
                                    {
                                        await BuscarProductosPorFamilia(gruposProductosId);
                                    }
                                    else
                                    {
                                        await DisplayAlert("Alerta", "La familia del producto es 0", "Aceptar");
                                    }
                                }
                                else
                                {
                                    await DisplayAlert("Alerta", "Producto no encontrado", "Aceptar");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
            }
            finally
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private async Task BuscarProductosPorFamilia(int gruposProductosId)
        {
            var loadingPopup = new LoadingPopup();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            try
            {
                string query = $"SELECT Upc FROM productos WHERE GruposProductosId = {gruposProductosId}";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            UpcList.Clear();
                            while (reader.Read())
                            {
                                UpcList.Add(reader["Upc"].ToString());
                            }

                            if (UpcList.Count == 0)
                            {
                                await DisplayAlert("Información", "No se encontraron productos para esta familia.", "Aceptar");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
            }
            finally
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private async void Editar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            if (sender is Button button && button.BindingContext is Producto producto)
            {
                string upc = producto.Upc;

                string query = $"SELECT GruposProductosId FROM productos WHERE Upc = '{upc}'";

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int gruposProductosId = reader.GetInt32("GruposProductosId");

                                    if (gruposProductosId > 0)
                                    {
                                        await DisplayAlert("Advertencia", "Se modificara la familia y sus productos!", "OK");
                                        VerFamilia(sender, e);
                                    }
                                    else
                                    {
                                        VerProducto(sender, e);
                                    }
                                }
                                else
                                {
                                    await DisplayAlert("Alerta", "Producto no encontrado", "Aceptar");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
                }
            }
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Img.IsVisible = false;
            VistaBusqueda.IsVisible = true;
            VistaCambio.IsVisible = false;
        }

        // ActualizarFamiliaYProductos function to update the products in the family
        private async Task ActualizarFamiliaYProductos(int gruposProductosId, decimal nuevoPrecio, decimal nuevoMargen, int nivel1, decimal precio1, decimal margen1, int nivel2, decimal precio2, decimal margen2)
        {
            string query = @"UPDATE productos SET 
                   Precio = @NuevoPrecio, 
                   Margen = @NuevoMargen,
                   Nivel1 = @Nivel1,
                   PrecioMaxNivel1 = @Precio1,
                   Margen1 = @Margen1,
                   Nivel2 = @Nivel2,
                   PrecioMaxNivel2 = @Precio2,
                   Margen2 = @Margen2
                   WHERE GruposProductosId = @GruposProductosId";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NuevoPrecio", nuevoPrecio);
                        command.Parameters.AddWithValue("@NuevoMargen", nuevoMargen);
                        command.Parameters.AddWithValue("@Nivel1", nivel1);
                        command.Parameters.AddWithValue("@Precio1", precio1);
                        command.Parameters.AddWithValue("@Margen1", margen1);
                        command.Parameters.AddWithValue("@Nivel2", nivel2);
                        command.Parameters.AddWithValue("@Precio2", precio2);
                        command.Parameters.AddWithValue("@Margen2", margen2);
                        command.Parameters.AddWithValue("@GruposProductosId", gruposProductosId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            try
            {
                CalcularPrecioyMargen();

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                string upc = upcc.Text;
                string precioString = precioc.Text;
                string margenString = margenc.Text;

                string nivel1String = N1.Text;
                string precio1String = P1.Text;
                string margen1String = M1.Text;

                string nivel2String = N2.Text;
                string precio2String = P2.Text;
                string margen2String = M2.Text;

                if (string.IsNullOrWhiteSpace(upc) ||
                    string.IsNullOrWhiteSpace(precioString) ||
                    string.IsNullOrWhiteSpace(margenString) ||
                    string.IsNullOrWhiteSpace(nivel1String) ||
                    string.IsNullOrWhiteSpace(precio1String) ||
                    string.IsNullOrWhiteSpace(margen1String) ||
                    string.IsNullOrWhiteSpace(nivel2String) ||
                    string.IsNullOrWhiteSpace(precio2String) ||
                    string.IsNullOrWhiteSpace(margen2String))
                {
                    await DisplayAlert("Error", "Todos los campos deben estar completos", "OK");
                    return;
                }

                int nivel1, nivel2;
                decimal precio, margen, precio1, margen1, precio2, margen2;

                if (!decimal.TryParse(precioString, out precio) ||
                    !decimal.TryParse(margenString, out margen) ||
                    !int.TryParse(nivel1String, out nivel1) ||
                    !decimal.TryParse(precio1String, out precio1) ||
                    !decimal.TryParse(margen1String, out margen1) ||
                    !int.TryParse(nivel2String, out nivel2) ||
                    !decimal.TryParse(precio2String, out precio2) ||
                    !decimal.TryParse(margen2String, out margen2))
                {
                    await DisplayAlert("Error", "Por favor, ingrese valores válidos", "OK");
                    return;
                }

                if (precio <= 0)
                {
                    await DisplayAlert("Error", "El precio principal no puede ser 0", "OK");
                    return;
                }

                if ((nivel1 > 0 || precio1 > 0 || margen1 > 0) && (nivel1 == 0 || precio1 == 0 || margen1 == 0))
                {
                    await DisplayAlert("Error", "Los campos del Nivel 1 no pueden quedar a 0", "OK");
                    return;
                }

                if ((nivel2 > 0 || precio2 > 0 || margen2 > 0) && (nivel2 == 0 || precio2 == 0 || margen2 == 0))
                {
                    await DisplayAlert("Error", "Los campos del Nivel 2 no pueden quedar a 0", "OK");
                    return;
                }

                if (margen < 0 || margen1 < 0 || margen2 < 0)
                {
                    bool continueWithNegativeMargins = await DisplayAlert("Advertencia", "Uno o más márgenes son negativos. ¿Desea continuar?", "Sí", "No");
                    if (!continueWithNegativeMargins)
                    {
                        return;
                    }
                }

                if (nivel1 != 0 && nivel2 != 0 && nivel1 >= nivel2)
                {
                    await DisplayAlert("Error", "El nivel 2 debe de ser mayor al nivel 1", "OK");
                    return;
                }

                if (precio1 != 0 && precio2 != 0 && precio1 <= precio2)
                {
                    await DisplayAlert("Error", "El precio del nivel 2 debe ser menor al precio de nivel 1", "OK");
                    return;
                }

                bool isGroupProduct = upc.Length < 13;
                bool success = false;

                string query = isGroupProduct ?
                    @"UPDATE gruposproductos SET PrecioNormal = @Precio, Margen = @Margen, Nivel1 = @Nivel1, PrecioMaxNivel1 = @Precio1, Margen1 = @Margen1, Nivel2 = @Nivel2, PrecioMaxNivel2 = @Precio2, Margen2 = @Margen2 WHERE Id = @Upc" :
                    @"UPDATE productos SET Precio = @Precio, Margen = @Margen, Nivel1 = @Nivel1, PrecioMaxNivel1 = @Precio1, Margen1 = @Margen1, Nivel2 = @Nivel2, PrecioMaxNivel2 = @Precio2, Margen2 = @Margen2 WHERE Upc = @Upc";

                ValidarCambios();

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Precio", precio);
                        command.Parameters.AddWithValue("@Margen", margen);
                        command.Parameters.AddWithValue("@Nivel1", nivel1);
                        command.Parameters.AddWithValue("@Precio1", precio1);
                        command.Parameters.AddWithValue("@Margen1", margen1);
                        command.Parameters.AddWithValue("@Nivel2", nivel2);
                        command.Parameters.AddWithValue("@Precio2", precio2);
                        command.Parameters.AddWithValue("@Margen2", margen2);
                        command.Parameters.AddWithValue("@Upc", upc);

                        int result = command.ExecuteNonQuery();
                        success = result > 0;

                        if (isGroupProduct && success)
                        {
                            string updateProductosQuery = @"UPDATE productos SET Precio = @NuevoPrecio, Margen = @NuevoMargen, Nivel1 = @Nivel1, PrecioMaxNivel1 = @Precio1, Margen1 = @Margen1, Nivel2 = @Nivel2, PrecioMaxNivel2 = @Precio2, Margen2 = @Margen2 WHERE GruposProductosId = @Upc";
                            using (MySqlCommand updateCommand = new MySqlCommand(updateProductosQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@NuevoPrecio", precio);
                                updateCommand.Parameters.AddWithValue("@NuevoMargen", margen);
                                updateCommand.Parameters.AddWithValue("@Nivel1", nivel1);
                                updateCommand.Parameters.AddWithValue("@Precio1", precio1);
                                updateCommand.Parameters.AddWithValue("@Margen1", margen1);
                                updateCommand.Parameters.AddWithValue("@Nivel2", nivel2);
                                updateCommand.Parameters.AddWithValue("@Precio2", precio2);
                                updateCommand.Parameters.AddWithValue("@Margen2", margen2);
                                updateCommand.Parameters.AddWithValue("@Upc", upc);

                                updateCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }

                if (success)
                {
                    string message = isGroupProduct ? "Familia y productos actualizados correctamente" : "Producto actualizado correctamente";
                    await DisplayAlert("Éxito", message, "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo actualizar", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
            }
            finally
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();

                upcc.Text = descripcionc.Text = costoc.Text = precioc.Text = margenc.Text = null;
                N1.Text = P1.Text = M1.Text = N2.Text = P2.Text = M2.Text = null;

                VistaCambio.IsVisible = false;
                VistaBusqueda.IsVisible = true;
                Img.IsVisible = false;
            }
        }

        public class HistorialCambiosHelper
        {
            public static async Task InsertarHistorialCambio(string upc, int tipocambio, string valorAntiguo, string valorNuevo)
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                string query = "INSERT INTO historialcambios (IdUsuarios, Usuario, Fecha, FechaHora, Upc, TipoCambio, DoubleAnt, DoubleAct) " +
                               "VALUES (@IdUsuario, @Usuario, @Fecha, @FechaHora, @Upc, @TipoCambio, @ValorAntiguo, @ValorNuevo)";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", appShell.Id);
                    command.Parameters.AddWithValue("@Usuario", appShell.Usuario);
                    command.Parameters.AddWithValue("@Upc", upc);
                    command.Parameters.AddWithValue("@TipoCambio", tipocambio);
                    command.Parameters.AddWithValue("@ValorAntiguo", valorAntiguo);
                    command.Parameters.AddWithValue("@ValorNuevo", valorNuevo);
                    command.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                    command.Parameters.AddWithValue("@FechaHora", DateTime.Now);

                    try
                    {
                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Error al insertar historial: " + ex.Message, "OK");
                    }
                }
            }
        }

        public async void ValidarCambios()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string upc = upcc.Text;
            string precioString = precioc.Text;

            string nivel1String = N1.Text;
            string precio1String = P1.Text;

            string nivel2String = N2.Text;
            string precio2String = P2.Text;


            if (decimal.TryParse(precioString, out decimal precio))
            {
                if (upc.Length == 13)
                {
                    await ValidarCambiosProducto(upc, precioString, nivel1String, precio1String, nivel2String, precio2String);
                }
                else if (upc.Length < 13)
                {
                    await ValidarCambiosGrupoProducto(upc, precioString, nivel1String, precio1String, nivel2String, precio2String);
                }
            }
        }

        private async Task ValidarCambiosProducto(string upc, string precioString, string nivel1String, string precio1String, string nivel2String, string precio2String)
        {
            string query1 = $"SELECT Precio, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2 FROM productos WHERE Upc = '{upc}'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query1, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                await CompararYAgregarCambio(upc, 13, Convert.ToDecimal(reader["Precio"]).ToString("F2"), precioString);
                                await CompararYAgregarCambio(upc, 14, reader["Nivel1"].ToString(), nivel1String);
                                await CompararYAgregarCambio(upc, 15, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                await CompararYAgregarCambio(upc, 16, reader["Nivel2"].ToString(), nivel2String);
                                await CompararYAgregarCambio(upc, 17, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
        }

        private async Task ValidarCambiosGrupoProducto(string codigoFamilia, string precioString, string nivel1String, string precio1String, string nivel2String, string precio2String)
        {
            string query2 = $"SELECT PrecioNormal, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM gruposproductos WHERE Id = '{codigoFamilia}'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query2, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Validar y agregar cambios para el grupo de productos (familia)
                                await CompararYAgregarCambio(codigoFamilia, 21, Convert.ToDecimal(reader["PrecioNormal"]).ToString("F2"), precioString);
                                await CompararYAgregarCambio(codigoFamilia, 22, reader["Nivel1"].ToString(), nivel1String);
                                await CompararYAgregarCambio(codigoFamilia, 23, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                await CompararYAgregarCambio(codigoFamilia, 24, reader["Nivel2"].ToString(), nivel2String);
                                await CompararYAgregarCambio(codigoFamilia, 25, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);

                                // Validar y agregar cambios para cada producto en la familia
                                foreach (var upc in UpcList)
                                {
                                    await CompararYAgregarCambio(upc, 13, Convert.ToDecimal(reader["PrecioNormal"]).ToString("F2"), precioString);
                                    await CompararYAgregarCambio(upc, 14, reader["Nivel1"].ToString(), nivel1String);
                                    await CompararYAgregarCambio(upc, 15, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                    await CompararYAgregarCambio(upc, 16, reader["Nivel2"].ToString(), nivel2String);
                                    await CompararYAgregarCambio(upc, 17, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
        }

        private async Task CompararYAgregarCambio(string upc, int tipoCambioId, string valorAntiguo, string valorNuevo)
        {
            if (valorAntiguo != valorNuevo)
            {
                await HistorialCambiosHelper.InsertarHistorialCambio(upc, tipoCambioId, valorAntiguo, valorNuevo);
            }
        }

        public void CalcularPrecioyMargen()
        {
            string costoText = costoc.Text;

            string precioText = precioc.Text;
            string margenText = margenc.Text;

            string precio1Text = P1.Text;
            string margen1Text = M1.Text;

            string precio2Text = P2.Text;
            string margen2Text = M2.Text;

            decimal.TryParse(costoText, out decimal costo);
            decimal.TryParse(precioText, out decimal precio);
            decimal.TryParse(margenText, out decimal margen);
            decimal.TryParse(precio1Text, out decimal precio1);
            decimal.TryParse(margen1Text, out decimal margen1);
            decimal.TryParse(precio2Text, out decimal precio2);
            decimal.TryParse(margen2Text, out decimal margen2);

            if (string.IsNullOrWhiteSpace(precioText) || precio == 0)
            {
                if (string.IsNullOrWhiteSpace(margenText) || margen == 0)
                {
                    // Ambos campos están vacíos o son cero
                    precioc.Text = "0.00";
                    margenc.Text = "0.00";
                }
                else
                {
                    // Margen tiene un valor, calcular el precio basado en el margen y costo
                    decimal precioN = costo / (1 - (margen * 0.01m));
                    precioc.Text = precioN.ToString("F2");
                }
            }
            else
            {
                // Precio tiene un valor, calcular el margen basado en el precio y costo
                decimal margenN = ((precio - costo) / (precio * 0.01m));
                if (margenN > 99)
                {
                    margenc.Text = "99.99";
                }
                else if (margenN < -99)
                {
                    margenc.Text = "-99.99";
                }
                else
                {
                    margenc.Text = margenN.ToString("F2");
                }
            }

            // Repetir lógica para nivel 1
            if (string.IsNullOrWhiteSpace(precio1Text) || precio1 == 0)
            {
                if (string.IsNullOrWhiteSpace(margen1Text) || margen1 == 0)
                {
                    P1.Text = "0.00";
                    M1.Text = "0.00";
                }
                else
                {
                    decimal precio1N = costo / (1 - (margen1 * 0.01m));
                    P1.Text = precio1N.ToString("F2");
                }
            }
            else
            {
                decimal margen1N = ((precio1 - costo) / (precio1 * 0.01m));
                if (margen1N > 99)
                {
                    M1.Text = "99.99";
                }
                else if (margen1N < -99)
                {
                    M1.Text = "-99.99";
                }
                else
                {
                    M1.Text = margen1N.ToString("F2");
                }
            }

            // Repetir lógica para nivel 2
            if (string.IsNullOrWhiteSpace(precio2Text) || precio2 == 0)
            {
                if (string.IsNullOrWhiteSpace(margen2Text) || margen2 == 0)
                {
                    P2.Text = "0.00";
                    M2.Text = "0.00";
                }
                else
                {
                    decimal precio2N = costo / (1 - (margen2 * 0.01m));
                    P2.Text = precio2N.ToString("F2");
                }
            }
            else
            {
                decimal margen2N = ((precio2 - costo) / (precio2 * 0.01m));
                if (margen2N > 99)
                {
                    M2.Text = "99.99";
                }
                else if (margen2N < -99)
                {
                    M2.Text = "-99.99";
                }
                else
                {
                    M2.Text = margen2N.ToString("F2");
                }
            }
        }


        public void CalcularPrecio()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string costoText = costoc.Text;
            string precioText = precioc.Text;
            string precio1Text = P1.Text;
            string precio2Text = P2.Text;

            decimal.TryParse(costoText, out decimal costo);

            CalcularYActualizarMargen(costo, precioText, margenc);
            CalcularYActualizarMargen(costo, precio1Text, M1);
            CalcularYActualizarMargen(costo, precio2Text, M2);
        }

        private void CalcularYActualizarMargen(decimal costo, string precioText, Entry margenEntry)
        {
            if (string.IsNullOrEmpty(precioText) || precioText == "0")
            {
                margenEntry.Text = "0.00";
            }
            else if (decimal.TryParse(precioText, out decimal precio) && precio != 0)
            {
                decimal margen = ((precio - costo) / (precio * 0.01m));
                if (margen > 99)
                {
                    margenEntry.Text = "99.99";
                }
                else if (margen < -99)
                {
                    margenEntry.Text = "-99.99";
                }
                else
                {
                    margenEntry.Text = margen.ToString("F2");
                }
            }
            else
            {
                margenEntry.Text = "0.00";
            }
        }

        public void CalcularMargen()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string costoText = costoc.Text;
            string margenText = margenc.Text;
            string margen1Text = M1.Text;
            string margen2Text = M2.Text;

            decimal.TryParse(costoText, out decimal costo);

            CalcularYActualizarPrecio(costo, margenText, precioc);
            CalcularYActualizarPrecio(costo, margen1Text, P1);
            CalcularYActualizarPrecio(costo, margen2Text, P2);
        }

        private void CalcularYActualizarPrecio(decimal costo, string margenText, Entry precioEntry)
        {
            if (string.IsNullOrEmpty(margenText) || margenText == "0")
            {
                precioEntry.Text = "0.00";
            }
            else if (decimal.TryParse(margenText, out decimal margen) && margen != 0)
            {
                decimal precio = costo / (1 - (margen * 0.01m));
                precioEntry.Text = precio.ToString("F2");
            }
            else
            {
                precioEntry.Text = "0.00";
            }
        }

        public void Precioc_Completed(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            CalcularPrecio();
        }

        private void Margenc_Completed(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            CalcularMargen();
        }

        private void Reset_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
        }

        private void OnPriceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                // Permitir que el campo esté vacío
                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                // Verificar si el nuevo texto es un número válido con decimal y no negativo
                if (!IsValidNonNegativeDecimal(newText))
                {
                    // Si no es válido, deshacer el cambio
                    entry.TextChanged -= OnPriceTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnPriceTextChanged;
                }
            }
        }

        private bool IsValidNonNegativeDecimal(string text)
        {
            // Permitir sólo números no negativos y un punto decimal
            if (decimal.TryParse(text, out decimal result))
            {
                // Verificar que el número no tenga más de 2 decimales y que no sea mayor a 99999
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
                if (decimalPlaces <= 2 && result <= 99999)
                {
                    return result >= 0;
                }
            }
            return false;
        }


        private void OnLevelTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                // Permitir que el campo esté vacío
                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                // Verificar si el nuevo texto es un número entero válido y no negativo
                if (!IsValidNonNegativeInteger(newText))
                {
                    // Si no es válido, deshacer el cambio
                    entry.TextChanged -= OnLevelTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnLevelTextChanged;
                }
            }
        }

        private bool IsValidNonNegativeInteger(string text)
        {
            // Permitir sólo números enteros no negativos
            if (int.TryParse(text, out int result))
            {
                return result >= 0;
            }
            return false;
        }

        private void OnMarginTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                // Permitir que el campo esté vacío
                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                // Verificar si el nuevo texto es un número válido con decimal
                if (!IsValidMargin(newText))
                {
                    // Si no es válido, deshacer el cambio
                    entry.TextChanged -= OnMarginTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnMarginTextChanged;
                }
            }
        }

        private bool IsValidMargin(string text)
        {
            // Permitir números en el rango de -99.99 a +99.99
            if (decimal.TryParse(text, out decimal result))
            {
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
                if (decimalPlaces <= 2 && result >= -99.99m && result <= 99.99m)
                {
                    return true;
                }
            }
            return false;
        }

    }
}