using IrcD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    public static class IRCServer
    {
        public static void Run()
        {
            Task.Run(() =>
            {
                var ircDaemon = new IrcDaemon();

                ircDaemon.Options.ClientCompatibilityMode = false;
                ircDaemon.Options.IrcCaseMapping = IrcCaseMapping.Ascii;
                ircDaemon.Options.MaxAwayLength = 300;
                ircDaemon.Options.MaxChannelLength = 40;
                ircDaemon.Options.MaxKickLength = 300;
                ircDaemon.Options.MaxLanguages = 5;
                ircDaemon.Options.MaxLineLength = 510;
                ircDaemon.Options.MaxNickLength = 25;
                ircDaemon.Options.MaxSilence = 20;
                ircDaemon.Options.MaxTopicLength = 300;
                ircDaemon.Options.MessageOfTheDay = null;
                ircDaemon.Options.NetworkName = "S5 Server Emulation Services";
                ircDaemon.Options.ServerName = "S5 Chat";
                ircDaemon.Options.ServerPass = null;
                ircDaemon.Options.ServerPorts = new List<int>() { ServerConfig.Instance.IRCPort };
                ircDaemon.Options.StandardKickMessage = "Kicked";
                ircDaemon.Options.StandardPartMessage = "Leaving";
                ircDaemon.Options.StandardQuitMessage = "Quit";
                ircDaemon.Options.StandardKillMessage = "Killed";

                ircDaemon.Options.AdminLocation1 = "no admin set";
                ircDaemon.Options.AdminLocation2 = "no admin set";
                ircDaemon.Options.AdminEmail = "no admin set";
                ircDaemon.Start();
            });
        }
    }
}