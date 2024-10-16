using AppBodegona.Services;
using AppBodegona.Views;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AppBodegona
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected async override void OnStart()
        {
            base.OnStart();

            // Verificar si es la primera vez que se inicia la aplicación
            bool isFirstLaunch = Preferences.Get("IsFirstLaunch", true);

            if (isFirstLaunch)
            {
                // Mostrar la página de configuración
                await MainPage.Navigation.PushAsync(new Views.IPConfig());

                // Establecer el indicador a false para que no se muestre de nuevo
                Preferences.Set("IsFirstLaunch", false);
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        public async Task NavigateToLogin()
        {
            await MainPage.Navigation.PushAsync(new Views.Login());
        }
    }
}
