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

        public object[] Items { get; } =
        {
            new { Color = Color.Red, Text = "RED" },
            new { Color = Color.Orange, Text = "ORANGE" },
            new { Color = Color.Yellow, Text = "YELLOW" },
            new { Color = Color.Green, Text = "GREEN" },
            new { Color = Color.SkyBlue, Text = "SKY BLUE" },
            new { Color = Color.Blue, Text = "BLUE" },
            new { Color = Color.Purple, Text = "PURPLE" },
            new { Color = Color.Pink, Text = "PINK" },
            new { Color = Color.Aqua, Text = "AQUA" },
            new { Color = Color.Azure, Text = "AZURE" },
            new { Color = Color.Violet, Text = "VIOLET" },
            new { Color = Color.Tan, Text = "TAN" },
            new { Color = Color.Teal, Text = "TEAL" },
            new { Color = Color.Silver, Text = "SILVER" },
            new { Color = Color.Tomato, Text = "TOMATO" },
            new { Color = Color.Sienna, Text = "SIENNA" },
            new { Color = Color.Peru, Text = "PERU" },
            new { Color = Color.Olive, Text = "OLIVE" },
            new { Color = Color.MintCream, Text = "MINT CREAM" },
            new { Color = Color.Lime, Text = "LIME" },
        };

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
