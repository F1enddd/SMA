using SMA.Pages;
using SMA.Helpers;

namespace SMA
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var manifest = Manifest.GetManifest();

            if (manifest.FirstRun)
                MainPage = new NavigationPage(new WelcomePage());
            else
                MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            window.Width = 500;
            window.Height = 700;
            window.MaximumHeight = 700;
            window.MaximumWidth = 700;
            window.MinimumHeight = 700;
            window.MinimumWidth = 400;
            return window;
        }
    }
}
