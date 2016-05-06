using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServices
{
    public class Message
    {
        public MessageType Type;
        public MessageCode Code;
        public DNodeList Data;
        public DNodeList LobbyData { get { return Data[1] as DNodeList; } }
        public byte[] Body;
        public byte NumA, NumB;


        public Message(MessageCode code, DNodeList args) : this(MessageType.GSMessage, code, args, 1, 4) { }

        public Message(MessageType type, MessageCode code, DNodeList args, byte numA, byte numB)
        {
            Type = type;
            Code = code;
            if (args == null)
                args = new DNodeList();
            Data = args;
            NumA = numA;
            NumB = numB;
        }

        public Message(LobbyMessageCode lobbyCode, DNodeList lobbyArgs) : this(MessageType.GSMessage, lobbyCode, lobbyArgs, 1, 4) { }

        public Message(MessageType type, LobbyMessageCode lobbyCode, DNodeList lobbyArgs, byte numA, byte numB)
        {
            Type = type;
            Code = MessageCode.LOBBY_MSG;
            if (lobbyArgs == null)
                lobbyArgs = new DNodeList();
            Data = new DNodeList { (int)lobbyCode, lobbyArgs };
            NumA = numA;
            NumB = numB;
        }

        public Message SuccessResponse(DNodeList args = null)
        {
            return new Message(MessageType.GSMessage, MessageCode.GSSUCCESS, new DNodeList { (byte)Code, args }, 1, 4);
        }

        public Message FailResponse(DNodeList args = null)
        {
            return new Message(MessageType.GSMessage, MessageCode.GSFAIL, new DNodeList { (byte)Code, args }, 1, 4);
        }

        public Message LobbySuccessResponse(DNodeList args = null)
        {
            return new Message(MessageType.GSMessage, LobbyMessageCode.LB_GSSUCCESS, new DNodeList { Data[0].AsString, args }, 1, 4);
        }

        public Message LobbyFailResponse(DNodeList args = null)
        {
            return new Message(MessageType.GSMessage, LobbyMessageCode.LB_GSFAIL, new DNodeList { Data[0].AsString, args }, 1, 4);
        }


        public byte[] Serialize(Blowfish blowfishContext = null)
        {
            var msgData = Data.Serialize();
            if (msgData.Length > 0)
            {
                switch (Type)
                {
                    case MessageType.GSMessage:
                        XorCrypt.Encrypt(msgData);
                        break;

                    case MessageType.GSEncryptMessage:
                        if (blowfishContext == null)
                            throw new Exception("Missing blowfish key!");

                        blowfishContext.EncipherPadded(ref msgData);
                        break;

                    case MessageType.GameMessage:
                        throw new NotImplementedException("Sending clGameMessage, WAT DO?");
                }
            }

            var msgLen = msgData.Length + 6;
            var message = new byte[msgLen];

            message[0] = (byte)(msgLen >> 16);
            message[1] = (byte)(msgLen >> 8);
            message[2] = (byte)msgLen;
            message[3] = (byte)((int)Type << 6);
            message[4] = (byte)Code;
            message[5] = (byte)(NumA << 4 | (NumB & 0x0F));
            Array.Copy(msgData, 0, message, 6, msgData.Length);

            return message;
        }

        public static IEnumerable<Message> ParseIncoming(byte[] data, Blowfish blowfishContext = null)
        {
            for (int pos = 0; pos < data.Length;)
            {
                if (pos + 6 > data.Length)
                    throw new Exception("Incomplete Header received!");

                int packetSize = data[pos + 2] + 256 * data[pos + 1] + 256 * 256 * data[pos + 0];
                MessageType type = (MessageType)(data[pos + 3] >> 6);
                MessageCode code = (MessageCode)(data[pos + 4]);
                byte numA = (byte)(data[pos + 5] >> 4);
                byte numB = (byte)(data[pos + 5] & 0x0F);

                if (pos + packetSize > data.Length)
                    throw new Exception("Incomplete message body received!");

                var argLen = packetSize - 6;
                byte[] msgData = new byte[argLen];

                if (argLen != 0)
                {
                    Array.Copy(data, pos + 6, msgData, 0, argLen);

                    switch (type)
                    {
                        case MessageType.GSMessage:
                            XorCrypt.Decrypt(msgData);
                            break;

                        case MessageType.GSEncryptMessage:
                            if (blowfishContext == null)
                                throw new Exception("Received BF encrypted message, but no key available!");

                            blowfishContext.DecipherPadded(ref msgData);
                            break;

                        case MessageType.GameMessage:
                            throw new NotImplementedException("GameMessage received, WAT DO?");
                    }
                }

                yield return new Message(type, code, DNodeList.Parse(msgData), numA, numB);

                pos += packetSize;
            }
            yield break;
        }

        public override string ToString()
        {
            if (Code == MessageCode.LOBBY_MSG)
                return string.Format("{0}|{1} {2} [{3}, {4}]", NumA, NumB, Code.Description(), ((LobbyMessageCode)Data[0].AsInt).Description(), LobbyData.ToString());
            else
                return string.Format("{0}|{1} {2} {3}", NumA, NumB, Code.Description(), Data.ToString());
        }

    }

    public class CDKeyMessage
    {
        public DNodeList Data;

        public CDKeyMessage(DNodeList data)
        {
            Data = data;
        }

        public CDKeyMessage(byte[] data)
        {
            if (data[0] != 0xD3)
                throw new Exception("CDKey Message: Unknown packet!");

            int dataLen = data[4];
            var packetData = new byte[dataLen];
            Array.Copy(data, 5, packetData, 0, dataLen);
            Global.CDKeyCrypt.DecipherPadded(ref packetData);
            
            Data = DNodeList.Parse(packetData);
        }

        public byte[] Serialize()
        {
            var msgData = Data.Serialize();
            Global.CDKeyCrypt.EncipherPadded(ref msgData);

            var msgLen = msgData.Length + 5;
            var message = new byte[msgLen];

            message[0] = 0xD3;
            message[4] = (byte)msgData.Length;
            Array.Copy(msgData, 0, message, 5, msgData.Length);

            return message;
        }

        public override string ToString()
        {
            return Data.ToString();
        }
    }


    public enum MessageType : byte { GSMessage = 0, GameMessage = 1, GSEncryptMessage = 2 }
    public enum MessageCode : byte
    {
        UNKNOWN = 0,
        NEWUSERREQUEST = 0x1,
        CONNECTIONREQUEST = 0x2,
        PLAYERNEW = 0x3,
        DISCONNECTION = 0x4,
        PLAYERREMOVED = 0x5,
        NEWS = 0x7,
        SEARCHPLAYER = 0x8,
        REMOVEACCOUNT = 0x9,
        SERVERSLIST = 0xB,
        SESSIONLIST = 0xD,
        PLAYERLIST = 0xF,
        GETGROUPINFO = 0x10,
        GROUPINFO = 0x11,
        GETPLAYERINFO = 0x12,
        PLAYERINFO = 0x13,
        CHATALL = 0x14,
        CHATLIST = 0x15,
        CHATSESSION = 0x16,
        CHAT = 0x18,
        CREATESESSION = 0x1A,
        SESSIONNEW = 0x1B,
        JOINSESSION = 0x1C,
        JOINNEW = 0x1F,
        LEAVESESSION = 0x20,
        JOINLEAVE = 0x21,
        SESSIONREMOVE = 0x22,
        GSSUCCESS = 0x26,
        GSFAIL = 0x27,
        BEGINGAME = 0x28,
        UPDATEPLAYERINFO = 0x2D,
        MASTERCHANGED = 0x30,
        UPDATESESSIONSTATE = 0x33,
        URGENTMESSAGE = 0x34,
        NEWWAITMODULE = 0x36,
        KILLMODULE = 0x37,
        STILLALIVE = 0x3A,
        PING = 0x3B,
        PLAYERKICK = 0x3C,
        PLAYERMUTE = 0x3D,
        ALLOWGAME = 0x3E,
        FORBIDGAME = 0x3F,
        GAMELIST = 0x40,
        UPDATEADVERTISMEMENTS = 0x41,
        UPDATENEWS = 0x42,
        VERSIONLIST = 0x43,
        UPDATEVERSIONS = 0x44,
        UPDATEDISTANTROUTERS = 0x46,
        ADMINLOGIN = 0x47,
        STAT_PLAYER = 0x48,
        STAT_GAME = 0x49,
        UPDATEFRIEND = 0x4A,
        ADDFRIEND = 0x4B,
        DELFRIEND = 0x4C,
        LOGINWAITMODULE = 0x4D,
        LOGINFRIENDS = 0x4E,
        ADDIGNOREFRIEND = 0x4F,
        DELIGNOREFRIEND = 0x50,
        STATUSCHANGE = 0x51,
        JOINARENA = 0x52,
        LEAVEARENA = 0x53,
        IGNORELIST = 0x54,
        IGNOREFRIEND = 0x55,
        GETARENA = 0x56,
        GETSESSION = 0x57,
        PAGEPLAYER = 0x58,
        FRIENDLIST = 0x59,
        PEERMSG = 0x5A,
        PEERPLAYER = 0x5B,
        DISCONNECTFRIENDS = 0x5C,
        JOINWAITMODULE = 0x5D,
        LOGINSESSION = 0x5E,
        DISCONNECTSESSION = 0x5F,
        PLAYERDISCONNECT = 0x60,
        ADVERTISEMENT = 0x61,
        MODIFYUSER = 0x62,
        STARTGAME = 0x63,
        CHANGEVERSION = 0x64,
        PAGER = 0x65,
        LOGIN = 0x66,
        PHOTO = 0x67,
        LOGINARENA = 0x68,
        SQLCREATE = 0x6A,
        SQLSELECT = 0x6B,
        SQLDELETE = 0x6C,
        SQLSET = 0x6D,
        SQLSTAT = 0x6E,
        SQLQUERY = 0x6F,
        ROUTEURLIST = 0x7F,
        DISTANCEVECTOR = 0x83,
        WRAPPEDMESSAGE = 0x84,
        CHANGEFRIEND = 0x85,
        NEWRELFRIEND = 0x86,
        DELRELFRIEND = 0x87,
        NEWIGNOREFRIEND = 0x88,
        DELETEIGNOREFRIEND = 0x89,
        ARENACONNECTION = 0x8A,
        ARENADISCONNECTION = 0x8B,
        ARENAWAITMODULE = 0x8C,
        ARENANEW = 0x8D,
        NEWBASICGROUP = 0x8F,
        ARENAREMOVED = 0x90,
        DELETEBASICGROUP = 0x91,
        SESSIONSBEGIN = 0x92,
        GROUPDATA = 0x94,
        ARENA_MESSAGE = 0x97,
        ARENALISTREQUEST = 0x9D,
        ROUTERPLAYERNEW = 0x9E,
        BASEGROUPREQUEST = 0x9F,
        UPDATEPLAYERPING = 0xA6,
        UPDATEGROUPSIZE = 0xA9,
        SLEEP = 0xB3,
        WAKEUP = 0xB4,
        SYSTEMPAGE = 0xB5,
        SESSIONOPEN = 0xBD,
        SESSIONCLOSE = 0xBE,
        LOGINCLANMANAGER = 0xC0,
        DISCONNECTCLANMANAGER = 0xC1,
        CLANMANAGERPAGE = 0xC2,
        UPDATECLANPLAYER = 0xC3,
        PLAYERCLANS = 0xC4,
        GETPERSISTANTGROUPINFO = 0xC7,
        UPDATEGROUPPING = 0xCA,
        DEFERREDGAMESTARTED = 0xCB,
        NEWQUERY = 0xCC,                    //
        BEGINCLIENTHOSTGAME = 0xCD,
        LOBBY_MSG = 0xD1,
        LOBBYSERVERLOGIN = 0xD2,
        SETGROUPSZDATA = 0xD3,
        GROUPSZDATA = 0xD4,
        UPDATEPLAYERGROUPPING = 0xD8,
        RSAEXCHANGE = 0xDB,                 //
        ROUTERGLOBALSTATS = 0xDC,
        GETMOTD = 0xDE,                     //
    }

    public enum LobbyMessageCode : byte
    {
        LB_LOBBYSERVERADMLOGIN = 0x2,
        LB_LOBBYSERVERJOIN = 0x3,
        LB_SLEEP = 0x5,
        LB_WAKEUP = 0x6,
        LB_GROUPLEAVE = 0x8,
        LB_GROUPINFOGET = 0x9,
        LB_PLAYERKICK = 0xA,
        LB_LOBBYCREATE = 0xB,
        LB_ROOMCREATE = 0xC,
        LB_PARENTGROUPIDGET = 0xE,
        LB_GAMESTART = 0xF,
        LB_MATCHSTART = 0x11,
        LB_LOBBYSRVREMOVED = 0x13,
        LB_LOBBYSRVNEW = 0x14,
        LB_LOBBYLOGIN = 0x15,
        LB_LOBBYJOIN = 0x17,
        LB_ROOMJOIN = 0x18,
        LB_MASTERNEW = 0x1B,
        LB_MATCHRESULTS = 0x1E,
        LB_UPDATEGROUPSETTINGS = 0x1F,
        LB_UPDATEPING = 0x20,
        LB_GAMEREADY = 0x21,
        LB_GAMECONNECTED = 0x22,
        LB_GAMEFINISHED = 0x23,
        LB_PLAYERBAN = 0x24,
        LB_GETPLAYERBANNEDLIST = 0x25,
        LB_GSSUCCESS = 0x26,
        LB_GSFAIL = 0x27,
        LB_PLAYERUNBAN = 0x28,
        LB_GAMEUPDATEINFO = 0x29,
        LB_LOBBYINFO = 0x2A,                 //
        LB_PLAYERMATCHSTARTED = 0x2C,
        LB_MATCHFINISH = 0x2D,
        LB_MEMBERGROUPJOIN = 0x32,
        LB_MEMBERGROUPLEAVE = 0x33,
        LB_GROUPREMOVED = 0x34,
        LB_GROUPINFO = 0x35,
        LB_GROUPNEW = 0x36,
        LB_GROUPREMOVE = 0x37,
        LB_GAMESTARTED = 0x38,
        LB_GROUPCONFIGUPDATE = 0x39,
        LB_MASTERCHANGE = 0x3B,
        LB_MATCHSTARTED = 0x3E,
        LB_PLAYERBANNEDLIST = 0x40,
        LB_MATCHREADY = 0x41,
        LB_PLAYERGAMEFINISHED = 0x44,
        LB_PLAYERUPDATESTATUS = 0x45,
        LB_PLAYERMATCHFINISHED = 0x46,
        LB_URGENTMESSAGE = 0x65,
        LB_CONNECTIONREQUEST = 0x66,
        LB_MEMBERNEW = 0x67,
        LB_MEMBERREMOVE = 0x68,
        LB_NEWWAITMODULE = 0x69,
        LB_PLAYERGROUPGET = 0x6A,
        LB_REGISTERSERVER = 0x6B,
        LB_TERMINATESERVER = 0x6C,
        LB_GMLOGIN = 0x96,
        LB_GMMEMBERSLIST = 0x97,
        LB_GMMEMBERNEW = 0x98,
        LB_GMMEMBERREMOVE = 0x99,
        LB_GMMASTERCHANGE = 0x9A,
        LB_GMUPDATEGROUPSETTINGS = 0x9B,
        LB_GAMESTATS = 0xFA,
        LB_GETBASICLOBBIESINFO = 0xFB
    }

    public static class EnumExtensions
    {
        public static string Description(this MessageCode code)
        {
            if (Enum.IsDefined(typeof(MessageCode), code))
                return string.Format("{0}({1})", code.ToString(), (int)code);
            else
                return string.Format("UNKNOWN({0})", (int)code);
        }

        public static string Description(this LobbyMessageCode code)
        {
            if (Enum.IsDefined(typeof(LobbyMessageCode), code))
                return string.Format("{0}(\"{1}\")", code.ToString(), (int)code);
            else
                return string.Format("LB_UNKNOWN({0})", (int)code);
        }
    }
}
