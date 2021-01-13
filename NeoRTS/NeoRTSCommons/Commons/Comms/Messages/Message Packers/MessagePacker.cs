using NeoRTS.Tools;
using System;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            /// <summary>
            /// MessagePacker objects are objects able to Pack and Unpack Messages. This works by supplying the Message header
            /// this Packer works with at construction or in the constructor if this particular MessagePacker type always works with the
            /// same header.
            /// 
            /// A single Packer can only manage one message header (IE one message type !). The packers possess the information needed
            /// to properly construct a MESSAGE object (basically a fancy wrapper for a byte[] array) from some data and the header
            /// they always work on.
            /// 
            /// Defines a Pack() function which allows packing a data structure of type T into a MESSAGE object with the appropriate header.
            /// Defines a Unpack() function which allows unpacking a data structure of type T from a MESSAGE object given it has the correct header.
            /// </summary>
            public abstract class MessagePacker<T> where T : struct
            {
                public const int DEFAULT_MAX_MESSAGE_SIZE = 1024;

                private MESSAGE_TYPE messageHeader;
                protected MESSAGE_TYPE MessageHeader
                {
                    get { return messageHeader; }
                }

                public MessagePacker(MESSAGE_TYPE type)
                {
                    messageHeader = type;
                }

                public unsafe MESSAGE PackMessage(T data, int maxMessageSize = DEFAULT_MAX_MESSAGE_SIZE)
                {
                    bool packingSucceeded = false;
                    byte[] packedData = new byte[maxMessageSize];
                    while (!packingSucceeded)
                    { 
                        try
                        {
                            fixed (byte* packedDataPtr = packedData)
                            {
                                MESSAGE_TYPE* msgTypePtr = (MESSAGE_TYPE*)packedDataPtr;
                                *msgTypePtr = messageHeader;

                                PackData(data, packedDataPtr + sizeof(MESSAGE_TYPE), maxMessageSize - sizeof(MESSAGE_TYPE));
                            }
                            packingSucceeded = true;
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            // This very likely means that we tried to encode too much data. Try again with a large size
                            Debug.LogWarning("WARNING : Message size was too low ! Consider implementing a chunking system.");
                            maxMessageSize *= 2;
                            packedData = new byte[maxMessageSize];
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }
                    return new MESSAGE(packedData);
                }

                public unsafe T UnpackMessage(MESSAGE message)
                {
                    if (message.Header != messageHeader)
                    {
                        throw new Exception("ERROR - Called the wrong MessagePacker object for the message of type " + message.Header + " ! This MessagePacker handles type " + messageHeader);
                    }

                    try
                    {
                        fixed (byte* dataPtr = message.data)
                        {
                            return UnpackData(dataPtr + sizeof(MESSAGE_TYPE), message.data.Length - sizeof(MESSAGE_TYPE));
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }

                protected unsafe abstract void PackData(T data, byte* packInto, int byteCountForData);
                protected unsafe abstract T UnpackData(byte* dataPtr, int dataByteSize);
            }
        }
    }
}

