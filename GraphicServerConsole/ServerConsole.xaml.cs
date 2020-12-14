using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
                if (ipTextBox.Text != "" && portTextBox.Text != "")
                {
                    try
                    {
                        srv = new Server(ref serverLogTextBox, ipTextBox.Text, portTextBox.Text);
                        srv.SetupServer();
                        srvState = false;
                    }
                    catch (Exception ex)
                    {
                        serverLogTextBox.Text += $"[{DateTime.Now}] Something went wrong :( Details: {ex.Message}\n";
                    }
                }
                else
                    MessageBox.Show("IP address and port can not be empty!", "Error");
            }
            else
                serverLogTextBox.Text += $"[{DateTime.Now}] Server already running!\n";
        }

        private void stopServerButton_click(object sender, RoutedEventArgs e)
        {
            if (!srvState)
            {
                srv.StopServer();
                srv.Dispose();
            }
            else
                serverLogTextBox.Text += $"[{DateTime.Now}] Server already stopped!\n";

            srvState = true;
        }

        private void exitButton_click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void listClientsButton_click(object sender, RoutedEventArgs e)
        {
            if(!srvState)
            {
                List<Socket> clientsSockets = srv.getClients();
                if (clientsSockets.Count != 0)
                {
                    var message = "";
                    foreach (Socket client in clientsSockets)
                        message += "Client IP: " + client.RemoteEndPoint + "\n";
                    MessageBox.Show(message, "Clients");
                }
                else
                    MessageBox.Show("No clients connected", "Clients");
            }
            else
            {
                MessageBox.Show("Run server first!", "Error");
            }
        }
    }
}
