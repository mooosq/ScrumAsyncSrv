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

namespace GraphicClient
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Client client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void connectToServer_click(object sender, RoutedEventArgs e)
        {
            if (ipAddr.Text != "" && port.Text != "")
            {
                try
                {
                    client = new Client(ipAddr.Text, port.Text);
                    ipAddr.Visibility = Visibility.Hidden;
                    port.Visibility = Visibility.Hidden;
                    Connect.Visibility = Visibility.Hidden;
                    ipAddrLabel.Visibility = Visibility.Hidden;
                    portLabel.Visibility = Visibility.Hidden;
                    clientLog.Visibility = Visibility.Visible;
                    clientCommand.Visibility = Visibility.Visible;
                    sendCommand.Visibility = Visibility.Visible;
                    this.Height = 350;
                    this.Width = 800;

                    clientLog.Text += client.Receive();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
            else
                MessageBox.Show("Fields can not be empty!", "Error");
        }
    }
}
