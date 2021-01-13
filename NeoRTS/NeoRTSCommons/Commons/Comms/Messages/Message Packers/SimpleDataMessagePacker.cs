using System;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            /// <summary>
            /// Packs and unpacks messages made up of a single unmanaged / "simple" data structure.
            /// </summary>
            public class SimpleDataMessagePacker<DATA_TYPE> : MessagePacker<DATA_TYPE> where DATA_TYPE : unmanaged, IMessageData
            {
                public SimpleDataMessagePacker(MESSAGE_TYPE type) : base(type)
                {

                }

                protected override unsafe void PackData(DATA_TYPE data, byte* packInto, int byteCountForData)
                {
                    Buffer.MemoryCopy(&data, packInto, byteCountForData, sizeof(DATA_TYPE));
                }

                protected override unsafe DATA_TYPE UnpackData(byte* dataPtr, int dataByteSize)
                {
                    if (dataByteSize < sizeof(DATA_TYPE))
                    {
                        throw new Exception("ERROR Unpacking data of type " + typeof(DATA_TYPE).ToString() + ". Size of received data isn't sufficient.");
                    }

                    return *(DATA_TYPE*)dataPtr;
                }
            }
        }
    }
}

