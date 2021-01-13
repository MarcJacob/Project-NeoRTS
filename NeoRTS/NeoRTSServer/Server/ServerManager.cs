using NeoRTS.GameData;
using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using System;
using NeoRTS.Server.Networking;
using NeoRTS.GameData.ObjectData;

namespace NeoRTS
{
    namespace Server
    {
        /// <summary>
        /// "Nerve center" of the server application. Receives messages from X amount of clients through
        /// a (many ?) IClientServerCommInterface(s) and dispatches them to server-side systems.
        /// (Maybe also make all messages we want to send transit through here ?)
        /// 
        /// (TODO : Consider breaking this up into multiple server types depending on what they'll be doing.
        /// - "Master server" : handles account connection and persists accross games. Handles matchmaking ?
        /// - "Match server" : handles actual gameplay and state updates during a match. ) 
        /// </summary>
        public class ServerManager
        {
            public static ServerManager Instance { get; private set; }

            private int ticksPerSecond = 60;
            private float m_gameStateUpdateCooldown = 0.1f;
            private const int PLAYERS_PER_MATCH = 2;

            private ChannelCollection m_communicationChannels;
            private MessageDispatcher m_messageDispatcher;
            public ManagersContainer Managers { get; private set; }

            public ServerManager()
            {
                Instance = this;
                m_communicationChannels = new ChannelCollection();
                m_messageDispatcher = new MessageDispatcher();

                MatchesManager matchesManager;
                ServerNetworkingManager serverNetworkingManager;
                ConnectedPlayersManager connectedPlayersManager;
                MatchmakingManager matchMakingManager;
                ChatChannelsManager chatManager;
                {
                    matchesManager = new MatchesManager(120);
                    serverNetworkingManager = new ServerNetworkingManager(25556, 30000);
                    connectedPlayersManager = new ConnectedPlayersManager();
                    matchMakingManager = new MatchmakingManager(matchesManager, connectedPlayersManager, PLAYERS_PER_MATCH);
                    chatManager = new ChatChannelsManager();

                    Managers = new ManagersContainer(   matchesManager, 
                                                        serverNetworkingManager, 
                                                        connectedPlayersManager, 
                                                        matchMakingManager,
                                                        chatManager);
                }
            }

            public void Start()
            {
                Managers.InitializeManagers();
                Managers.InitializeManagersMessageReception(m_messageDispatcher);

                ObjectDataTypeDatabase.BuildDatabase();
            }

            public void Update(float deltaTime)
            {
                Managers.UpdateManagers(deltaTime);

                m_gameStateUpdateCooldown -= deltaTime;
                if (m_gameStateUpdateCooldown < 0f)
                {
                    var messages = Managers.RetrieveAllManagersMessages();

                    foreach (var message in messages)
                        SendMessage(message);

                    m_gameStateUpdateCooldown = 1f / ticksPerSecond;
                }

                m_communicationChannels.RemoveClosedAndFailedChannels();

                string msg;
                if (Tools.Debug.PollMessage(out msg))
                {
                    Console.WriteLine(msg);
                }
            }

            public void SendMessage(MESSAGE msg)
            {
                if (msg.channelIDs.Count == 1)
                {
                    m_communicationChannels[msg.ChannelID].SendMessage(msg);
                }
                else if (msg.channelIDs.Count > 1)
                {
                    foreach(var channelID in msg.channelIDs)
                    {
                        m_communicationChannels[channelID].SendMessage(msg);
                    }
                }
            }

            public void AddChannel(CommunicationChannel newChannel)
            {
                newChannel.AssignMessageDispatcher(m_messageDispatcher);
                m_communicationChannels.AddChannel(newChannel);
            }

            public CommunicationChannel GetCommunicationChannelFromID(int ID)
            {
                var channel = m_communicationChannels[ID];
                if (channel == null) throw new Exception("ERROR : Attempted to retrieve NULL channel from ID " + ID);
                return channel;
            }
        }
    }
}

