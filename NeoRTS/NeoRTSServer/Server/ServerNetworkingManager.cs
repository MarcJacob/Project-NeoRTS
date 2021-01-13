using NeoRTS.Communication.Networking;
using System;
using System.Net;
using System.Net.Sockets;

namespace NeoRTS
{
    namespace Server
    {
        namespace Networking
        {
            /// <summary>
            /// See <see cref="NetworkingManager"/> for base functionality.
            /// Server Networking Manager is an extension of Networking Manager which adds the ability to listen to pending
            /// connection requests and accept them (and get an event to react to the newly created Socket).
            /// </summary>
            public class ServerNetworkingManager : NetworkingManager
            {
                private const int LISTENING_PORT = 27015;
                private const int BACKLOG_SIZE = 100;
                private const float PINGING_PERIOD = 2f;

                private Socket m_listeningSocket;
                private float m_clock = 0f;

                public event Action<Socket> OnConnectionRequestAccepted = delegate { };

                public ServerNetworkingManager(int minPort, int maxPort)
                {

                }

                protected override void OnManagerInitialize()
                {
                    base.OnManagerInitialize();

                    m_listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    m_listeningSocket.Bind(new IPEndPoint(IPAddress.Any, LISTENING_PORT));

                    m_listeningSocket.Listen(BACKLOG_SIZE);
                }
                protected override void OnManagerUpdate(float deltaTime)
                {
                    base.OnManagerUpdate(deltaTime);
                    if (m_listeningSocket.Poll(0, SelectMode.SelectRead))
                    {
                        var socket = m_listeningSocket.Accept();
                        var channel = CreateNetworkChannelObject(socket);
                        ServerManager.Instance.AddChannel(channel);
                        OnConnectionRequestAccepted(socket);
                        Console.WriteLine("Connection request accepted. Created channel ID " + channel.ChannelID);
                    }

                    m_clock -= deltaTime;
                    if (m_clock < 0f)
                    {
                        m_clock = PINGING_PERIOD;
                        foreach(var channel in m_networkChannels)
                        {
                            byte[] pingMessageData = new byte[]
                            {
                                0,0,0,0
                            };
                            channel.NetworkSocket.Send(pingMessageData);
                        }

                        var now = DateTime.Now;
                        foreach (var channel in m_networkChannels)
                        {
                            if ((now - channel.LastReceiveTime).TotalSeconds > PINGING_PERIOD * 2f)
                            {
                               channel.CloseChannel();
                            }
                        }
                    }
                }
            }
        }
    }
}

