using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace YamahaConfigTool
{
    public class FileCache
    {
        public static String[] SupportedModels
        {
            get
            {
                return new String[] {
                    "RX-A730", 
                    "RX-A830", 
                    "RX-A1030",
                    "RX-A2030",
                    "RX-A3030",
                    "RX-V675",
                    "RX-V775"};
            }
        }

        public static bool SetModel(String model)
        {
            if(SupportedModels.Contains(model))
            {
                PutFileName = "Put_" + model + "_U.txt";
                GetFileName = "Get_" + model + "_U.txt";

                System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                Stream putStream = myAssembly.GetManifestResourceStream("YamahaConfigTool.Resources." + PutFileName);
                Stream getStream = myAssembly.GetManifestResourceStream("YamahaConfigTool.Resources." + GetFileName);

                try
                {
                    using (StreamReader putStreamReader = new StreamReader(putStream))
                    {
                        PutFileText = putStreamReader.ReadToEnd();
                    }
                    using (StreamReader getStreamReader = new StreamReader(getStream))
                    {
                        GetFileText = getStreamReader.ReadToEnd();
                    }
                }
                catch(Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public static String GetFileText
        {
            get;
            protected set;
        }
        public static String PutFileText
        {
            get;
            protected set;
        }

        static String GetFileName
        {
            get;
            set;
        }
        static String PutFileName
        {
            get;
            set;
        }
    }
}
