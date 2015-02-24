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
using System.ComponentModel;

namespace YamahaConfigTool
{
    /// <summary>
    /// Interaction logic for TreeViewControl.xaml
    /// </summary>
    public partial class TreeViewControl : UserControl
    {

        public Dictionary<TreeViewItem, ControlBuilder> controlDic = new Dictionary<TreeViewItem, ControlBuilder>();


        public TreeViewControl()
        {
            InitializeComponent();
        }


        public void Connected()
        {

            TreeViewItem treeViewRoot = new TreeViewItem();
            treeViewRoot.Header = "YAMAHA_AV";
            treeViewRoot.IsExpanded = true;
            treeView.Items.Clear();
            treeView.Items.Add(treeViewRoot);
            controlDic.Clear();

            Put_AddAllCommands();

            AddAllGetRequestToTreeViewTags();
        }


        ControlBuilder CreateOrModifyControlBuilder(TreeViewItem tvNode)
        {
            ControlBuilder controlBuilder;

            if (controlDic.TryGetValue(tvNode, out controlBuilder))
            {
                return controlBuilder;
            }
            else
            {
                controlBuilder = new ControlBuilder(tvNode);
                controlDic.Add(tvNode, controlBuilder);
                return controlBuilder;
            }
        }

        string GetXpath(XmlNode node)
        {
            if (node.Name == "#document")
                return String.Empty;
            return GetXpath(node.SelectSingleNode("..")) + "/" + (node.NodeType == XmlNodeType.Attribute ? "@" : String.Empty) + node.Name;
        }

        
        delegate bool MergeFunction(XmlNode xmlNode, TreeViewItem tvNode, Object userData);

        void MergeTrees(XmlNode xmlNode, TreeViewItem tvNode, MergeFunction mergeFunction, Object userData)
        {
            if(mergeFunction(xmlNode, tvNode, userData))
                return;

            foreach (XmlNode xmlChild in xmlNode.ChildNodes)
            {

                TreeViewItem tvChild = null;

                if (tvNode.Items.Contains("Loading..."))
                {
                    tvNode.Items.Remove("Loading...");
                }

                foreach (TreeViewItem tvi in tvNode.Items) //try to find the new child node in the existing tree 
                {
                    if ((String)tvi.Header == xmlChild.Name)
                    {
                        tvChild = tvi;
                        break;
                    }
                }

                if (tvChild == null) // the branch exist recursively merge
                {
                    tvChild = new TreeViewItem();
                    tvChild.Header = xmlChild.Name;
                    tvNode.Items.Add(tvChild);
                }

                MergeTrees(xmlChild, tvChild, mergeFunction, userData);
            }

        }

        bool Put_AddCommandMergeFunction(XmlNode xmlNode, TreeViewItem tvNode, Object unused)
        {
            if (Put_ChildIsTextCommand(xmlNode, tvNode))
                return true;

            XmlNode xmlChild = xmlNode.ChildNodes[0];
            if (xmlChild == null) //TODO:presets i think?? ignore for now need to fix!!
            { 
                return true; 
            }
                
            return false;
        }

        bool Put_AddCommand(String command)
        {
            XmlDocument commandDoc = new XmlDocument();
            commandDoc.LoadXml(command);

            XmlNode xmlNode = commandDoc.DocumentElement;
            TreeViewItem tvNode = (TreeViewItem)treeView.Items[0];

            MergeTrees(xmlNode, tvNode, Put_AddCommandMergeFunction, null);

            return false;
        }


        public bool Put_AddAllCommands()
        {
            StringReader commandStream = new StringReader(FileCache.PutFileText);
            //queue up 6 commands and check if they are a range 

            String command = commandStream.ReadLine();

            Queue<String> queuedCommands = new Queue<string>();

            for (int i = 0; i < 5; i++)
            {
                queuedCommands.Enqueue(command);
                command = commandStream.ReadLine();
            }

            while (command != null)
            {
                queuedCommands.Enqueue(command);
                if (Put_AddValExpUnitRange(queuedCommands.ToArray()))
                {

                    queuedCommands.Clear();
                    for (int i = 0; i < 5; i++)
                        queuedCommands.Enqueue(commandStream.ReadLine());
                }
                else if (Put_AddTextRange(queuedCommands.ToArray()))
                {
                    queuedCommands.Clear();
                    for (int i = 0; i < 5; i++)
                        queuedCommands.Enqueue(commandStream.ReadLine());
                }
                else
                    Put_AddCommand(queuedCommands.Dequeue());

                command = commandStream.ReadLine();
            }

            for (int i = 0; i < queuedCommands.Count; i++)
            {
                Put_AddCommand(queuedCommands.Dequeue());
            }

            return true;
        }

