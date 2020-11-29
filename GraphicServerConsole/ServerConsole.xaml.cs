using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ServAsync;

namespace GraphicServerConsole
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        Server srv;
        bool srvState = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void serverStartButton_click(object sender, RoutedEventArgs e)
        {
            if (srvState)
            {
                srv = new Server(ref serverLogTextBox);
                srv.SetupServer();
            }
            srvState = false;
        }

        private void stopServerButton_click(object sender, RoutedEventArgs e)
        {
            srv.StopServer();
            serverLogTextBox.Text += $"[{DateTime.Now}] All clients disconnected. Server stopped\n";
        }

        private void exitButton_click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
