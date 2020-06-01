using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SideMenu;
using Xamarin.Forms;

namespace SideMenuSample
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnLeftButtonClicked(object sender, EventArgs e)
        {
            SideMenu.State = SideMenuViewState.LeftMenuShown;
        }

        private void OnRightButtonClicked(object sender, EventArgs e)
        {
            SideMenu.State = SideMenuViewState.RightMenuShown;
        }
    }
}
