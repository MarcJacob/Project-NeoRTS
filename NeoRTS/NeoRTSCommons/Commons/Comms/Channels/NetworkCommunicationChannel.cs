using NeoRTS.Communication.Messages;
using NeoRTS.Tools;
using System.Net;
using System.Net.Sockets;
using System;

namespace NeoRTS
{
    namespace Communication
    {
        public class NetworkCommunicationChannel : CommunicationChannel
        {
            private const int RECEIVE_BUFFER_SIZE = 1024 * 10;
            private const int SEND_BUFFER_SIZE = 1024 * 10;

            private Socket m_socket;
            private EndPoint m_endPoint;
            private DateTime m_lastReceiveTime;

            public DateTime LastReceiveTime
            {
                get { return m_lastReceiveTime; }
            }

            public Socket NetworkSocket { get { return m_socket; } }

            public NetworkCommunicationChannel(Socket socket, EndPoint endPoint)
            {
                m_socket = socket;
                m_endPoint = endPoint;

                m_socket.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;
                m_socket.SendBufferSize = SEND_BUFFER_SIZE;
            }

            public NetworkCommunicationChannel(Socket socket, string address, int port)
            {
                m_socket = socket;

                m_socket.Connect(address, port);
                m_endPoint = m_socket.RemoteEndPoint;

                m_socket.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;
                m_socket.SendBufferSize = SEND_BUFFER_SIZE;
            }

            protected override void OnOpen()
            {
                try
                {
                    if (m_socket.Connected == false)
                    {
                        m_socket.Connect(m_endPoint);
                    }

                    if (m_socket.Connected)
                        State = CHANNEL_STATE.READY;
                    else
                        FailChannel("Could not connect to remote endpoint.");
                }
                catch (SocketException e)
                {
                    FailChannel(e.Message);
                    throw e;
                }
            }

            protected override void Send(MESSAGE message)
            {
                try
                {
                    m_socket.Send(message.data);
                }
                catch
                {
                    Debug.Log("Networked Channel ID " + ChannelID + " failed to send data. Closing.");
                    CloseChannel();
                }
            }

            public void ReadReceivedSocketData()
            {
                byte[] buffer;
                try
                {
                    buffer = new byte[RECEIVE_BUFFER_SIZE];

                    m_socket.Receive(buffer);

                    MESSAGE message = new MESSAGE(buffer);

                    if (message.Header == MESSAGE_TYPE.PING)
                    {
                        byte[] pongMessageData = new byte[]
                        {
                                1,0,0,0
                        };
                        m_socket.Send(pongMessageData);
                    }
                    else if (message.Header != MESSAGE_TYPE.PING && message.Header != MESSAGE_TYPE.PONG)
                    {
                        DispatchReceivedMessage(message);
                    }
                    m_lastReceiveTime = DateTime.Now;
                }
                catch
                {
                    Debug.Log("Networked Channel ID " + ChannelID + " failed to receive data. Closing.");
                    CloseChannel();
                }
            }

            protected override void OnClose()
            {
                // Send whatever we need to send for a "clean" disconnect
                m_socket.Close();
            }
        }
    }

}

