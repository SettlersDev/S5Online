using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    class Lobby
    {
        public string Name { get; protected set; }
        public int LobbyID { get; protected set; }

        public List<PlayerAccount> Players = new List<PlayerAccount>();
        public Dictionary<int, GameRoom> Rooms = new Dictionary<int, GameRoom>();
        protected int nextRoomID = -100;

        protected Lobby(string name, int id)
        {
            Name = name;
            LobbyID = id;
        }

        public DNodeList LobbyInfo
        {
            get { return new DNodeList { "0", Name, LobbyID, Constants.LOBBY_SERVER_ID, "0", "2068", "0", "", "SETTLERSHOK", "SETTLERSHOK", new byte[0], "0", "500", "1" }; }
        }

        public event EventHandler<Message> LobbyMessage;

        public void Broadcast(Message msg)
        {
            LobbyMessage?.Invoke(this, msg);
        }

        public GameRoom CreateRoom(string name, byte[] gameInfo, PlayerAccount host)
        {
            var room = new GameRoom(name, nextRoomID, LobbyID, gameInfo, host);
            Rooms.Add(nextRoomID, room);
            nextRoomID--;
            return room;
        }

        public GameRoom GetRoom(int roomID)
        {
            GameRoom gr = null;
            Rooms.TryGetValue(roomID, out gr);
            return gr;
        }


        #region static

        static int nextLobbyID = 870;

        static Dictionary<int, Lobby> lobbies = new Dictionary<int, Lobby>();

        public static IEnumerable<Lobby> AllLobbies { get { return lobbies.Values; } }

        public static Lobby Get(int id)
        {
            Lobby lb = null;
            lobbies.TryGetValue(id, out lb);
            return lb;
        }

        public static void AddLobbies(params string[] names)
        {
            foreach (var name in names)
            {
                lobbies.Add(nextLobbyID, new Lobby(name, nextLobbyID));
                nextLobbyID++;
            }
        }

        #endregion
    }

    public class GameRoom
    {
        public string Name { get; protected set; }
        public int ID { get; protected set; }
        protected int lobbyID;

        public List<PlayerAccount> Players = new List<PlayerAccount>();
        public PlayerAccount Host { get; protected set; }
        public byte[] GameInformation { get; protected set; }

        public GameRoom(string name, int id, int lobbyId, byte[] gameInfo, PlayerAccount host)
        {
            Name = name;
            ID = id;
            lobbyID = lobbyId;
            GameInformation = gameInfo;
            Host = host;
           // Players.Add(host);
        }

        public DNodeList RoomInfo
        {
            //["7" "yoq4711yoq's Spiel-20:21:46" "-126" "51" "871" "2098" "1" "yoq4711yoq" "SETTLERSHOK" "SETTLERSHOK" Bin{00 2C 6B 02 02 00 00 00 73 DF DD E9 D8 2C 6B 02} "0" "8" "2" "0" "0" "SHOKPC1.05" "SHOKPC1.05" "84.115.212.253" "10.9.9.9"]
            
            get { return new DNodeList { 7, Name, ID, Constants.LOBBY_SERVER_ID, lobbyID, 2098, 1, Host.Username, "SETTLERSHOK", "SETTLERSHOK", GameInformation, 0, 8, 2, 0, 0, "SHOKPC1.05", "SHOKPC1.05", Host.PublicIP, Host.LocalIP }; }
        }

        public event EventHandler<Message> RoomMessage;

        public void Broadcast(Message msg)
        {
            RoomMessage?.Invoke(this, msg);
        }

    }
}
