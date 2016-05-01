using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServices
{
    public static class XorCrypt
    {
        static int GetRectSize(int len)
        {
            var squared = (int)Math.Sqrt(len);

            if (squared * squared == len)
                return squared;
            else
                return squared + 1;
        }

        static IEnumerable<KeyValuePair<int, int>> SnakeIterator(int rectLen, int maxLen)
        {
            int col = 0, row = 0, i = 0;

            do
            {
                if (col < rectLen)
                {
                    if (row < 0)
                    {
                        row = col;
                        col = 0;
                    }
                }
                else
                {
                    col = row + 2;
                    row = rectLen - 1;
                }
                var offset = col++ + rectLen * row--;
                yield return new KeyValuePair<int, int>(i, offset);
                ++i;
            }
            while (i < maxLen);
            yield break;
        }

        static void SimpleXor(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= (byte)(i - 0x77);
        }

        public static void Decrypt(byte[] data)
        {
            var rectLen = GetRectSize(data.Length);

            var workspace = new byte[rectLen * rectLen];

            foreach (var entry in SnakeIterator(rectLen, data.Length))
                workspace[entry.Value] = 1;

            for (int i = 0, j = 0; j < data.Length; i++)
                if (workspace[i] == 1)
                    workspace[i] = data[j++];

            foreach (var entry in SnakeIterator(rectLen, data.Length))
                data[entry.Key] = workspace[entry.Value];

            SimpleXor(data);
        }

        public static void Encrypt(byte[] data)
        {
            var rectLen = GetRectSize(data.Length);

            SimpleXor(data);

            var workspace = new UInt16[rectLen * rectLen];

            for (int i = 0; i < workspace.Length; i++)
                workspace[i] = 0xffff;

            foreach (var entry in SnakeIterator(rectLen, data.Length))
                workspace[entry.Value] = data[entry.Key];

            for (int i = 0, j = 0; i < workspace.Length; i++)
                if (workspace[i] != 0xffff)
                    data[j++] = (byte)workspace[i];
        }

    }
}
