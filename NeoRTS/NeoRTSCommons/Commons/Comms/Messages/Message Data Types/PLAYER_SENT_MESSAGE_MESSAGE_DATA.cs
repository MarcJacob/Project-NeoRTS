using NeoRTS.Tools;
using System;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {

            namespace Chat
            {
                public unsafe struct PLAYER_SENT_MESSAGE_MESSAGE_DATA : IMessageData
                {
                    public const int CHAT_MESSAGE_MAX_LENGTH = 256;

                    public PLAYER_SENT_MESSAGE_MESSAGE_DATA(string senderStr, string msg)
                    {
                        byte[] encodedSender = StringEncoding.Encode(senderStr);
                        byte[] encodedMessage = StringEncoding.Encode(msg);

                        senderByteCount = encodedSender.Length;
                        messageByteCount = encodedMessage.Length;

                        fixed (byte* senderPtr = encodedSender)
                        {
                            fixed (byte* thisSenderPtr = sender)
                                Buffer.MemoryCopy(senderPtr, thisSenderPtr, PLAYER_AUTHENTIFICATION_MESSAGE_DATA.PLAYER_NAME_MAX_CHAR_LENGTH * 4, encodedSender.Length);
                        }

                        fixed (byte* messagePtr = encodedMessage)
                        {
                            fixed (byte* thisMessagePtr = message)
                            {
                                Buffer.MemoryCopy(messagePtr, thisMessagePtr, CHAT_MESSAGE_MAX_LENGTH * 4, encodedMessage.Length);
                            }
                        }


                    }

                    public int senderByteCount;
                    public fixed byte sender[PLAYER_AUTHENTIFICATION_MESSAGE_DATA.PLAYER_NAME_MAX_CHAR_LENGTH];

                    public int messageByteCount;
                    public fixed byte message[CHAT_MESSAGE_MAX_LENGTH];
                }
            }
        }
    }
}

