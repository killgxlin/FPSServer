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
                svr.Listen(1234);
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
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " receive "+Encoding.ASCII.GetString(bytes));
                    
                    svr.SendBytes(l, bytes, arg3);
                };
                
                while (true)
                {
                    svr.Update();
                }
            }
        }
    }
}