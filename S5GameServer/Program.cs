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
            if (Type.GetType("Mono.Runtime") == null)
            {
                Console.SetWindowSize(200, 40);
                Console.BufferWidth = 800;
            }

            Console.WriteLine("\n\t-------------------------------");
            Console.WriteLine("\t       ~ UbiCom Rebuild ~");
            Console.WriteLine("\t       ~ Project Online ~");
            Console.WriteLine("\t-------------------------------");
            Console.WriteLine("\t for Settlers V\t\tv{0}", VersionHelper.GetVersion());


            Lobby.AddLobbies(ServerConfig.Instance.Lobbies);

            InitServer.Run();
            if (ServerConfig.Instance.HostName == ServerConfig.Instance.CDKeyHost)
                CDKeyServer.Run(); //doesnt work on dev machine, blocks udp port
            IRCServer.Run();
            LoginServer.Run();
            WaitModuleServer.Run();
            LobbyServer.Run();
            LadderWaitModule.Run();
            LadderServer.Run();

            Console.WriteLine();
            Console.WriteLine("\t PRESS 'ENTER' to exit!");
            Console.WriteLine();
            Console.ReadLine();
        }
    }
}
