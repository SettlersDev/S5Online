using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S5GameServices;
using System.IO;
using System.Collections;

namespace Testbed
{
    public enum DNodeType
    {
        Undefined,
        List,
        String,
        Binary
    }
    /*
    class DNode : IEnumerable<DNode>
    {
        public DNodeType Type = DNodeType.Undefined;
        List<DNode> subObjs;
        protected string stringValue;
        protected byte[] binBuffer;
        
        public DNode(int numberToString)
        {
            Type = DNodeType.String;
            stringValue = numberToString.ToString();
        }

        public DNode(string str)
        {
            Type = DNodeType.String;
            stringValue = str;
        }

        public DNode(byte[] binary)
        {
            Type = DNodeType.Binary;
            binBuffer = binary;
        }

        public void Add(int numberToString)
        {
            subObjs.Add(new DNode(numberToString));
        }

        public void Add(string str)
        {
            subObjs.Add(new DNode(str));
        }

        public void Add(DNode subList)
        {

        }


        public IEnumerator<DNode> GetEnumerator()
        {
            return subObjs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }*/

    class DN : IEnumerable
    {
        public void Add(object o) { }

        public void Add(params object[] oo)
        {
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(200, 30);

            // var dd = new DNode { 33, 22, "foo", new DNode { "foo", 42, 33 } };
          
            //RSAParameters p = new RSAParameters();
            //p.Exponent = new byte[] { 3 };
            //p.Modulus = File.ReadAllBytes("D:/reproj/shok/crypto/test/n");
            //p.D = File.ReadAllBytes("D:/reproj/shok/crypto/test/d");
            //p.P = File.ReadAllBytes("D:/reproj/shok/crypto/test/p");
            //p.Q = File.ReadAllBytes("D:/reproj/shok/crypto/test/q");
            //p.DP = File.ReadAllBytes("D:/reproj/shok/crypto/test/dp");
            //p.DQ = File.ReadAllBytes("D:/reproj/shok/crypto/test/dq");
            //p.InverseQ = File.ReadAllBytes("D:/reproj/shok/crypto/test/c");

            //var data = File.ReadAllBytes("D:/reproj/shok/crypto/test/resp");
            //var fishkey = Decryption(data, p, false);

            //var rk = new RsaKeyExchange();

            //var bf = new Blowfish("foo");// rk.Key);

            ////RSAParameters key = new RSAParameters();
            ////key.Exponent = new byte[] { 3 };
            ////key.Modulus = File.ReadAllBytes("D:/reproj/shok/crypto/test/n");

            ////var serverResp = Encryption(fishkey, key, false);

            //var mm = Message.ParseIncoming(File.ReadAllBytes("D:/reproj/shok/crypto/seq1_G2S.bin"), bf).ToArray();
            //var body = mm[0].Body;
            //Console.WriteLine(mm[0].Data.ToString());
            //File.WriteAllBytes("D:/reproj/shok/crypto/seq3_dec.bin", body);
            //mm = Message.ParseIncoming(File.ReadAllBytes("D:/reproj/shok/crypto/seq2_S2G.bin"), bf).ToArray();
            //Console.WriteLine(mm[0].Data.ToString());

            //var mm2 = Message.ParseIncoming(File.ReadAllBytes("D:/reproj/shok/crypto/respM2.bin"), bf).ToArray();
            //File.WriteAllBytes("D:/reproj/shok/crypto/seq3_dec.bin", mm2[2].Body);
            //File.WriteAllBytes("D:/reproj/shok/crypto/seq3_dec.bin", mm2[2].Serialize());

            //foreach (var m in mm2)
            //    Console.WriteLine(m.Data.ToString());

            Console.ReadKey();
        }
    }
}
