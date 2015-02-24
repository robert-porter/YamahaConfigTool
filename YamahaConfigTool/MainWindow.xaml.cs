using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.XPath;
using System.Threading;
using System.Net.Sockets;

/* 
 * TODO LIST: 
 * 
 * Fix all TODOs in code.
 * 
 * Button control spinner interaction, how to do things like like mute?? also Max_Volume ands it one out of place number? it shares the same put, so may be easy to tie to the corresponding spinner but how should it look??
 * Call get for each UI action, lags a little?? Put get updates in a background thread?? Events are better but polling will see changes made outside program
 * maybe polling and events. EVENTS DONE, NEEDS POLLING
 * 
 * Save and send save file to receiver. Poll to make save faster??
 * 
 * Create controls without talking to receiver, maybe connect to each receiver and create definition files - Is this needed?
 * 
 * Network settings, currently cannot be set, will break everything. Probably need an apply button and custom code or something, setting on each keystroke is asking to break everything...
 * 
 * Does memory guard affect YNC commands?
 * 
 * Investigate failed commands(some are definitely bad, figure out if I need to fix or just ignore/remove gabage).
 * 
 * Clean up protocol files remove worthless and redundant commands
 * 
 * Better feedback if a command fails, needs to not be overwhelming when using on unsupported model
 * 
 * Create address book? Worth the effort?
 * 
 * Investigate IR commands, should I add them?
 * 
 * Dynamically create pages from defined branches
 * 
 * Create and save quick functionality pages(maybe checkboxes for each command branch to determine what is on the page)
 * 
 * Custom pages
 * 
 * Make protocol files for lower level rx-v receivers by checking return values and purging unsupported commands
 */

//IR Command
//cmd="PUT"><System><Misc><Remote_Signal><Receive><Code>@@@@@@@@</Code></Receive></Remote_Signal></Misc></System></YAMAHA_AV>

namespace YamahaConfigTool
{
    public partial class MainWindow : Window
    {
        public Communication controller;
        NetworkSettingsControl networkSettingsControl;
        ConnectionSettingsControl connectionSettingsControl;
        CursorControl cursorControl;
        public TreeViewControl treeViewControl;

        
        public MainWindow()
        {
            InitializeComponent();

            controller = new Communication();
            cursorControl = new CursorControl();
            cursorControl.InitializeComponent();

            treeViewControl = new TreeViewControl();

            connectionSettingsControl = new ConnectionSettingsControl();
            connectionSettingsControl.controller = controller;
            connectionSettingsControl.mainWindow = this;
            connectionSettingsControl.InitializeComponent();

            networkSettingsControl = new NetworkSettingsControl();
            networkSettingsControl.controller = controller;
            networkSettingsControl.InitializeComponent();


            cursorControl.Controller = controller;
            connectionSettingsControl.cursorControl = cursorControl;

            mainDockPanel.Children.Insert(0,treeViewControl);

            dockPanel.Children.Add(connectionSettingsControl);

            controller.StartMulticastListener();

            try
            {
                TreeViewItem treeViewRoot = new TreeViewItem();
                treeViewRoot.Header = "YAMAHA_AV";
                treeViewRoot.IsExpanded = true;
                treeViewControl.treeView.Items.Clear();
                treeViewControl.treeView.Items.Add(treeViewRoot);

                ControlBuilder.mainWindow = this;


            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void Connected()
        {
            statusLabel.Content = "Connected" + ": " + connectionSettingsControl.ModelName;
        }
        void CreateMenus()
        {

            StringReader commandStream = new StringReader(FileCache.GetFileText);

            String command = commandStream.ReadLine();
            while (command != null)
            {

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(command);

                XmlNode xmlNode = doc.DocumentElement;
                xmlNode = xmlNode.FirstChild;

                MenuItem found = null;
                foreach(MenuItem m in menu.Items)
                {
                    if (m.Header as String == xmlNode.Name)
                    {
                        found = m;
                        break;
                    }

                }
                if(found == null)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = xmlNode.Name;
                    menu.Items.Add(menuItem);
                }

                foreach (MenuItem m in menu.Items)
                {
                    if (m.Header as String == xmlNode.Name)
                    {
                        XmlNode xmlNode2 = xmlNode.FirstChild;
                        MenuItem found2 = null;
                        foreach (MenuItem m2 in m.Items)
                        {
                            if (m2.Header as String == xmlNode2.Name)
                            {
                                found2 = m2;
                                break;
                            }

                        }
                        if (found2 == null)
                        {
                            MenuItem menuItem = new MenuItem();
                            menuItem.Header = xmlNode2.Name;
                            m.Items.Add(menuItem);
                        }

                    }
                }

                command = commandStream.ReadLine();
            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if ((String)item.Header == "Connection Settings")
            {

                dockPanel.Children.Clear();
                dockPanel.Children.Add(connectionSettingsControl);
            }
            if ((String)item.Header == "Controller")
            {
                dockPanel.Children.Clear();
                dockPanel.Children.Add(cursorControl);
            }
            if((String)item.Header == "Network")
            {
                dockPanel.Children.Clear();
                dockPanel.Children.Add(networkSettingsControl);
            }
            if ((String)item.Header == "Tree View")
            {
                if (item.IsChecked)
                {
                    if (!mainDockPanel.Children.Contains(treeViewControl))
                    {
                        mainDockPanel.Children.Insert(0, treeViewControl);
                    }
                }
                else
                {
                    if (mainDockPanel.Children.Contains(treeViewControl))
                    {
                        mainDockPanel.Children.Remove(treeViewControl);
                    }
                }
            }
            if((String)item.Header == "Exit")
            {
                this.Close();
            }
        }


    }


}

