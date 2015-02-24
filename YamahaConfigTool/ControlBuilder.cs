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

namespace YamahaConfigTool
{
    public class ValExpUnitPutCommand
    {
        public String CommandString;

        public int Max { get; set; }
        public int Min { get; set; }
        public int Exp { get; set; }
        public int Step { get; set; }
        public string Unit { get; set; }
    }

    public class TextValuePutCommand
    {
        public String CommandString;

        public String Value { get; set; }
    }

    public class VariableTextPutCommand
    {
        public String CommandString;

        public int MaxLength { get; set; }
    }


    public class ValExpUnitGetResult
    {
        public int Val { get; set; }
        public int Exp { get; set; }
        public String Unit { get; set; }
    }

    public class TextGetResult
    {
        public String Value { get; set; }
    }



    public class ControlBuilder
    {
        public ControlBuilder(TreeViewItem tvi)
        {
            parent = tvi;
            tvi.Expanded += new RoutedEventHandler(CommandNodeParent_Expanded);
            parent.Items.Add("Loading...");
            built = false;
        }

        public void AddValExpUnitPutCommand(ValExpUnitPutCommand command)
        {
            valExpUnitPutCommand = command;
            putCommand = command.CommandString;
        }
        public void AddVariableTextPutCommand(VariableTextPutCommand command)
        {
            variableTextPutCommand = command;
            putCommand = variableTextPutCommand.CommandString;
        }
        public void AddTextValuePutCommand(TextValuePutCommand textValuePutCommand)
        {
            textValuePutCommands.Add(textValuePutCommand);
            putCommand = textValuePutCommand.CommandString;
        }

        public void AddValExpUnitGetResult(ValExpUnitGetResult result)
        {
            if (built && spinner != null)
            {
                spinner.ValueChanged -= Spinner_ValueChanged;
                spinner.Value = (Decimal)((result.Val) * Math.Pow(10, -valExpUnitPutCommand.Exp));
                spinner.ValueChanged += Spinner_ValueChanged;
            }
            else
                valExpUnitGetResult = result;
        }

        public void AddTextGetResult(TextGetResult result)
        {
            if (built)
            {
                if (textBox != null)
                {
                    textBox.TextChanged -= TextBox_TextChanged;
                    textBox.Text = result.Value;
                    textBox.TextChanged += TextBox_TextChanged;
                }
                if (comboBox != null)
                {
                    comboBox.SelectionChanged -= ComboBox_SelectionChanged;
                    comboBox.SelectedItem = result.Value;
                    comboBox.SelectionChanged += ComboBox_SelectionChanged;
                }
            }

            textGetResult = result;
        }