        bool Put_AddValExpUnitRange(String[] commands)
        {
            XmlDocument[] commandDocs = new XmlDocument[6];

            for (int i = 0; i < 6; i++)
            {

                commandDocs[i] = new XmlDocument();
                commandDocs[i].LoadXml(commands[i]);
            }

            int[] values = new int[6];
            int[] exps = new int[6];
            string[] units = new string[6];
            String parentName = ""; // so we don't get a false positive with multiple numeric puts with the same step in a row
            // TODO: check entire xpath rather than just parent name...

            for (int i = 0; i < 6; i++)
            {
                XmlNodeList valNodeList = commandDocs[i].GetElementsByTagName("Val");

                if (valNodeList.Count != 0 && valNodeList[0].FirstChild != null)
                {
                    if (i == 0)
                        parentName = valNodeList[0].ParentNode.Name;
                    else if (valNodeList[0].ParentNode.Name != parentName)
                        return false;


                    XmlNodeList expNodeList = commandDocs[i].GetElementsByTagName("Exp");
                    XmlNodeList unitNodeList = commandDocs[i].GetElementsByTagName("Unit");

                    if (expNodeList.Count == 0 || unitNodeList.Count == 0)
                        return false;

                    if (!int.TryParse(valNodeList[0].FirstChild.InnerText, out values[i]) || !int.TryParse(expNodeList[0].FirstChild.InnerText, out exps[i]))
                        return false;
                }
                else
                    return false;
            }

            int diff1 = values[1] - values[0];
            int diff2 = values[2] - values[1];

            int diff3 = values[4] - values[3];
            int diff4 = values[5] - values[4];


            if (diff1 == diff2 && diff1 == diff3 && diff1 == diff4)
            {
                XmlNode valNode = commandDocs[0].GetElementsByTagName("Val")[0];
                valNode.FirstChild.InnerText = "{0}";
                ValExpUnitPutCommand command = new ValExpUnitPutCommand();
                command.CommandString = commandDocs[0].OuterXml;
                command.Step = diff1;
                command.Min = values[0];
                command.Max = values[5];
                command.Unit = commandDocs[0].GetElementsByTagName("Unit")[0].InnerText;
                command.Exp = int.Parse(commandDocs[0].GetElementsByTagName("Exp")[0].InnerText);

                Put_AddValExpUnitCommand(commandDocs[0], command);
                return true;

            }

            return false;
        }

        bool Put_AddTextRange(String[] commands) 
        {
            //some of these are not val exp unit commands but they are treated as such since its a small special case, just make sure don't look for val exp unit stuff here it will break stuff. 
            XmlDocument[] commandDocs = new XmlDocument[6];
            for (int i = 0; i < 6; i++)
            {
                commandDocs[i] = new XmlDocument();
                commandDocs[i].LoadXml(commands[i]);
            }


            int[] values = new int[6];
            String xpath;
            XmlNode xmlNode;
            for (xmlNode = commandDocs[0].DocumentElement; xmlNode.FirstChild != null; xmlNode = xmlNode.FirstChild) ;

            if (xmlNode.Value == null)
                return false;

            if (!int.TryParse(xmlNode.Value, out values[0]))
                return false;

            xpath = GetXpath(xmlNode);

            for (int i = 1; i < 6; i++)
            {
                for (xmlNode = commandDocs[i].DocumentElement; xmlNode.FirstChild != null; xmlNode = xmlNode.FirstChild) ;
                String xpath2 = GetXpath(xmlNode);

                if (xpath != xpath2)
                    return false;

                if (!int.TryParse(xmlNode.Value, out values[i]))
                    return false;
            }

            int diff1 = values[1] - values[0];
            int diff2 = values[2] - values[1];

            int diff3 = values[4] - values[3];
            int diff4 = values[5] - values[4];

            if (diff1 == diff2 && diff1 == diff3 && diff1 == diff4)
            {

                xmlNode.InnerText = "{0}"; 
                ValExpUnitPutCommand command = new ValExpUnitPutCommand();
                command.CommandString = xmlNode.OwnerDocument.OuterXml;
                command.Step = diff1;
                command.Min = values[0];
                command.Max = values[5];
                command.Unit = "";
                command.Exp = 0;

                Put_AddValExpUnitCommand(xmlNode.OwnerDocument, command);
                return true;
            }

            return false;
        }

