using System;

namespace NeoRTS
{
    namespace Tools
    {

        public static class BytesReader
        {
            static public object ReadFromBytes(byte[] bytes, Type type)
            {
                if (type  == typeof(int))
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
                else if (type == typeof(uint))
                {
                    return BitConverter.ToUInt32(bytes, 0);
                }
                else if (type == typeof(bool))
                {
                    return BitConverter.ToBoolean(bytes, 0);
                }
                else if (type == typeof(float))
                {
                    return BitConverter.ToSingle(bytes, 0);
                }
                else
                    throw new Exception("Error - Unsupported type !");
            }
        }
    }
}
