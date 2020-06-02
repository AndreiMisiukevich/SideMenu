using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SideMenuSample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
