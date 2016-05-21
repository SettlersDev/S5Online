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
        //do not reference any foreign assemblies in Main(), since embedding via Fody.Costura won't work here in a Mono environment
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

            StartServers();
            
            Console.WriteLine("\n\t PRESS 'ENTER' to exit!\n");
            Console.ReadLine();
        }

        static void StartServers()
        {
            var logger = new DualLogger("logs/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", true);

            Lobby.AddLobbies(ServerConfig.Instance.Lobbies);

            InitServer.Run(logger);
            if (ServerConfig.Instance.HostName == ServerConfig.Instance.CDKeyHost)
                CDKeyServer.Run(logger); //doesn't work on dev machine, blocks udp port
            IRCServer.Run();


            new MessageServer<LoginClientHandler> { Port = ServerConfig.Instance.RouterPort, Logger = logger }.Run();
            new MessageServer<WaitModuleConnection>() { Port = Constants.WAITMODULE_SERVER_PORT, Logger = logger }.Run();
            new MessageServer<LobbyServerConnection>() { Port = Constants.LOBBY_SERVER_PORT, Logger = logger }.Run();
            new MessageServer<LadderWaitModuleConnection>() { Port = Constants.LADDER_LOGIN_SERVER_PORT, Logger = logger }.Run();
            new MessageServer<LadderConnection>() { Port = Constants.LADDER_SERVER_PORT, Logger = logger }.Run();
        }
    }
}
