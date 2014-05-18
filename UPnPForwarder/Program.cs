using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UPnP;

namespace UPnPForwarder
{
    class Program
    {
        private static bool _success;
        private static bool _discovered;
        private static bool _running = true;
        private static readonly List<int> ForwardedPorts = new List<int>();


        static void Main()
        {
            Console.Title = "UPnP Forwarder";
            using (var forward = new Forwarder())
            {
                forward.BeginRun();
            }
        }

        private class Forwarder : IDisposable
        {
            public void BeginRun(string otherText = null)
            {
                while (_running)
                {
                    _discovered = SharpUPnP.Discover(SharpUPnP.GetGateways());
                    Console.Clear();


                    if (otherText != null)
                        Console.WriteLine(otherText + Environment.NewLine);

                    Help();
                    var s = Console.ReadLine();
                    if (s == null)
                        return;

                    if (s.StartsWith("start"))
                    {
                        Console.Clear();
                        var port = s.Split(' ').Length > 1 ? s.Split(' ')[1] : "Invalid";

                        if (port != "Invalid")
                        {
                            int intPort;
                            if (int.TryParse(port, out intPort))
                            {
                                if (intPort > 1024 && intPort < 65537)
                                {
                                    ForwardedPorts.Add(intPort);
                                    Start(intPort);
                                }
                            }
                        }
                        else
                        {
                            BeginRun();
                        }
                    }

                    if (s.StartsWith("stop"))
                    {
                        Console.Clear();
                        var port = s.Split(' ').Length > 1 ? s.Split(' ')[1] : "Invalid";

                        if (port != "Invalid")
                        {
                            int intPort;
                            if (int.TryParse(port, out intPort))
                            {
                                if (intPort > 1024 && intPort < 65537)
                                {
                                    if (ForwardedPorts.Contains(intPort))
                                        ForwardedPorts.Remove(intPort);
                                    else
                                        BeginRun();
                                    Stop(intPort);
                                }
                            }
                        }
                        else
                            BeginRun();
                    }

                    if (s == "exit")
                    {
                        var thread = new Thread(delegate()
                        {
                            Console.WriteLine("Closing...");
                            Thread.Sleep(2000);
                        });
                        thread.Start();
                        thread.Join();
                        _running = false;
                        break;
                    }
                    otherText = null;
                }
            }

            private void Start(int port = 7777)
            {
                if (_discovered)
                {
                    SharpUPnP.DeleteForwardingRule(port, ProtocolType.Udp);
                    SharpUPnP.DeleteForwardingRule(port, ProtocolType.Tcp);
                    var udp = SharpUPnP.ForwardPort(port, ProtocolType.Udp, "UPnP @ Port: " + port);
                    var tcp = SharpUPnP.ForwardPort(port, ProtocolType.Tcp, "UPnP @ Port: " + port);

                    _success = udp & tcp;

                    var s = ("(UPnP) Port Forward succesful.");
                    if (_success)
                    {
                        try
                        {
                            var ip = SharpUPnP.GetExternalIP().ToString();
                            if (!String.IsNullOrEmpty(ip))
                                s +=
                                    (Environment.NewLine + "(UPnP) Your IP: " + SharpUPnP.GetExternalIP());
                            BeginRun(s);
                        }
                        catch (Exception ex)
                        {
                            s = ex.ToString();
                            BeginRun(s);
                        }
                    }
                    else
                    {
                        s = ("(UPnP) Port Forward failed. (Port already taken?)");
                        BeginRun(s);
                    }
                }
                else
                {
                    const string s = ("(UPnP) Failed to discover UPnP service.");
                    BeginRun(s);
                }
            }

            private void Stop(int port = 7777, bool disposing = false)
            {
                if (_discovered)
                {
                    const string s = ("(UPnP) Disposing port forward.");
                    SharpUPnP.DeleteForwardingRule(port, ProtocolType.Udp);
                    SharpUPnP.DeleteForwardingRule(port, ProtocolType.Tcp);
                    if (!disposing)
                        BeginRun(s);
                }
                else
                {
                    const string s = ("(UPnP) Service was not discovered, nothing to dispose.");
                    if (!disposing)
                        BeginRun(s);
                }
            }

            private static void Help()
            {
                Console.WriteLine("Use 'start {portNumber}' to open a port");
                Console.WriteLine("Use 'stop {portNumber} to close an opened port");
                Console.WriteLine("Use 'exit' to close the program");
            }

            public void Dispose()
            {
                foreach (var port in ForwardedPorts)
                    Stop(port, true);
            }
        }
    }
}
