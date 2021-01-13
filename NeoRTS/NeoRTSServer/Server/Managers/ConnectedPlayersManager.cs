using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using NeoRTS.Server.Players;
using NeoRTS.Tools;
using System;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace Server
    {
        /// <summary>
        /// "Entry point" manager for ConnectedPlayer objects. Picks up on newly connected channels
        /// sending in a Authentification message. If the authentification succeeds, then a new ConnectedPlayer
        /// object is loaded in along with its profile data (TODO) and the rest of the server gets notified via
        /// the OnPlayerConnectedEvent.
        /// 
        /// On top of holding all ConnectedPlayer objects in a list, this manager also manages their lifecycle
        /// by keeping track of their connection status and calling appropriate events.
        /// </summary>
        public class ConnectedPlayersManager : ManagerObject
        {
            private HashSet<ConnectedPlayer> m_connectedPlayersList;
            private Dictionary<int, ConnectedPlayer> m_channelIDToPlayerInfoDictionary;
            private MessageDispatcher m_parentMessageDispatcher;
            
            private SimpleDataMessagePacker<PLAYER_AUTHENTIFICATION_MESSAGE_DATA> m_authentificationMessagePacker;
            private SimpleDataMessagePacker<PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA> m_authentificationResponseMessagePacker;

            public event Action<ConnectedPlayer> OnPlayerConnected = delegate { };

            public IEnumerable<ConnectedPlayer> Players
            {
                get { return m_connectedPlayersList; }
            }

            public int PlayerCount
            {
                get { return m_connectedPlayersList.Count; }
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
            
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                m_authentificationMessagePacker = new SimpleDataMessagePacker<PLAYER_AUTHENTIFICATION_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION);
                m_authentificationResponseMessagePacker = new SimpleDataMessagePacker<PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION_RESPONSE);
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION, OnAuthentificationMessageReceived);
                m_parentMessageDispatcher = dispatcher;
            }

            protected override void OnManagerInitialize()
            {
                m_connectedPlayersList = new HashSet<ConnectedPlayer>();
                m_channelIDToPlayerInfoDictionary = new Dictionary<int, ConnectedPlayer>();
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
                // TODO : Cleanup disconnected players from the list eventually ?
            }

            private void OnAuthentificationMessageReceived(MESSAGE msg)
            {
 

                var authentificationData = m_authentificationMessagePacker.UnpackMessage(msg);

                string name;
                unsafe
                {
                    name = new string(authentificationData.playerName);
                }

                // TODO : Load player info (such as the name) as a single PlayerProfileData structure from a backend
                ConnectedPlayer newPlayerInfo = new ConnectedPlayer(name);
                newPlayerInfo.AssignChannel(msg.ChannelID, m_parentMessageDispatcher);
                m_connectedPlayersList.Add(newPlayerInfo);
                m_channelIDToPlayerInfoDictionary.Add(msg.ChannelID, newPlayerInfo);

                var channel = ServerManager.Instance.GetCommunicationChannelFromID(msg.ChannelID);

                channel.OnChannelClosedOrFailed += (state) => { OnPlayerChannelClosedOrFailed(newPlayerInfo); };

                OnPlayerConnected(newPlayerInfo);

                Debug.Log("Player authentified, Name = " + newPlayerInfo.Name);

                PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA response = new PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA();
                response.accepted = true;

                var message = m_authentificationResponseMessagePacker.PackMessage(response);
                message.ChannelID = msg.ChannelID;
                StageMessageForSending(message);
            }

            private void OnPlayerChannelClosedOrFailed(ConnectedPlayer info)
            {
                // TODO : Consider not immediately disconnecting a player in case their connection simply temporarily drops.
                // Instead, there could be a short delay to give them a chance to reconnect and for example resume their ongoing match.

                RemovePlayer(info, false);
            }

            public void RemovePlayer(ConnectedPlayer info, bool closeChannel = true)
            {
                m_connectedPlayersList.Remove(info);
                if (info.ConnectionChannelAlive)
                {
                    m_channelIDToPlayerInfoDictionary.Remove(info.CommunicationChannelID);
                    // TODO : Find some clever way of making this manager agnostic of what owns it (the server) again.
                    if (closeChannel) ServerManager.Instance.GetCommunicationChannelFromID(info.CommunicationChannelID).CloseChannel();
                    info.UnassignCurrentChannel(m_parentMessageDispatcher);
                }

                info.SetDisconnected();
            }

            public ConnectedPlayer GetConnectedPlayerFromChannelID(int channelID)
            {
                if (m_channelIDToPlayerInfoDictionary.ContainsKey(channelID) == false)
                {
                    return null;
                }
                return m_channelIDToPlayerInfoDictionary[channelID];
            }
        }
    }
}

