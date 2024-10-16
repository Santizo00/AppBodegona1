using AppBodegona.Views;
using System;
using System.Timers;
using Xamarin.Forms;

namespace AppBodegona
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        private const int InactivityThreshold = 1200000;
        private Timer _timer;
        private DateTime _lastInteractionTime;

        public static readonly BindableProperty UsuarioProperty =
        BindableProperty.Create(nameof(Usuario), typeof(string), typeof(AppShell), default(string));
        public string Usuario
        {
            get => (string)GetValue(UsuarioProperty);
            set => SetValue(UsuarioProperty, value);
        }

        public static readonly BindableProperty IdProperty =
        BindableProperty.Create(nameof(Id), typeof(string), typeof(AppShell), default(string));
        public new string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public static readonly BindableProperty IsLoggedInProperty =
        BindableProperty.Create(nameof(IsLoggedIn), typeof(bool), typeof(AppShell), default(bool), propertyChanged: OnIsLoggedInChanged);
        public bool IsLoggedIn
        {
            get => (bool)GetValue(IsLoggedInProperty);
            set => SetValue(IsLoggedInProperty, value);
        }

        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
            StartInactivityTimer();
            Usuario = string.Empty;
            IsLoggedIn = false;
            BindingContext = this;
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute("Login", typeof(Login));
            Routing.RegisterRoute("IPConfig", typeof(IPConfig));
        }

        private void StartInactivityTimer()
        {
            _timer = new Timer();
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
        }

        private static void OnIsLoggedInChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var shell = (AppShell)bindable;
            bool isLoggedIn = (bool)newValue;

            if (isLoggedIn)
            {
                shell.ResetInactivityTimer();
            }
            else
            {
                shell._timer.Stop();
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (IsLoggedIn && (DateTime.Now - _lastInteractionTime).TotalMilliseconds > InactivityThreshold)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    _timer.Stop();
                    IsLoggedIn = false;
                    Usuario = string.Empty;
                    Id = string.Empty;
                    await DisplayAlert("Alerta", "Sesión finalizada por inactividad", "Ok");
                    await Shell.Current.GoToAsync($"//Existencia");
                });
            }
        }

        public void ResetInactivityTimer()
        {
            if (IsLoggedIn)
            {
                _lastInteractionTime = DateTime.Now;
                _timer.Start();
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            IsLoggedIn = false;
            Usuario = string.Empty;
            Id = string.Empty;
            await Shell.Current.GoToAsync($"//Existencia");
        }

        private void Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsLoggedIn = true;
        }
    }

    public static class NavigationService
    {
        private static string _destinationPage;

        public static string DestinationPage
        {
            get => _destinationPage;
            set => _destinationPage = value;
        }
    }
}
