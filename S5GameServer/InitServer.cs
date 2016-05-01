using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    public class ServerDefinition
    {
        public string Host;
        public int Port;
    }

    public class InitServer
    {
        

        static string initResponse;
        static WebServer ws;

        public static void Run()
        {
            BuildInitResponse();
            ws = new WebServer(new[] { "http://" + ServerConfig.Instance.HostName + ":" + ServerConfig.Instance.InitPort.ToString() + "/" }, HandleRequest);
            ws.Run();
        }

        static void BuildInitResponse()
        {
            var template =    "[Servers]\n"
                            + "RouterIP0={0}\n"
                            + "RouterPort0={1}\n"
                            + "RouterLauncherPort0=40005\n" //??
                            + "RouterIP1={0}\n"
                            + "RouterPort1={1}\n"
                            + "RouterLauncherPort1=40005\n" //??
                            + "IRCIP0={0}\n"
                            + "IRCPort0={2}\n"
                            + "CDKeyServerIP0={5}\n" //{0}\n"
                            + "CDKeyServerPort0={3}\n"
                            + "NATServerIP0=216.98.52.136\n" //NAT???
                            + "NATServerPort0=45000\n"
                            + "NATServerIP1=216.98.52.133\n"
                            + "NATServerPort1=45000";
            var cfg = ServerConfig.Instance;
            initResponse = string.Format(template, cfg.HostName, cfg.RouterPort, cfg.IRCPort, cfg.CDKeyPort, cfg.NATPort, cfg.CDKeyHost);
        }

        static string HandleRequest(HttpListenerRequest req)
        {
            Console.WriteLine("InitServer: " + req.RawUrl);
            return initResponse;
        }
    }
}
