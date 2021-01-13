using NeoRTS.Communication;
using NeoRTS.Communication.Networking;
using NeoRTS.GameData;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NeoRTS
{
    namespace Communication
    {
        namespace Networking
        {
            /// <summary>
            /// Base networking code shared by every entity on our networked architecture (IE the Clients, and all kinds of Servers).
            /// Handles creation of Networked Communication Channels and updating them so that they react to receiving data.
            /// </summary>
            public class NetworkingManager : ManagerObject
            {

                public List<NetworkCommunicationChannel> m_networkChannels;


                public NetworkingManager()
                {
                    m_networkChannels = new List<NetworkCommunicationChannel>();
                }

                public NetworkCommunicationChannel CreateNetworkChannelObject(string ip, int port)
                {
                    
                    Socket socket; IPEndPoint endpoint;
                    endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                    socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    return CreateNetworkChannelObject(socket, endpoint);
                }

                public NetworkCommunicationChannel CreateNetworkChannelObject(Socket socket, EndPoint endPoint = null)
                {
                    NetworkCommunicationChannel channel;
                    channel = new NetworkCommunicationChannel(socket, endPoint != null ? endPoint : socket.RemoteEndPoint);

                    m_networkChannels.Add(channel);
                    return channel;
                }

                public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
                {
                    
                }

                public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
                {
                    
                }

                protected override void OnManagerInitialize()
                {

                }

                protected override void OnManagerUpdate(float deltaTime)
                {
                    foreach(var channel in m_networkChannels)
                    {
                        if (channel.State == CHANNEL_STATE.READY && channel.NetworkSocket.Poll(0, SelectMode.SelectRead))
                        {
                            channel.ReadReceivedSocketData();
                        }
                    }


                    List<NetworkCommunicationChannel> closedOrFailedChannels = new List<NetworkCommunicationChannel>();
                    foreach(var channel in m_networkChannels)
                    {
                        if (channel.State == CHANNEL_STATE.CLOSED || channel.State == CHANNEL_STATE.FAILED)
                        {
                            closedOrFailedChannels.Add(channel);
                        }
                    }

                    foreach(var channel in closedOrFailedChannels)
                    {
                        m_networkChannels.Remove(channel);
                    }
                }
            }
        }
    }
}