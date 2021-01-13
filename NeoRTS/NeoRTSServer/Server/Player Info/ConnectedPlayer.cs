using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.Tools;
using System;

namespace NeoRTS
{
    namespace Server
    {
        namespace Players
        {
            /// <summary>
            /// ConnectedPlayer represents an instance of a player's session on the server.
            /// Provides an event that triggers when it gets disconnected AND its own Message Dispatcher
            /// which transmits every message sent by the corresponding client.
            /// Its lifecycle isn't necessarily equal exactly to the connection time of the corresponding client.
            /// Namely, it (TODO) doesn't get disconnected if the disconnection is really short in time.
            /// 
            /// It is analoguous to a Session on Web apps.
            /// </summary>
            public class ConnectedPlayer
            {
                private string m_name;
                private MessageDispatcher m_playerMessagesDispatcher;
                private int m_communicationChannelID;

                public string Name { get { return m_name; } }
                public int CommunicationChannelID { get { return m_communicationChannelID; } }
                public bool Connected { get; private set; }
                public bool ConnectionChannelAlive { get { return m_communicationChannelID >= 0; } }

                public event Action OnPlayerDisconnected = delegate { };

                public MessageDispatcher PlayerMessagesDispatcher
                {
                    get
                    {
                        return m_playerMessagesDispatcher;
                    }
                }

                public ConnectedPlayer(string name)
                {
                    m_name = name;
                    m_playerMessagesDispatcher = new MessageDispatcher();
                    m_communicationChannelID = -1;
                    Connected = true;
                }

                public void AssignChannel(int id, MessageDispatcher sourceMessageDispatcher)
                {
                    if (ConnectionChannelAlive)
                    {
                        throw new Exception("ERROR : Attempted to assign new channel to Connected Player without first unassigning its existing one.");
                    }
                    m_communicationChannelID = id;
                    sourceMessageDispatcher.RegisterOnMessageReceivedHandler(id, m_playerMessagesDispatcher.DispatchMessage);
                }

                public void UnassignCurrentChannel(MessageDispatcher sourceMessageDispatcher)
                {
                    m_communicationChannelID = -1;
                    sourceMessageDispatcher.UnregisterOnMessageReceivedHandler(m_playerMessagesDispatcher.DispatchMessage);
                }

                public void SetDisconnected()
                {
                    Debug.Log("Player " + m_name + " has disconnected !");
                    Connected = false;
                    OnPlayerDisconnected();
                }

                public override bool Equals(object obj)
                {
                    return (obj is ConnectedPlayer) && ((ConnectedPlayer)obj).m_communicationChannelID == m_communicationChannelID;
                }

                public override int GetHashCode()
                {
                    return m_name.GetHashCode() + m_communicationChannelID;
                }
            }
        }
    }
}

