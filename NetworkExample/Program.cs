using System;
using System.Runtime.CompilerServices;
using System.Text;
using Share.Network;

namespace NetworkExample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (var svr = new UDPServer())
            {
                svr.OnConnect = l => 
                {
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " connected "+l);
                };
                svr.OnDisconnect = l =>
                {
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " disconnected "+l);
                };
                svr.OnReceive = (l, bytes, arg3) =>
                {
                    var recved = Encoding.ASCII.GetString(bytes);
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " receive "+recved);

                    switch (recved)
                    {
                    case "dc":
                        svr.Disconnect(l);
                        break;
                    default:
                        svr.SendBytes(l, bytes, arg3);
                        break;
                    }
                    
                    
                };
                
                svr.Listen(1234);
                while (true)
                {
                    svr.Update();
                }
            }
        }
    }
}