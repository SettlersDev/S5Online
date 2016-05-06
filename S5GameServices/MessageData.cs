using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServices
{
    public interface IDNode
    {
        bool IsList { get; }

        void Serialize(List<byte> dataBlock);


        //now this is a crap solution but its faster than casts and less of an eyesore
        //throws execeptions if you dont get a compatible type though

        IDNode this[int i] { get; }
        string AsString { get; set; }
        int AsInt { get; set; }
        byte[] AsBinary { get; set; }

    }

    // DNodeList can be instatiated using a short form:
    // var d = new DNodeList { (byte)0x22, "foo", { 33, "bar", new byte[3] }, 77 };
    // Result:              [ Bin{ 22 }, "foo", [ "33", "bar", Bin{ 00, 00, 00 } ], "77" ]
    // Nesting of one level doesnt even require "new DNodeList", { } is good enough
    // further nesting, does need explicit DNodeList instatiation

    // Auto-Mapping:
    //   DNodeString: string, int
    //   DNodeBinary: byte, byte[]

    public class DNodeList : List<IDNode>, IDNode
    {
        public bool IsList { get { return true; } }

        public int AsInt { get { throw new Exception(); } set { throw new Exception(); } }
        public string AsString { get { throw new Exception(); } set { throw new Exception(); } }

        public byte[] AsBinary { get { throw new Exception(); } set { throw new Exception(); } }

        public DNodeList()
        { }

        public void Add(object obj)
        {
            if (obj == null)
                return;
            else if (obj is IDNode)
                base.Add(obj as IDNode);
            else if (obj is byte)
                Add((byte)obj);
            else if (obj is int)
                Add((int)obj);
            else if (obj is string)
                Add((string)obj);
            else if (obj is byte[])
                Add((byte[])obj);
            else
                throw new Exception("Unsupported data type for DNode!");
        }
        public void Add(byte b) { base.Add(new DNodeBinary(b)); }
        public void Add(byte[] data) { base.Add(new DNodeBinary(data)); }
        public void Add(int number) { base.Add(new DNodeString(number)); }
        public void Add(string str) { base.Add(new DNodeString(str)); }

        public void Add(params object[] subListValues)
        {
            var dn = new DNodeList();
            foreach (var obj in subListValues)
                dn.Add(obj);
            base.Add(dn);
        }

        public void Add(IEnumerable<IDNode> nodes)
        {
            if (nodes == null)
                return;

            var dn = new DNodeList();
            foreach (var obj in nodes)
                dn.Add(obj);
            base.Add(dn);
        }


        public static DNodeList Parse(byte[] data)
        {
            int pos = 0;
            return new DNodeList(data, ref pos);
        }

        protected DNodeList(byte[] data, ref int pos)
        {
            for (; pos < data.Length;)
            {
                byte type = data[pos];
                pos++;

                switch (type)
                {
                    case (byte)'s':
                        int endPos;
                        for (endPos = pos; endPos < data.Length && data[endPos] != 0; endPos++) ;
                        Add(new DNodeString(Global.ServerEncoding.GetString(data, pos, endPos - pos)));
                        pos = endPos + 1;
                        break;

                    case (byte)'b':
                        int binLen = (data[pos] << 24) + (data[pos + 1] << 16) + (data[pos + 2] << 8) + data[pos + 3];

                        if (pos + 4 + binLen > data.Length)
                            throw new Exception("Binary DataElement too large!");

                        var binData = new byte[binLen];
                        Array.Copy(data, pos + 4, binData, 0, binLen);

                        Add(new DNodeBinary(binData));
                        pos += 4 + binLen;
                        break;

                    case (byte)'[':
                        var subList = new DNodeList(data, ref pos);
                        if (data[pos - 1] != (byte)']')
                            throw new Exception("List not finished!");
                        Add(subList);
                        break;

                    case (byte)']':
                        return;

                    default:
                        throw new Exception("Unknown Element in DataList!");

                }
            }
        }

        public byte[] Serialize()
        {
            var dataBlock = new List<byte>();
            Serialize(dataBlock);
            return dataBlock.ToArray();
        }

        public void Serialize(List<byte> dataBlock)
        {
            foreach (var elm in this)
            {
                if (elm.IsList)
                {
                    dataBlock.Add((byte)'[');
                    elm.Serialize(dataBlock);
                    dataBlock.Add((byte)']');
                }
                else
                    elm.Serialize(dataBlock);
            }
        }

        public override string ToString()
        {
            var str = "[";
            foreach (var v in this)
                str += v.ToString() + " ";
            return str.TrimEnd() + "]";
        }
    }

    public class DNodeString : IDNode
    {
        public bool IsList { get { return false; } }

        public string Text;
        public int AsInt { get { return int.Parse(Text); } set { Text = value.ToString(); } }

        public string AsString { get { return Text; } set { Text = value; } }

        public byte[] AsBinary { get { throw new Exception(); } set { throw new Exception(); } }

        public IDNode this[int n] { get { throw new Exception("NOPE, its a string!"); } }

        public DNodeString(int number)
        {
            Text = number.ToString();
        }
        public DNodeString(string text)
        {
            Text = text;
        }
        public void Serialize(List<byte> dataBlock)
        {
            dataBlock.Add((byte)'s');
            dataBlock.AddRange(Global.ServerEncoding.GetBytes(Text));
            dataBlock.Add(0);
        }

        public override string ToString()
        {
            return "\"" + Text + "\"";
        }
    }

    public class DNodeBinary : IDNode
    {
        public bool IsList { get { return false; } }

        public int AsInt
        {
            get { throw new NotImplementedException("would make sense to have this"); }
            set { throw new NotImplementedException("maybe"); }
        }
        public string AsString
        {
            get { throw new NotImplementedException("would make sense to have this"); }
            set { throw new NotImplementedException("maybe"); }
        }

        public byte[] AsBinary { get { return Data; } set { Data = value; } }

        public IDNode this[int n] { get { throw new Exception("NOPE, its a binary!"); } }

        public byte[] Data;

        public DNodeBinary(byte[] data)
        {
            Data = data;
        }
        public DNodeBinary(byte singleByte)
        {
            Data = new byte[] { singleByte };
        }

        public DNodeBinary(int dword)
        {
            Data = BitConverter.GetBytes(dword);
        }

        public void Serialize(List<byte> dataBlock)
        {
            dataBlock.Add((byte)'b');
            dataBlock.Add((byte)(Data.Length >> 24));
            dataBlock.Add((byte)(Data.Length >> 16));
            dataBlock.Add((byte)(Data.Length >> 8));
            dataBlock.Add((byte)(Data.Length));
            dataBlock.AddRange(Data);
        }

        public override string ToString()
        {
            return "Bin{" + BitConverter.ToString(Data).Replace("-", " ") + "}";
        }
    }
}
