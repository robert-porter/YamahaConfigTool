using System;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace YamahaConfigTool
{
    /// <summary>
    /// Interaction logic for CursorControl.xaml
    /// </summary>
    public partial class CursorControl : UserControl
    {
        public Communication Controller { get; set; }
        String selectedZone = "Main_Zone";
        String[] zones = { "Main_Zone", "Zone_2", "Zone_3", "Zone_4" };
        Dictionary<String, List<String>> inputLists;
        Dictionary<String, List<String>> sceneLists;
        List<String> programs;

        public CursorControl()
        {
            InitializeComponent();
        }

        public void Connected()
        {
            Put_AddCommands();
            Get_AddCommands();

        }
        public void Put_AddCommands()
        {
            inputLists = new Dictionary<string, List<string>>();
            sceneLists = new Dictionary<string, List<string>>();
            foreach(String zone in zones)
            {
                inputLists.Add(zone, new List<String>());
                sceneLists.Add(zone, new List<String>());
                
            }
            programs = new List<string>();

            StringReader commandStream = new StringReader(FileCache.PutFileText);
            {
                String command = commandStream.ReadLine();
                do
                {
                    XmlDocument document = new XmlDocument();

                    document.LoadXml(command);

                    for (int i = 0; i < 4; i++)
                    {
                        String xpath = String.Format("/YAMAHA_AV/{0}/Input/Input_Sel", zones[i]);
                        XmlNode xmlNode = document.SelectSingleNode(xpath);
                        if (xmlNode != null)
                        {
                            inputLists[zones[i]].Add(xmlNode.InnerText);
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        String xpath = String.Format("/YAMAHA_AV/{0}/Scene/Scene_Load", zones[i]);
                        XmlNode xmlNode = document.SelectSingleNode(xpath);
                        if (xmlNode != null)
                        {
                            sceneLists[zones[i]].Add(xmlNode.InnerText);
                        }
                    }
                    {
                        String xpath = "/YAMAHA_AV/Main_Zone/Surround/Program_Sel/Current/Sound_Program";
                        XmlNode xmlNode = document.SelectSingleNode(xpath);
                        if (xmlNode != null)
                        {
                            programs.Add(xmlNode.InnerText);
                            programComboBox.Items.Add(xmlNode.InnerText);
                        }
                    }

                    command = commandStream.ReadLine();
                } while (command != null);
            }
        }

        public void Get_AddCommands()
        {
            XmlDocument zoneExistanceDoc = Controller.SendCommand(@"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""GET""><System><Config>GetParam</Config></System></YAMAHA_AV>");
            XmlNodeList xmlNodeList = zoneExistanceDoc.SelectNodes("/YAMAHA_AV/System/Config/Feature_Existence/*");

            if (xmlNodeList.Item(1).InnerText != "1") // zone 2
                radioButtonZone2.Visibility = Visibility.Collapsed;
            if (xmlNodeList.Item(2).InnerText != "1") // zone 3
                radioButtonZone3.Visibility = Visibility.Collapsed;
            if (xmlNodeList.Item(3).InnerText != "1") // zone 4
                radioButtonZone4.Visibility = Visibility.Collapsed;


            String inputGetCommand = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""GET""><{0}><Input><Input_Sel>GetParam</Input_Sel></Input></{0}></YAMAHA_AV>", selectedZone);
            XmlDocument inputDoc = Controller.SendCommand(inputGetCommand);
            XmlNode inputXmlNode = inputDoc.SelectSingleNode(String.Format("/YAMAHA_AV/{0}/Input/Input_Sel", selectedZone));
            if(inputXmlNode != null)
            {
                inputComboBox.SelectedItem = inputXmlNode.InnerText;
            }


            //program get command
            String programCommand = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""GET""><Main_Zone><Surround><Program_Sel><Current>GetParam</Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>";
            XmlDocument programDoc = Controller.SendCommand(programCommand);
            XmlNodeList programXmlNodeList = programDoc.SelectNodes("/YAMAHA_AV/Main_Zone/Surround/Program_Sel/Current/*");
            foreach(XmlNode programXmlNode in programXmlNodeList)
            {
                if (programXmlNode.Name == "Sound_Program")
                {
                    programComboBox.SelectedItem = programXmlNode.InnerText;
                }
            }
            radioButtonMainZone.IsChecked = true;
        }


        /*
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Down</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Up</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Left</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Right</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Sel</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Return</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Cursor>Return to Home</Cursor></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Menu_Control>On Screen</Menu_Control></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Menu_Control>Top Menu</Menu_Control></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Menu_Control>Menu</Menu_Control></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Menu_Control>Option</Menu_Control></Cursor_Control></Main_Zone></YAMAHA_AV>
<?xml version="1.0" encoding="utf-8"?><YAMAHA_AV cmd="PUT"><Main_Zone><Cursor_Control><Menu_Control>Display</Menu_Control></Cursor_Control></Main_Zone></YAMAHA_AV>
        */

        private void Cursor_Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            String arg = button.Tag as String;
            String command = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""PUT""><{0}><Cursor_Control><Cursor>{1}</Cursor></Cursor_Control></{0}></YAMAHA_AV>", selectedZone, arg);

            Controller.SendCommand(command);
        }

        private void Menu_Control_Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            String arg = button.Tag as String;
            String command = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""PUT""><{0}><Cursor_Control><Menu_Control>{1}</Menu_Control></Cursor_Control></{0}></YAMAHA_AV>", selectedZone, arg);
            
            Controller.SendCommand(command);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if(comboBox == programComboBox)
            {
                String arg = comboBox.SelectedItem as String;
                String command = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""PUT""><Main_Zone><Surround><Program_Sel><Current><Sound_Program>{0}</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>";
                Controller.SendCommand(String.Format(command, arg));

                String getCommand = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""GET""><Main_Zone><Surround><Program_Sel><Current>GetParam</Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>";
                XmlDocument document = Controller.SendCommand(getCommand);
                GetResponse_UpdateCommand(getCommand, document);
                return;
            }
            if(comboBox == inputComboBox)
            {
                String arg = comboBox.SelectedItem as String;
                String command = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""PUT""><{0}><Input><Input_Sel>{1}</Input_Sel></Input></{0}></YAMAHA_AV>";
                Controller.SendCommand(String.Format(command, selectedZone, arg));

                String getCommand = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""GET""><{0}><Input><Input_Sel>GetParam</Input_Sel></Input></{0}></YAMAHA_AV>";
                XmlDocument document = Controller.SendCommand(String.Format(getCommand, selectedZone));
                GetResponse_UpdateCommand(getCommand, document);
                return;
            }

        }

       

        void ZoneChanged()
        {
            inputComboBox.Items.Clear();
            if (inputLists == null)
                return;

            foreach (String input in inputLists[selectedZone])
            {
                inputComboBox.Items.Add(input);
            }
            /*
            sceneComboBox.Items.Clear();
            if (sceneLists == null)
                return;

            foreach (String scene in sceneLists[selectedZone])
            {
                sceneComboBox.Items.Add(scene);
                sceneComboBox.IsEnabled = true;
            }
            if (sceneLists[selectedZone].Count() == 0)
                sceneComboBox.IsEnabled = false;
            */
            if (selectedZone == "Main_Zone")
            {
                programComboBox.IsEnabled = true;
            }
            else
                programComboBox.IsEnabled = false;
        }

        private void radioButtonMainZone_Checked(object sender, RoutedEventArgs e)
        {
            selectedZone = "Main_Zone";
            ZoneChanged();

        }

        private void radioButtonZone2_Checked(object sender, RoutedEventArgs e)
        {
            selectedZone = "Zone_2";
            ZoneChanged();
        }

        private void radioButtonZone3_Checked(object sender, RoutedEventArgs e)
        {
            selectedZone = "Zone_3";
            ZoneChanged();
        }

        private void radioButtonZone4_Checked(object sender, RoutedEventArgs e)
        {
            selectedZone = "Zone_4";
            ZoneChanged();
        }

        private void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            String zone = selectedZone;
            String arg = "";
            Button button = sender as Button;

            if(button == systemPowerOnButton)
            {
                zone = "System";
                arg = "On";
            }
            else if (button == systemPowerStandbyButton)
            {
                zone = "System";
                arg = "Standby";
            }
            else if (button == zonePowerOnButton)
            {
                arg ="On";

            }
            else if (button == zonePowerStandbyButton)
            {
                arg = "Standby";
            }

            String command = @"<?xml version=""1.0"" encoding=""utf-8""?><YAMAHA_AV cmd=""PUT""><{0}><Power_Control><Power>{1}</Power></Power_Control></{0}></YAMAHA_AV>";
            Controller.SendCommand(String.Format(command, selectedZone, arg));
        }

        void GetResponse_UpdateCommand(String command, XmlDocument document)
        {
            XmlNode xmlNode = document.SelectSingleNode("/YAMAHA_AV");
            String zoneName = xmlNode.FirstChild.Name;

            xmlNode = document.SelectSingleNode(String.Format("/YAMAHA_AV/{0}/Input/Input_Sel", zoneName));
            if(xmlNode != null)
            {
                inputComboBox.SelectedItem = xmlNode.InnerText;

                return;
            }

            xmlNode = document.SelectSingleNode("/YAMAHA_AV/Main_Zone/Surround/Program_Sel/Current/Sound_Program");

            if(xmlNode != null)
            {
                programComboBox.SelectedItem = xmlNode.InnerText;
            }
        }
    }
}
