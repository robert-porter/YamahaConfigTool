using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Xml;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace YamahaConfigTool
{
    /// <summary>
    /// Interaction logic for ConnectionSettingsControl.xaml
    /// </summary>
    /// 
    public static class ExtensionMethods //HACK to force wpf to update it's controls(for updating status messages) in the middle of the long running function
    { // doesn't really work that well but easier than creating another thread 
        //doesn't really work for more than 1 status update mid function

        private static Action EmptyDelegate = delegate() { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

    }

    public partial class ConnectionSettingsControl : UserControl
    {
        public ConnectionSettingsControl()
        {
            InitializeComponent();
            
            modelComboBox.Items.Add("AutoDetect");
            modelComboBox.Items.Add("RX-A730");
            modelComboBox.Items.Add("RX-A830");
            modelComboBox.Items.Add("RX-A1030");
            modelComboBox.Items.Add("RX-A2030");
            modelComboBox.Items.Add("RX-A3030");
            modelComboBox.Items.Add("RX-V675");
            modelComboBox.Items.Add("RX-V775");
            modelComboBox.SelectedIndex = 0;

        }

        public String ModelName { get; set; }
        public bool Connected { get; private set; }
        
        public Communication controller;
        public MainWindow mainWindow;
        public CursorControl cursorControl;



        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            controller.HostNameOrAddress = this.IPAddressOrHostnameTextBox.Text;
            controller.Port = PortTextBox.Text;

            IPAddress ip;

            if (string.IsNullOrEmpty(controller.HostNameOrAddress) || !IPAddress.TryParse(controller.HostNameOrAddress, out ip))
            {
                statusTextBlock.Text += "IP address is not valid";
                return;
            }

            String selectedModel = (String)modelComboBox.SelectedItem;

            String request = @"<YAMAHA_AV cmd=""GET""><System><Config>GetParam</Config></System></YAMAHA_AV>";
            XmlDocument response = controller.SendCommand(request);
            if (response == null)
            {
                statusTextBlock.Text = "Error connecting to receiver.";
                return;
            }

            String model = response.SelectSingleNode("//YAMAHA_AV//System//Config//Model_Name").InnerText;

            statusTextBlock.Text += String.Format("Successfully connected to a {0}\n", model);
            statusTextBlock.Text += "Loading protocol files...\n";
            ExtensionMethods.Refresh(statusTextBlock);


            if (selectedModel == "AutoDetect")
            {
                if (modelComboBox.Items.Contains(model))
                {
                    modelComboBox.SelectedItem = model;
                }
                else
                {
                    String message = String.Format("Auto Detect error: The receiver is a {0}, this model is not fully supported. You must select a model. Warning not all commands may function!\n", model);
                    statusTextBlock.Text = message;
                    return;
                }
            }


            if (model != selectedModel)
            {
                String message = String.Format("Warning receiver model does not match the selected model, the receiver model is {0}, the model selected is {1}, not all commands may function!\n", model, selectedModel);
                statusTextBlock.Text += message;
            }

            ModelName = model;

            FileCache.SetModel(selectedModel);


            mainWindow.treeViewControl.Connected();

            cursorControl.Connected();

            mainWindow.Connected();

        }
    }
}