        bool Put_AddValExpUnitCommandMergeTrees(XmlNode xmlNode, TreeViewItem tvNode, Object valExpUnitCommand) // not really all val exp unit, see Put_AddTextRange
        {
            ValExpUnitPutCommand command = valExpUnitCommand as ValExpUnitPutCommand;
            if (XmlNodeChildIsValExpUnit(xmlNode) || xmlNode.FirstChild.NodeType == XmlNodeType.Text) 
            {
                CreateOrModifyControlBuilder(tvNode).AddValExpUnitPutCommand(command);
                return true;
            }

            return false;
        }

        void Put_AddValExpUnitCommand(XmlDocument putDoc, ValExpUnitPutCommand command)
        {
            XmlNode xmlNode = putDoc.DocumentElement;
            TreeViewItem tvNode = (TreeViewItem)treeView.Items[0];

            MergeTrees(xmlNode, tvNode, Put_AddValExpUnitCommandMergeTrees, command);
        }

        bool XmlNodeChildIsValExpUnit(XmlNode xmlNode)
        {
            if (xmlNode.HasChildNodes && xmlNode.ChildNodes.Count == 3 && xmlNode.ChildNodes[0].Name == "Val" && xmlNode.ChildNodes[1].Name == "Exp" && xmlNode.ChildNodes[2].Name == "Unit")
                return true;
            else
                return false;
        }

        bool Put_ChildIsTextCommand(XmlNode xmlNode, TreeViewItem tvNode)
        {
            if (XmlNodeChildIsValExpUnit(xmlNode))
            {
                if (xmlNode.FirstChild.FirstChild == null)
                {
                    //TODO: These are presets(need to check that is only presets)
                    //I think need to set the same as setting a normal value?
                    return true;
                }

                TextValuePutCommand command = new TextValuePutCommand();
                command.Value = xmlNode.FirstChild.FirstChild.Value;
                xmlNode.FirstChild.FirstChild.Value = "{0}";
                command.CommandString = xmlNode.OwnerDocument.OuterXml;

                CreateOrModifyControlBuilder(tvNode).AddTextValuePutCommand(command);

                return true;
            }

            else if (xmlNode.HasChildNodes && xmlNode.ChildNodes.Count == 1 && xmlNode.ChildNodes[0].NodeType == XmlNodeType.Text)
            {
                if (xmlNode.FirstChild.Value.All(c => c == '@'))
                {
                    VariableTextPutCommand command = new VariableTextPutCommand();

                    command.MaxLength = xmlNode.FirstChild.Value.Length;
                    xmlNode.FirstChild.Value = "{0}";
                    command.CommandString = xmlNode.OwnerDocument.OuterXml;

                    CreateOrModifyControlBuilder(tvNode).AddVariableTextPutCommand(command);

                    return true;
                }
                else
                {
                    TextValuePutCommand command = new TextValuePutCommand();
                    command.Value = xmlNode.FirstChild.Value;
                    xmlNode.FirstChild.Value = "{0}";
                    command.CommandString = xmlNode.OwnerDocument.OuterXml;

                    CreateOrModifyControlBuilder(tvNode).AddTextValuePutCommand(command);

                    return true;
                }
            }
            
            return false;
        }

        bool GetResponse_ChildIsValExpUnit(XmlNode xmlNode, TreeViewItem tvNode, Object request) 
        {
            String requestString = request as String;

            int val, exp;
            String unit;
            ValExpUnitGetResult command = new ValExpUnitGetResult();

            if (XmlNodeChildIsValExpUnit(xmlNode))
            {
                if (int.TryParse(xmlNode.ChildNodes[0].InnerText, out val) && int.TryParse(xmlNode.ChildNodes[1].InnerText, out exp))
                {
                    unit = xmlNode.ChildNodes[2].InnerText;

                    command.Val = val;
                    command.Exp = exp;
                    command.Unit = unit;
                    ControlBuilder controlBuilder = CreateOrModifyControlBuilder(tvNode);
                    controlBuilder.AddValExpUnitGetResult(command);
                    controlBuilder.getCommand = requestString;
                    return true;
                }
                //else its a value like Up, Down or something
            }

            return false;
        }

