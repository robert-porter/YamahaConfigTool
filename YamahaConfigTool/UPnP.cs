/*
        void DiscoverDevice()
        {
            IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 1900);
            IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);


            Socket UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpSocket.Bind(LocalEndPoint);
            UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastEndPoint.Address, IPAddress.Any));
            UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);

            Console.WriteLine("UDP-Socket setup done...\r\n");

            string SearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nST:ssdp:all\r\nMX:3\r\n\r\n";

            UdpSocket.SendTo(Encoding.UTF8.GetBytes(SearchString), SocketFlags.None, MulticastEndPoint);

            Console.WriteLine("M-Search sent...\r\n");

            byte[] ReceiveBuffer = new byte[64000];

            int ReceivedBytes = 0;

            while (true)
            {
                if (UdpSocket.Available > 0)
                {
                    ReceivedBytes = UdpSocket.Receive(ReceiveBuffer, SocketFlags.None);

                    if (ReceivedBytes > 0)
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(ReceiveBuffer, 0, ReceivedBytes));
                    }
                }
            }
        }
 * 
 * 
        public void StartMulticastListener()
        {
            Socket s = null;
            try
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 1900);
                s.Bind(ipep);

                IPAddress ip = IPAddress.Parse("239.255.255.250");

                s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                        new MulticastOption(ip, IPAddress.Any));

            }

           
            catch(Exception e)
            {
                System.Console.Write(e.Message);
            }

            System.Console.Write(ssdpMessage);
            s.Send(Encoding.UTF8.GetBytes(ssdpMessage));
            while (true)
            {
                byte[] b = new byte[1024];
                s.Receive(b);

                MemoryStream stream = new MemoryStream(b);
                StreamReader reader = new StreamReader(stream);
                String messageType = reader.ReadLine();
                if (messageType.Contains("NOTIFY"))
                {
                    reader.ReadLine();//HOST
                    reader.ReadLine();//NT
                    reader.ReadLine();//NTS
                    String location = reader.ReadLine();
                    if (location.Contains("Location"))
                    {
                        int startIndex = location.IndexOf(":") + 1;
                        String url = location.Substring(startIndex);
                        //string str = System.Text.Encoding.ASCII.GetString(b, 0, b.Length);
                        ReadNotify(url);
                    }
                    
                }
            }

            //   Thread thread = new Thread(MulticastListen);
            //   thread.Start();
        }

        void ReadNotify(String url)
        {

        }
        void MulticastListen(Object unused)
        {
            /*
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                    ProtocolType.Udp);

                    IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 1900);
                    s.Bind(ipep);

                    IPAddress ip = IPAddress.Parse("239.255.255.250");

                    s.SetSocketOption(SocketOptionLevel.IP,
                        SocketOptionName.AddMembership,
                            new MulticastOption(ip, IPAddress.Any));

                    while (true)
                    {
                        byte[] b = new byte[4096];
                        s.Receive(b);
                        string str = System.Text.Encoding.ASCII.GetString(b, 0, b.Length);
                        Console.WriteLine(str.Trim());
                    }
                }
             */
        }
*/