using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace S5GameServer
{
    [Serializable]
    class ServerConfig
    {
        public string HostName = "thesettlers.tk";
        public int RouterPort = 40000;
        public int IRCPort = 16668;
        public int CDKeyPort = 44000;
        public int InitPort = 40080;
        public string CDKeyHost = "thesettlers.tk";
        public string AccountsFile = "accounts.xml";

        [DataMember(IsRequired = false)]
        public string[] Lobbies = { " The Settlers: Heritage of Kings", "The Settlers: Fog Realm", "The Settlers: Legends" };


        [NonSerialized]
        public string MOTD;

        [IgnoreDataMember]
        static ServerConfig inst;
        public static ServerConfig Instance
        {
            get
            {
                if (inst == null)
                {
                    if (!File.Exists(ConfigFile))
                    {
                        inst = new ServerConfig();
                        WriteConfig();
                    }
                    else
                    {
                        var ser = new DataContractSerializer(typeof(ServerConfig));
                        using (FileStream file = File.OpenRead(ConfigFile))
                        {
                            inst = (ServerConfig)ser.ReadObject(file);
                        }
                    }

                    var fsw = new FileSystemWatcher();
                    fsw.Path = Path.GetDirectoryName(Path.GetFullPath(MOTDFile));
                    fsw.Filter = Path.GetFileName(MOTDFile);
                    fsw.Changed += MOTD_Changed;
                    fsw.EnableRaisingEvents = true;
                    MOTD_Changed(fsw, new FileSystemEventArgs(WatcherChangeTypes.Changed, fsw.Path, fsw.Filter));
                }

                return inst;
            }
        }

        private static void MOTD_Changed(object sender, FileSystemEventArgs e)
        {
            var lines = File.Exists(MOTDFile) ? File.ReadAllLines(MOTDFile).ToList() : new List<string> { "Write this message to " + MOTDFile };
            lines.Add("Server v" + VersionHelper.GetVersion() + ", " + VersionHelper.GetCopyright());
            inst.MOTD = "\n" + lines.Select((s) => "    " + s).Aggregate((l1, l2) => l1 + "\n" + l2).Replace(' ', (char)0xA0);
        }

        public static void WriteConfig()
        {
            var ser = new DataContractSerializer(typeof(ServerConfig));
            using (var writer = XmlWriter.Create(ConfigFile, new XmlWriterSettings { Indent = true }))
            {
                ser.WriteObject(writer, inst);
            }
        }

        [IgnoreDataMember]
        const string ConfigFile = "config.xml";

        [IgnoreDataMember]
        const string MOTDFile = "motd.txt";
    }
}
