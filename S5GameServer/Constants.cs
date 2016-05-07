using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    static class Constants
    {
        public const int LOBBY_SERVER_ID = 50; //there could be more than one lobby servers, we just use one

        public const int WAITMODULE_SERVER_PORT = 40001;
        public const int LADDER_LOGIN_SERVER_PORT = 42000;
        public const int LADDER_SERVER_PORT = 40100;
        public const int LOBBY_SERVER_PORT = 40101;
    }
}
