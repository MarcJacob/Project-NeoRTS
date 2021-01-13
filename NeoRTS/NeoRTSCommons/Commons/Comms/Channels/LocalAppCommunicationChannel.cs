using UnityEngine;
using System;
using NeoRTS.Communication.Messages;
using System.IO;

namespace NeoRTS
{
    namespace Communication
    {
        /// <summary>
        /// Most primitive implementation of IClientServerCommInterface.
        /// Relies on the Server and Client interfaces (and the Client and Server themselves) being in the same scene.
        /// All it requires is for the corresponding interface object (presumably another of the same type) to be linked
        /// through the otherInterface property.
        /// 
        /// (Will probably be the only Comm interface type that has a direct reference to what it's communicating with).
        /// </summary>
        public class LocalAppCommunicationChannel : CommunicationChannel
        {
            static public void LinkCommunicationChannels(LocalAppCommunicationChannel a, LocalAppCommunicationChannel b)
            {
                a.targetChannel = b;
                b.targetChannel = a;
            }

            private LocalAppCommunicationChannel targetChannel;

            public LocalAppCommunicationChannel()
            {
            }



            protected override void Send(MESSAGE message)
            {
                targetChannel.OnDataReceived(message.data);
            }

            private void OnDataReceived(byte[] data)
            {
                MESSAGE message;
                message = new MESSAGE(data);
                DispatchReceivedMessage(message);
            }

            protected override void OnOpen()
            {
                if (targetChannel != null)
                {
                    State = CHANNEL_STATE.READY;
                }
                else
                {
                    FailChannel("Target channel was not set !");
                    throw new Exception("ERROR : Local App Channel 'targetChannel' property was not set !");
                }
            }

            protected override void OnClose()
            {
            }
        }
    }

}

