using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S5GameServices;

namespace S5GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            InitServer.Run();
            if (ServerConfig.Instance.HostName == ServerConfig.Instance.CDKeyHost)
                CDKeyServer.Run(); //doesnt work on dev machine, blocks udp port
            IRCServer.Run();
            LoginServer.Run();
            WaitModuleServer.Run();

            while (Console.ReadKey(true).Key != ConsoleKey.Q)
                Console.WriteLine("Press Q to exit!");
        }
    }
}