        public void CreateControls()
        {
            if (valExpUnitPutCommand != null)
            {
                spinner = new Xceed.Wpf.Toolkit.DecimalUpDown();
                spinner.Maximum = (Decimal)(valExpUnitPutCommand.Max * Math.Pow(10, -valExpUnitPutCommand.Exp));
                spinner.Minimum = (Decimal)(valExpUnitPutCommand.Min * Math.Pow(10, -valExpUnitPutCommand.Exp));
                spinner.Increment = (Decimal)(valExpUnitPutCommand.Step * Math.Pow(10, -valExpUnitPutCommand.Exp));

                spinner.FormatString = ".";
                for (int i = 0; i < valExpUnitPutCommand.Exp; i++)
                {
                    spinner.FormatString += "0 ";
                }
                spinner.FormatString += " " + valExpUnitPutCommand.Unit;

                parent.Items.Add(spinner);

                if (valExpUnitGetResult != null)
                {
                    spinner.Value = (Decimal)(valExpUnitGetResult.Val * Math.Pow(10, -valExpUnitGetResult.Exp));

                    putValue = valExpUnitGetResult.Val.ToString();
                }

                spinner.ValueChanged += new RoutedPropertyChangedEventHandler<object>(Spinner_ValueChanged);
            }

            if (valExpUnitGetResult != null && valExpUnitPutCommand == null)
            {
                Label label = new Label();
                label.Content = (valExpUnitGetResult.Val * Math.Pow(10, -valExpUnitGetResult.Exp)).ToString();
                label.Content += " " + valExpUnitGetResult.Unit;
                parent.Items.Add(label);
            }

            if (textValuePutCommands.Count != 0)
            {
                if (textGetResult != null)
                {
                    comboBox = new ComboBox();
                    foreach (TextValuePutCommand put in textValuePutCommands)
                    {
                        comboBox.Items.Add(put.Value);
                    }

                    comboBox.SelectedItem = textGetResult.Value;
                    putValue = textGetResult.Value;
                    comboBox.SelectionChanged += new SelectionChangedEventHandler(ComboBox_SelectionChanged);

                    parent.Items.Add(comboBox);
                }
                else
                {
                    foreach (TextValuePutCommand put in textValuePutCommands)
                    {
                        Button button = new Button();
                        button.Tag = String.Format(put.CommandString, put.Value);
                        button.Content = put.Value;
                        button.Click += new RoutedEventHandler(Button_Click);
                        parent.Items.Add(button);
                    }
                }
            }

            if (variableTextPutCommand != null)
            {
                textBox = new TextBox();
                textBox.MaxLength = variableTextPutCommand.MaxLength;

                if (textGetResult != null)
                {
                    putValue = textGetResult.Value;
                    textBox.Text = textGetResult.Value;
                }

                textBox.TextChanged += new TextChangedEventHandler(TextBox_TextChanged);

                parent.Items.Add(textBox);
            }

            if (textGetResult != null && variableTextPutCommand == null && textValuePutCommands.Count == 0)
            {
                Label label = new Label();
                label.Content = textGetResult.Value;
                parent.Items.Add(label);
            }
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            putValue = (sender as TextBox).Text;
            mainWindow.controller.SendCommand(String.Format(putCommand, (sender as TextBox).Text));
            mainWindow.treeViewControl.GetResponse_UpdateCommandAsync(getCommand);
        }

        void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (sender as ComboBox).SelectedItem as String;
            putValue = text;
            mainWindow.controller.SendCommand(String.Format(putCommand, text));
            mainWindow.treeViewControl.GetResponse_UpdateCommandAsync(getCommand);
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.controller.SendCommand((sender as Button).Tag as String);
            mainWindow.treeViewControl.GetResponse_UpdateCommandAsync(getCommand); // TODO: little broken, can't get all buttons
        }

        void Spinner_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Decimal coeff = (Decimal)Math.Pow(10.0, valExpUnitPutCommand.Exp);
            Decimal val = (Decimal)e.NewValue;
            putValue = ((int)(val * coeff)).ToString();
            mainWindow.controller.SendCommand(String.Format(putCommand, putValue));

            mainWindow.treeViewControl.GetResponse_UpdateCommandAsync(getCommand);
        }

        void CommandNodeParent_Expanded(Object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;
            if (tvi != null)
            {
                if (tvi.Items.Contains("Loading..."))
                {
                    if (tvi.Tag != null)
                    {
                        mainWindow.treeViewControl.GetResponse_UpdateCommand((String)tvi.Tag); 
                        tvi.Tag = null;
                    }
                    if (!built)
                    {
                        CreateControls();
                        built = true;
                    }
                    tvi.Items.Remove("Loading...");
                }
            }

        }



        Xceed.Wpf.Toolkit.DecimalUpDown spinner; //TODO: this is kind of a hack
        TextBox textBox;
        ComboBox comboBox;

        public String putCommand; // in the case of buttons the put command is in the buttons Tag, some of the buttons commands are not consistant with their shared commands but they're never used for buttons...
        public String putValue;
        public string getCommand;

        bool built;
        ItemsControl parent;

        ValExpUnitPutCommand valExpUnitPutCommand;
        VariableTextPutCommand variableTextPutCommand;
        List<TextValuePutCommand> textValuePutCommands = new List<TextValuePutCommand>();

        ValExpUnitGetResult valExpUnitGetResult; // results are cached so they can be used when the controls are built
        TextGetResult textGetResult;

        public static MainWindow mainWindow; // TODO: remove this
    }
}