        bool GetResponse_ChildIsTextValue(XmlNode xmlNode, TreeViewItem tvNode, Object request)
        {
            String requestString = request as String;

            if (xmlNode.HasChildNodes)
            {
                if (xmlNode.ChildNodes.Count == 1 && xmlNode.ChildNodes[0].NodeType == XmlNodeType.Text)
                {
                    TextGetResult command = new TextGetResult();
                    command.Value = xmlNode.FirstChild.Value;

                    ControlBuilder controlBuilder = CreateOrModifyControlBuilder(tvNode);
                    controlBuilder.AddTextGetResult(command);
                    controlBuilder.getCommand = requestString;

                    return true;
                }
            }

            return false;
        }

        bool AddAllGetRequestToTreeViewTagsMergeTrees(XmlNode xmlNode, TreeViewItem tvNode, Object unused)
        {
            XmlNode xmlChild = xmlNode.ChildNodes[0];

            if (xmlChild.NodeType == XmlNodeType.Text)
            {
                tvNode.Tag = xmlNode.OwnerDocument.OuterXml;
                tvNode.Items.Add("Loading...");
                tvNode.Expanded += new RoutedEventHandler(TreeViewItem_Expanded);
                return true;
            }

            return false;
        }

        public void AddAllGetRequestToTreeViewTags()
        {
            StringReader commandStream = new StringReader(FileCache.GetFileText);

            String command = commandStream.ReadLine();
            while (command != null)
            {

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(command);

                XmlNode xmlNode = doc.DocumentElement;
                TreeViewItem tvNode = treeView.Items[0] as TreeViewItem;

                MergeTrees(xmlNode, tvNode, AddAllGetRequestToTreeViewTagsMergeTrees, null);

                command = commandStream.ReadLine();
            }
        }

        void GetResponse_UpdateAll(String getfile)
        {
            using (StreamReader commandStream = new StreamReader(getfile))
            {
                while (!commandStream.EndOfStream)
                {
                    GetResponse_UpdateCommandAsync(commandStream.ReadLine());
                }
            }
        }

        void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi != null)
            {
                if (tvi.Items.Contains("Loading..."))
                {
                    tvi.Items.Remove("Loading...");

                    if (tvi.Tag != null)
                        GetResponse_UpdateCommandAsync((String)tvi.Tag);
                }
            }
        }

        bool GetResponseUpdateCommandMergeTrees(XmlNode xmlNode, TreeViewItem tvNode, Object requestString)
        {
            if (GetResponse_ChildIsValExpUnit(xmlNode, tvNode, requestString))
                return true;

            if (GetResponse_ChildIsTextValue(xmlNode, tvNode, requestString))
                return true;

            if (tvNode.Items.Contains("Loading..."))
            {
                tvNode.Items.Remove("Loading...");
            }


            return false;

        }

        public void GetResponse_UpdateCommand(String command)
        {
            XmlDocument document = ControlBuilder.mainWindow.controller.SendCommand(command);

            if (document != null)
                MergeTrees(document.DocumentElement, (TreeViewItem)treeView.Items[0], GetResponseUpdateCommandMergeTrees, command);
        }

        public void GetResponse_UpdateCommandAsync(String command)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker(); 
            backgroundWorker.DoWork += backgroundWorker_DoWork; 
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted; 

            backgroundWorker.RunWorkerAsync(command); 
        }

        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e) 
        {
            String command = e.Argument as String;

            XmlDocument document = ControlBuilder.mainWindow.controller.SendCommand(command);

            Object result = new Object[] {document, command};
            e.Result = result;
        } 

        public void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        { 
            Object[] result = e.Result as Object[];
            XmlDocument document = result[0] as XmlDocument;
            String command = result[1] as String;

            if (e.Cancelled) 
            {
                
            } 
            else if (e.Error != null) 
            { 
                
            } 
            else 
            {
                if (document != null)
                MergeTrees(document.DocumentElement, (TreeViewItem)treeView.Items[0], GetResponseUpdateCommandMergeTrees, command);
            } 
        }
    }
}
