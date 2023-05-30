using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using Ait.WeatherClient.Core.Entities;
using Ait.WeatherClient.Core.Services;
using Ait.WeatherClient.Core.Helpers;
using Ait.WeatherClient.Core.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ait.WheatherClient.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Global variables
        LocationService locationService;
        Socket serverSocket;
        IPEndPoint serverEndpoint;
        #endregion

        #region Event Handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            locationService = new LocationService();
            PopulateControls();
            btnUpdate.Visibility = Visibility.Hidden;
            StartupConfig();
        }
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if(cmbLocations.SelectedItem == null)
            {
                cmbLocations.Focus();
                return;
            }
            SaveConfig();
            SendLocalInformation();
        }
        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {            
            SendLocalInformation();
        }
        private void CmbAvailableLocations_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbAvailableLocations.SelectedItem == null) return;
            string message = "GET|" + cmbAvailableLocations.SelectedItem.ToString() + "##EOM";
            string response = SendMessageToServer(message);
            if (response != "")
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                Location location = JsonConvert.DeserializeObject<Location>(response);
                DisplayLocationInfo(location);
            }
            else
            {
                DoVisuals(false);
                MessageBox.Show("Server unreachable");
            }

        }
        private void SldTemp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded) return;
            lblTemp.Content = sldTemp.Value.ToString();
        }
        private void SldWindSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded) return;
            lblWindSpeed.Content = sldWindSpeed.Value.ToString();
        }
        #endregion

        #region Methods
        private void SendLocalInformation()
        {
            Location location = new Location();
            location.LocationName = cmbLocations.SelectedItem.ToString();
            location.Temperature = int.Parse(lblTemp.Content.ToString());
            location.WindSpeed = int.Parse(lblWindSpeed.Content.ToString());
            location.CloudSituation = (CloudSituation)cmbCloudSituation.SelectedItem;
            location.WindDirection = (WindDirection)cmbWindDirection.SelectedItem;
            string json =  JsonConvert.SerializeObject(location);

            string message = "PUT|" + json + "##EOM";
            string response = SendMessageToServer(message);
            if (response != "")
            {
                response = response.Replace("##EOM", "").Trim().ToUpper();
                locationService.Locations = JsonConvert.DeserializeObject<List<Location>>(response);

                DoVisuals(true);
                DisplayLocations();
            }
            else
            {
                MessageBox.Show("Server unreachable");
                DoVisuals(false);
            }
        }
        private void DisplayLocations()
        {
            string selectedLocationName = "";
            if (cmbAvailableLocations.SelectedIndex > -1)
            {
                selectedLocationName = cmbAvailableLocations.SelectedItem.ToString();
            }
            cmbAvailableLocations.ItemsSource = null;
            cmbAvailableLocations.ItemsSource = locationService.Locations;
            if(selectedLocationName != "")
            {
                for(int r = 0; r < cmbAvailableLocations.Items.Count; r++)
                {
                    if(cmbAvailableLocations.Items[r].ToString() == selectedLocationName)
                    {
                        cmbAvailableLocations.SelectedIndex = r;
                        CmbAvailableLocations_SelectionChanged(null, null);
                    }
                }
            }
        }
        private void DisplayLocationInfo(Location location)
        {
            lblRemoteWindDirection.Content = location.WindDirection.ToString();
            lblRemoteWindSpeed.Content = location.WindSpeed.ToString();
            lblRemoteTemp.Content = location.Temperature.ToString();
            lblRemoteCloudSituation.Content = location.CloudSituation.ToString();

        }
        private void DoVisuals(bool isConnected)
        {
            if(isConnected)
            {
                btnUpdate.Visibility = Visibility.Visible;
                btnConnect.Visibility = Visibility.Hidden;
                cmbLocations.IsEnabled = false;
                cmbAvailableLocations.IsEnabled = true;
            }
            else
            {
                btnUpdate.Visibility = Visibility.Hidden;
                btnConnect.Visibility = Visibility.Visible;
                cmbLocations.IsEnabled = true;
                cmbAvailableLocations.IsEnabled = false;
                cmbAvailableLocations.ItemsSource = null;
            }
        }
        private void PopulateControls()
        {
            cmbLocations.Items.Clear();
            cmbLocations.Items.Add("Brussels");
            cmbLocations.Items.Add("London");
            cmbLocations.Items.Add("Paris");
            cmbLocations.Items.Add("Amsterdam");
            cmbLocations.Items.Add("Berlin");
            cmbLocations.Items.Add("Luxembourg");
            cmbLocations.Items.Add("Dublin");
            
            cmbCloudSituation.ItemsSource = Enum.GetValues(typeof(CloudSituation));
            cmbCloudSituation.SelectedIndex = 0;
            cmbWindDirection.ItemsSource = Enum.GetValues(typeof(WindDirection));
            cmbWindDirection.SelectedIndex = 0;
        }
        private void StartupConfig()
        {
            for (int port = 49200; port <= 49500; port++)
            {
                cmbPorts.Items.Add(port);
            }
            AppConfig.GetConfig(out string serverIP, out int communicationPort);
            txtServerIP.Text = serverIP;
            try
            {
                cmbPorts.SelectedItem = communicationPort;
            }
            catch
            {
                cmbPorts.SelectedItem = 49200;
            }

        }
        private void SaveConfig()
        {
            AppConfig.WriteConfig(txtServerIP.Text, int.Parse(cmbPorts.SelectedItem.ToString()));

        }

        private string SendMessageToServer(string message)
        {
            if (serverSocket == null)
            {
                IPAddress serverIP = IPAddress.Parse(txtServerIP.Text);
                int serverPort = int.Parse(cmbPorts.SelectedItem.ToString());
                serverEndpoint = new IPEndPoint(serverIP, serverPort);
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            try
            {
                serverSocket.Connect(serverEndpoint);
                byte[] outMessage = Encoding.ASCII.GetBytes(message);
                byte[] inMessage = new byte[1024];

                serverSocket.Send(outMessage);
                string response = "";
                while (true)
                {
                    int responseLength = serverSocket.Receive(inMessage);
                    response += Encoding.ASCII.GetString(inMessage, 0, responseLength).ToUpper();
                    if (response.IndexOf("##EOM") > -1)
                        break;
                }
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
                serverSocket = null;
                return response;
            }
            catch (Exception fout)
            {
                serverSocket = null;
                return "";
            }
        }
        #endregion

    }
}
