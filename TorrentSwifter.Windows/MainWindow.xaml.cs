using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace TorrentSwifter.Windows
{
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();

            Forms.Init();
            LoadApplication(new UI.App());
        }
    }
}
