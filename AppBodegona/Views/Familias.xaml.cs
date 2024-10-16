using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using AppBodegona.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin.Essentials;
using System.Threading.Tasks;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Familias : ContentPage
    {
        public ObservableCollection<Familia> Family { get; set; }
        public ObservableCollection<Producto> Productos { get; set; } = new ObservableCollection<Producto>();
        public ObservableCollection<ProductoFamilia> ProductosFamilia { get; set; } = new ObservableCollection<ProductoFamilia>();

        public Familias()
        {
            InitializeComponent();
            UpdateImage2();
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

            Family = new ObservableCollection<Familia>();
            Productos = new ObservableCollection<Producto>();
            ProductosFamilia = new ObservableCollection<ProductoFamilia>();
            ResultadosListView.ItemsSource = Family;
            FamiliaListView.ItemsSource = Productos;
            ProductoListView.ItemsSource = ProductosFamilia;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
            };
            MainContentView.GestureRecognizers.Add(tapGestureRecognizer);
        }
        public void UpdateImage2()
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
                UpdateImage2();
                bool loginistrue = await DisplayAlert("Advertencia", "Para continuar, inicie sesión", "Aceptar", "Cancelar");
                if (!loginistrue)
                {
                    await Shell.Current.GoToAsync($"//{nameof(Existencia)}");
                }
                else
                {
                    NavigationService.DestinationPage = "Familias";
                    await Shell.Current.GoToAsync("Login");
                }
                return;
            }
        }

        public class Familia
        {
            public string Codigo { get; set; }
            public string Nombre { get; set; }
        }

        private void Cambio_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Family.Clear();
            cfamilia.Text = "";
            nfamilia.Text = "";
            ResultadosListView.IsVisible = false;
            Img.IsVisible = true;

            if (cfamilia.IsVisible)
            {
                cfamilia.IsVisible = false;
                nfamilia.IsVisible = true;
            }
            else
            {
                cfamilia.IsVisible = true;
                nfamilia.IsVisible = false;
            }
        }

        private void Limpiar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Img.IsVisible = true;
            cfamilia.Text = "";
            nfamilia.Text = "";
            ResultadosListView.IsVisible = false;
        }

        private async void Buscar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear la instancia del popup

            try
            {
                // Mostrar el spinner al inicio
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                ResultadosListView.IsVisible = true;
                Family.Clear();

                if (!string.IsNullOrEmpty(cfamilia.Text))
                {
                    string entryidText = cfamilia.Text;
                    string query = $"SELECT Id, Nombre FROM gruposproductos WHERE Id = '{entryidText}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    Family.Add(new Familia
                                    {
                                        Codigo = reader["Id"].ToString(),
                                        Nombre = reader["Nombre"].ToString()
                                    });

                                    Img.IsVisible = false;
                                }
                                else
                                {
                                    await DisplayAlert("Alerta", "Familia no encontrada.", "Aceptar");
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(nfamilia.Text))
                {
                    string entryDescText = nfamilia.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Id, Nombre FROM gruposproductos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"Nombre LIKE @searchTerm{i}";
                        if (i < searchTerms.Length - 1)
                        {
                            query += " AND ";
                        }
                    }
                    query += " ORDER BY Id Asc";

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
                                Family.Clear();
                                while (reader.Read())
                                {
                                    Family.Add(new Familia
                                    {
                                        Codigo = reader["Id"].ToString(),
                                        Nombre = reader["Nombre"].ToString()
                                    });
                                    Img.IsVisible = false;
                                }
                                if (Family.Count == 0)
                                {
                                    await DisplayAlert("Alerta", "No se encontraron familias con esa descripción.", "Aceptar");
                                }
                            }
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Alerta", "No hay datos para buscar.", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
            finally
            {
                // Ocultar el spinner al finalizar
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private async void Todo_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear la instancia del popup

            try
            {
                // Mostrar el spinner al inicio
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                ResultadosListView.IsVisible = true;
                Img.IsVisible = false;

                string query = "SELECT Id, Nombre FROM gruposproductos ORDER BY Id Asc";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            Family.Clear();
                            while (reader.Read())
                            {
                                Family.Add(new Familia
                                {
                                    Codigo = reader["Id"].ToString(),
                                    Nombre = reader["Nombre"].ToString()
                                });
                            }
                            if (Family.Count == 0)
                            {
                                await DisplayAlert("Alerta", "No se encontraron familias.", "Aceptar");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message, "OK");
            }
            finally
            {
                // Ocultar el spinner al finalizar
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Costo { get; set; }
            public string Existencia { get; set; }
        }

        public class ProductoFamilia
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Costo { get; set; }
            public string Existencia { get; set; }
        }

        private async void EditarFamilia(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (sender is Button button && button.BindingContext is Familia familia)
            {
                string id = familia.Codigo;
                var familiaFiltrada = Family.FirstOrDefault(f => f.Codigo == id);
                if (familiaFiltrada != null)
                {
                }

                try
                {
                    string query = $"SELECT Id, Nombre FROM gruposproductos WHERE Id = @Id";
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string Id = reader["Id"].ToString();
                                    string nombreString = reader["Nombre"].ToString();

                                    codigof.Text = Id;
                                    descf.Text = nombreString;
                                }
                            }
                        }
                    }

                    string query1 = $"SELECT Upc, DescLarga, Costo FROM productos WHERE GruposProductosId = '{id}'";
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query1, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                Productos.Clear();
                                while (reader.Read())
                                {
                                    Productos.Add(new Producto
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Costo = Convert.ToDecimal(reader["Costo"]).ToString("F2")
                                    });
                                }
                                if (Productos.Count == 0)
                                {
                                    await DisplayAlert("Alerta", "No se encontraron productos con esa descripción.", "Aceptar");
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

        private async void EditarF_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                // Modificar la visibilidad de los elementos
                Img.IsVisible = false;
                VistaBusqueda.IsVisible = false;
                VistaFamilia.IsVisible = true;

                // Llamar a la función EditarFamilia (asegúrate de que esta función sea válida)
                EditarFamilia(sender, e);
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje en caso de error
                await Application.Current.MainPage.DisplayAlert("Error", $"Se produjo un error: {ex.Message}", "OK");
            }
            finally
            {
                // Ocultar el spinner al finalizar la operación
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private async void EliminarF_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                if (sender is Button button && button.BindingContext is Producto producto)
                {
                    string upc = producto.Upc;
                    var productoFiltrado = Productos.FirstOrDefault(p => p.Upc == upc);

                    if (productoFiltrado != null)
                    {
                        bool continueWithNegativeMargins = await DisplayAlert("Advertencia", "¿Desea eliminar el producto de la familia?", "Sí", "No");

                        if (!continueWithNegativeMargins)
                            return;

                        try
                        {
                            string query = $"UPDATE productos SET GruposProductosId = '0' WHERE Upc = '{upc}'";

                            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                            {
                                connection.Open();
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    int result = command.ExecuteNonQuery();

                                    if (result > 0)
                                    {
                                        Productos.Remove(productoFiltrado);
                                        await DisplayAlert("Éxito", "Producto eliminado correctamente", "OK");
                                    }
                                    else
                                    {
                                        await DisplayAlert("Error", "No se pudo eliminar el producto", "OK");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Se produjo un error inesperado: {ex.Message}", "OK");
            }
            finally
            {
                // Ocultar el spinner al finalizar la operación
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private void Regresar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaFamilia.IsVisible = false;
            VistaBusqueda.IsVisible = true;
            Img.IsVisible = false;
        }

        private void Agregar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaAgregarProducto.IsVisible = true;
            VistaFamilia.IsVisible = false;
            Img.IsVisible = true;
            ProductoListView.IsVisible = false;

        }

        private void CambioTextProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductosFamilia.Clear();
            descproduc.Text = "";
            upcproduc.Text = "";
            ProductoListView.IsVisible = false;

            if (descproduc.IsVisible)
            {
                descproduc.IsVisible = false;
                upcproduc.IsVisible = true;
            }
            else
            {
                descproduc.IsVisible = true;
                upcproduc.IsVisible = false;
            }
        }

        private async void BuscarProduc(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                if (!string.IsNullOrEmpty(upcproduc.Text))
                {
                    Img.IsVisible = false;
                    string entryupcText = upcproduc.Text;

                    if (entryupcText.Length < 13)
                    {
                        int cerosToAdd = 13 - entryupcText.Length;
                        entryupcText = new string('0', cerosToAdd) + entryupcText;
                    }

                    string query = $"SELECT Upc, Costo, DescLarga, Existencia FROM productos WHERE Upc = '{entryupcText}' AND GruposProductosId = '0'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ProductosFamilia.Add(new ProductoFamilia
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Costo = reader["Costo"].ToString()
                                    });
                                }
                                else
                                {
                                    await DisplayAlert("Alerta", "Producto no encontrado o ya pertenece a una familia.", "Aceptar");
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(descproduc.Text))
                {
                    Img.IsVisible = false;
                    string entryDescText = descproduc.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Upc, Costo, DescLarga, Existencia FROM productos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"DescLarga LIKE @searchTerm{i} AND ";
                    }
                    query += "GruposProductosId = '0'"; // Solo mostrar productos que no están en ninguna familia
                    query += @"
            ORDER BY 
                CASE 
                    WHEN Existencia > 0 THEN 1
                    WHEN Existencia = 0 THEN 2
                    ELSE 3
                END, 
                Existencia DESC";

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
                                ProductosFamilia.Clear();
                                while (reader.Read())
                                {
                                    ProductosFamilia.Add(new ProductoFamilia
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Costo = reader["Costo"].ToString()
                                    });
                                }
                                if (ProductosFamilia.Count == 0)
                                {
                                    await DisplayAlert("Alerta", "No se encontraron productos con esa descripción o ya pertenecen a una familia.", "Aceptar");
                                }
                            }
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Alerta", "No hay datos para buscar.", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Error al intentar conectar a la base de datos: {ex.Message}", "OK");
            }
            finally
            {
                // Ocultar el spinner al finalizar la operación
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }


        private void BuscarProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductoListView.IsVisible = true;
            ProductosFamilia.Clear();
            BuscarProduc(sender, e);
        }

        private async void EscanearProducto_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            upcproduc.Text = null;
            descproduc.Text = null;
            ProductosFamilia.Clear();

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
                    upcproduc.Text = result.Text;

                    BuscarProduc(sender, e);

                    ProductoListView.IsVisible = true;

                }
                else
                {
                    upcproduc.Text = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void LimpiarProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductosFamilia.Clear();
            descproduc.Text = "";
            upcproduc.Text = "";
            ProductoListView.IsVisible = false;
            Img.IsVisible = true;
        }

        private async void AgregarFamilia_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner al inicio si no está ya abierto
                if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
                }

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                if (sender is Button button && button.BindingContext is ProductoFamilia productoFamilia)
                {
                    string upc = productoFamilia.Upc;
                    string familiaId = codigof.Text;

                    var productoFamiliaFiltrado = ProductosFamilia.FirstOrDefault(p => p.Upc == upc);
                    if (productoFamiliaFiltrado != null)
                    {
                        // Ocultar spinner antes del DisplayAlert
                        await CerrarPopupSiEstaAbierto();

                        bool continueWithNegativeMargins = await DisplayAlert(
                            "Confirmar",
                            $"¿Desea agregar el producto a la familia?\n\nUPC: {upc}\nDescripción: {productoFamiliaFiltrado.DescLarga}",
                            "Sí", "No");

                        if (!continueWithNegativeMargins)
                        {
                            return;
                        }

                        // Volver a mostrar el spinner para la operación
                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                        string updateProductValuesQuery = @"
                                                            UPDATE productos 
                                                            SET 
                                                                Costo = @Costo, 
                                                                Precio = @PrecioNormal, 
                                                                Nivel1 = @Nivel1, 
                                                                PrecioMaxNivel1 = @PrecioMaxNivel1, 
                                                                Nivel2 = @Nivel2, 
                                                                PrecioMaxNivel2 = @PrecioMaxNivel2 
                                                            WHERE Upc = @Upc";

                        string insertHistorialQuery = @"
                                                        INSERT INTO historialcambios (IdUsuarios, Usuario, Fecha, FechaHora, Upc, TipoCambio, DoubleAnt, DoubleAct) 
                                                        VALUES (@IdUsuario, @Usuario, @Fecha, @FechaHora, @Upc, @TipoCambio, @DoubleAnt, @DoubleAct)";

                        string updateProductQuery = $"UPDATE productos SET GruposProductosId = @FamiliaId WHERE Upc = @Upc";
                        string productoQuery = $"SELECT Precio, Costo, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM productos WHERE Upc = @Upc";
                        string familiaQuery = $"SELECT PrecioNormal, Costo, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM gruposproductos WHERE ID = @FamiliaId";

                        using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                        {
                            connection.Open();

                            using (MySqlTransaction transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    // Obtener datos del producto
                                    var productoDatos = new Dictionary<string, double>();
                                    using (MySqlCommand productoCommand = new MySqlCommand(productoQuery, connection, transaction))
                                    {
                                        productoCommand.Parameters.AddWithValue("@Upc", upc);
                                        using (MySqlDataReader productoReader = productoCommand.ExecuteReader())
                                        {
                                            if (productoReader.Read())
                                            {
                                                productoDatos["Precio"] = Convert.ToDouble(productoReader["Precio"]);
                                                productoDatos["Costo"] = Convert.ToDouble(productoReader["Costo"]);
                                                productoDatos["Nivel1"] = Convert.ToDouble(productoReader["Nivel1"]);
                                                productoDatos["PrecioMaxNivel1"] = Convert.ToDouble(productoReader["PrecioMaxNivel1"]);
                                                productoDatos["Nivel2"] = Convert.ToDouble(productoReader["Nivel2"]);
                                                productoDatos["PrecioMaxNivel2"] = Convert.ToDouble(productoReader["PrecioMaxNivel2"]);
                                                productoDatos["Nivel3"] = Convert.ToDouble(productoReader["Nivel3"]);
                                                productoDatos["PrecioMaxNivel3"] = Convert.ToDouble(productoReader["PrecioMaxNivel3"]);
                                            }
                                        }
                                    }

                                    // Obtener datos de la familia
                                    var familiaDatos = new Dictionary<string, double>();
                                    using (MySqlCommand familiaCommand = new MySqlCommand(familiaQuery, connection, transaction))
                                    {
                                        familiaCommand.Parameters.AddWithValue("@FamiliaId", familiaId);
                                        using (MySqlDataReader familiaReader = familiaCommand.ExecuteReader())
                                        {
                                            if (familiaReader.Read())
                                            {
                                                familiaDatos["PrecioNormal"] = Convert.ToDouble(familiaReader["PrecioNormal"]);
                                                familiaDatos["Costo"] = Convert.ToDouble(familiaReader["Costo"]);
                                                familiaDatos["Nivel1"] = Convert.ToDouble(familiaReader["Nivel1"]);
                                                familiaDatos["PrecioMaxNivel1"] = Convert.ToDouble(familiaReader["PrecioMaxNivel1"]);
                                                familiaDatos["Nivel2"] = Convert.ToDouble(familiaReader["Nivel2"]);
                                                familiaDatos["PrecioMaxNivel2"] = Convert.ToDouble(familiaReader["PrecioMaxNivel2"]);
                                                familiaDatos["Nivel3"] = Convert.ToDouble(familiaReader["Nivel3"]);
                                                familiaDatos["PrecioMaxNivel3"] = Convert.ToDouble(familiaReader["PrecioMaxNivel3"]);
                                            }
                                        }
                                    }

                                    // Comparar y registrar cambios
                                    var diferencias = new List<(int TipoCambio, double ValorAnterior, double ValorNuevo)>();
                                    if (productoDatos["Costo"] != familiaDatos["Costo"])
                                        diferencias.Add((12, productoDatos["Costo"], familiaDatos["Costo"]));

                                    if (productoDatos["Precio"] != familiaDatos["PrecioNormal"])
                                        diferencias.Add((13, productoDatos["Precio"], familiaDatos["PrecioNormal"]));

                                    if (productoDatos["Nivel1"] != familiaDatos["Nivel1"])
                                        diferencias.Add((14, productoDatos["Nivel1"], familiaDatos["Nivel1"]));

                                    if (productoDatos["PrecioMaxNivel1"] != familiaDatos["PrecioMaxNivel1"])
                                        diferencias.Add((15, productoDatos["PrecioMaxNivel1"], familiaDatos["PrecioMaxNivel1"]));

                                    if (productoDatos["Nivel2"] != familiaDatos["Nivel2"])
                                        diferencias.Add((16, productoDatos["Nivel2"], familiaDatos["Nivel2"]));

                                    if (productoDatos["PrecioMaxNivel2"] != familiaDatos["PrecioMaxNivel2"])
                                        diferencias.Add((17, productoDatos["PrecioMaxNivel2"], familiaDatos["PrecioMaxNivel2"]));

                                    // Actualizar producto
                                    using (MySqlCommand updateValuesCommand = new MySqlCommand(updateProductValuesQuery, connection, transaction))
                                    {
                                        updateValuesCommand.Parameters.AddWithValue("@Costo", familiaDatos["Costo"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioNormal", familiaDatos["PrecioNormal"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Nivel1", familiaDatos["Nivel1"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioMaxNivel1", familiaDatos["PrecioMaxNivel1"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Nivel2", familiaDatos["Nivel2"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioMaxNivel2", familiaDatos["PrecioMaxNivel2"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Upc", upc);
                                        updateValuesCommand.ExecuteNonQuery();
                                    }


                                    // Insertar los cambios en la tabla historialcambios
                                    foreach (var (TipoCambio, ValorAnterior, ValorNuevo) in diferencias)
                                    {
                                        using (MySqlCommand historialCommand = new MySqlCommand(insertHistorialQuery, connection, transaction))
                                        {
                                            historialCommand.Parameters.AddWithValue("@IdUsuario", appShell.Id);
                                            historialCommand.Parameters.AddWithValue("@Usuario", appShell.Usuario);
                                            historialCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                                            historialCommand.Parameters.AddWithValue("@FechaHora", DateTime.Now);
                                            historialCommand.Parameters.AddWithValue("@Upc", upc);
                                            historialCommand.Parameters.AddWithValue("@TipoCambio", TipoCambio);
                                            historialCommand.Parameters.AddWithValue("@DoubleAnt", ValorAnterior);
                                            historialCommand.Parameters.AddWithValue("@DoubleAct", ValorNuevo);
                                            historialCommand.ExecuteNonQuery();
                                        }
                                    }

                                    // Actualizar el producto con la familia
                                    using (MySqlCommand updateCommand = new MySqlCommand(updateProductQuery, connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@FamiliaId", familiaId);
                                        updateCommand.Parameters.AddWithValue("@Upc", upc);
                                        updateCommand.ExecuteNonQuery();
                                    }

                                    // Agregar el producto al ListView
                                    Productos.Add(new Producto
                                    {
                                        Upc = upc,
                                        DescLarga = productoFamiliaFiltrado.DescLarga,
                                        Costo = familiaDatos["Costo"].ToString("F2") // Mostrar el costo con 2 decimales
                                    });

                                    transaction.Commit();

                                    VistaAgregarProducto.IsVisible = false;
                                    VistaFamilia.IsVisible = true;

                                    await CerrarPopupSiEstaAbierto();
                                    await DisplayAlert("Éxito", "Producto agregado a la familia.", "OK");
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    await CerrarPopupSiEstaAbierto();
                                    await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await CerrarPopupSiEstaAbierto();
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
        }

        // Helper para cerrar popup
        private async Task CerrarPopupSiEstaAbierto()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }


        private void Volver_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaAgregarProducto.IsVisible = false;
            Img.IsVisible = false;
            VistaFamilia.IsVisible = true;
        }

        private void Reset_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
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
    }
}
