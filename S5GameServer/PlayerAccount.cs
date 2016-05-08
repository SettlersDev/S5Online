using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using S5GameServices;
using System.Xml;

namespace S5GameServer
{
    [Serializable]
    public class PlayerAccount
    {
        protected bool isInDB = false;

        protected string username;
        protected string ircAlias;
        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                ircAlias = "u" + BitConverter.ToString(MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(value))).Replace("-", "").Substring(0, 8);
            }
        }
        public string IrcAlias { get { return ircAlias; } }
        public string FirstName;
        public string LastName;
        public string Language;
        public string Email;

        protected byte[] passwordHash;
        protected byte[] salt = new byte[16];

        public void SetPassword(string password)
        {
            rng.GetBytes(salt);
            passwordHash = (new Rfc2898DeriveBytes(password, salt)).GetBytes(20);
        }
        public bool CheckPassword(string password)
        {
            var inputHash = (new Rfc2898DeriveBytes(password, salt)).GetBytes(20);

            for (int i = 0; i < inputHash.Length; i++)
                if (passwordHash[i] != inputHash[i])
                    return false;

            return true;
        }

        public PlayerAccount() { }
        protected PlayerAccount(bool inDB) { isInDB = inDB; }

        public void UpdateStore()
        {
            if (!isInDB)
                accountDB.Add(this.Username, this);

            isInDB = true;
            StoreDB();
        }

        #region Dynamic

        [NonSerialized]
        public string PublicIP;

        [NonSerialized]
        public string LocalIP;

        [NonSerialized]
        public string LocalSubnet;

        [NonSerialized]
        public byte[] GameIdentifier;

        [NonSerialized]
        public GameRoom Room;

        public DNodeList PlayerInfo
        {
            get { return new DNodeList { username, 0, PublicIP, LocalIP, GameIdentifier, new DNodeList { Room?.ID }, -1, 0 }; }
        }

        #endregion

        #region STATIC

        static PlayerAccount()
        {
            LoadDB();
        }

        protected static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        protected static Dictionary<string, PlayerAccount> accountDB;

        public static PlayerAccount Get(string accountName)
        {
            PlayerAccount pa;
            return accountDB.TryGetValue(accountName, out pa) ? pa : null;
        }

        public static bool IsNameAvailable(string accountName)
        {
            return !accountDB.ContainsKey(accountName);
        }

        static void StoreDB()
        {
            lock (accountDB)
            {
                var ser = new DataContractSerializer(typeof(PlayerAccount[]));
                using (var writer = XmlWriter.Create(ServerConfig.Instance.AccountsFile, new XmlWriterSettings { Indent = true }))
                {
                    ser.WriteObject(writer, accountDB.Values.ToArray());
                }
            }
        }

        static void LoadDB()
        {
            if (!File.Exists(ServerConfig.Instance.AccountsFile))
            {
                accountDB = new Dictionary<string, PlayerAccount>();
                return;
            }

            var ser = new DataContractSerializer(typeof(PlayerAccount[]));
            using (FileStream file = File.OpenRead(ServerConfig.Instance.AccountsFile))
            {
                accountDB = new Dictionary<string, PlayerAccount>();
                var accounts = (PlayerAccount[])ser.ReadObject(file);
                foreach (var acc in accounts)
                    accountDB.Add(acc.Username, acc);
            }
        }

        #endregion
    }
}
