using System.Text;

namespace NeoRTS
{
    namespace Tools
    {
        public unsafe static class StringEncoding
        {
            static public byte[] Encode(string str)
            {
                return UTF32Encoding.UTF32.GetBytes(str);
            }

            static public string Decode(byte[] bytes)
            {
                fixed(byte* ptr = bytes)
                {
                    return Decode(ptr, bytes.Length);
                }
            }

            static public string Decode(byte* bytes, int byteSize)
            {
                return UTF32Encoding.UTF32.GetString(bytes, byteSize);
            }
        }
    }
}
