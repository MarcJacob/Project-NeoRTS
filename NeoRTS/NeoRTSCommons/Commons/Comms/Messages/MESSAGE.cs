using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            /// <summary>
            /// MESSAGES are a wrapper for a certain amount of "packed" data. They internally only contain an array of bytes a reader (Header property) that's able
            /// to determine the Message's type by reading the first few bytes to construct a MESSAGE_TYPE object.
            /// </summary>
            [Serializable]
            public struct MESSAGE
            {
                public List<int> channelIDs;

                /// <summary>
                /// The first element of the Channel IDs list. For messages that were just received, this is equal to
                /// the reception channel's ID. For messages staged to be sent, this is equal to the first channel (and sometimes
                /// only channel) for the message to be sent through.
                /// </summary>
                public int ChannelID { get { return channelIDs[0]; } set { channelIDs[0] = value; } }

                public MESSAGE(byte[] data)
                {
                    this.data = data;
                    channelIDs = new List<int>() { 0 };
                }

                public unsafe MESSAGE_TYPE Header
                {
                    get
                    {
                        if (data.Length >= sizeof(MESSAGE_TYPE))
                        {
                            fixed (byte* dataPtr = data)
                            {
                                return *((MESSAGE_TYPE*)dataPtr);
                            }
                        }
                        else
                        {
                            throw new Exception("ERROR - Attempted to read Message header before it was properly constructed !");
                        }

                    }
                }


                public byte[] data;
            }

            public interface IMessageData
            {
            }
        }
    }
}

