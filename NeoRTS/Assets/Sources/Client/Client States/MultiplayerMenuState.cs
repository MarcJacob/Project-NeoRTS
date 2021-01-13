using NeoRTS.Client.UI;
using NeoRTS.GameData;
using UnityEngine;
using NeoRTS.Communication.Messages;
using NeoRTS.Communication;
using System;
using NeoRTS.Communication.Messages.Chat;
using NeoRTS.Tools;
using UnityEngine.SceneManagement;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// Active state once the client has connected to the Master Server but isn't in a game (basically similar
        /// to Main Menu but after we've logged in)
        /// </summary>
        public class MultiplayerMenuState : ClientState
        {
            private ConnectedToMasterServerMenuUIModule m_menuUIModule;
            private ChatBoxUIModule m_generalChatChatBox;

            public override ManagersContainer StateManagers { get; protected set; }

            private SimpleDataMessagePacker<PLAYER_START_MATCHMAKING_MESSAGE_DATA> m_startMatchmakingMessagePacker;
            
            private SimpleDataMessagePacker<EMPTY_MESSAGE_DATA> m_joinChannelMessagePacker;
            private SimpleDataMessagePacker<EMPTY_MESSAGE_DATA> m_leaveChannelMessagePacker;
            private SimpleDataMessagePacker<PLAYER_SENT_MESSAGE_MESSAGE_DATA> m_chatMessagesMessagePacker;

            private CommunicationChannel m_masterServerCommunicationChannel;

            public MultiplayerMenuState(CommunicationChannel masterServerCommunicationChannel)
            {
                StateManagers = new ManagersContainer();
                m_masterServerCommunicationChannel = masterServerCommunicationChannel;
                
            }

            private void OnMasterServerLostConnection(CHANNEL_STATE state)
            {
                GameClient.Instance.ChangeState<MainMenuState>();
                GameClient.Instance.GetManager<UIManager>().ShowErrorPopup("Connection to Match Server lost. Returning to Main Menu.");
            }

            public override void MessageReceptionSetup(MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_MATCH_FOUND, OnMatchFound);
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_SENT_MESSAGE, OnChatMessageMessageReceived);
                m_masterServerCommunicationChannel.OnChannelClosedOrFailed += OnMasterServerLostConnection;
            }

            public override void MessageReceptionCleanup(MessageDispatcher dispatcher)
            {
                dispatcher.UnregisterOnMessageReceivedHandler(OnMatchFound);
                dispatcher.UnregisterOnMessageReceivedHandler(OnChatMessageMessageReceived);
                m_masterServerCommunicationChannel.OnChannelClosedOrFailed -= OnMasterServerLostConnection;

            }

            public void StartSingleplayerMatch()
            {
                // TODO : Switch to "SP match creation" state ?



                // Start PlayingState with default constructor which creates a singleplayer game.
                GameClient.Instance.ChangeState(new PlayingState(this));
            }
            private void StartMatchmaking()
            {
                m_menuUIModule.SwitchToMatchmakingState();

                var matchmakingData = new PLAYER_START_MATCHMAKING_MESSAGE_DATA();

                MESSAGE msg = m_startMatchmakingMessagePacker.PackMessage(matchmakingData);
                msg.ChannelID = (int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER;
                GameClient.Instance.SendMessage(msg);
            }
            private void OnMatchFound(MESSAGE msg)
            {
                GameClient.Instance.ChangeState(new PlayingState(m_masterServerCommunicationChannel, this));
            }

            private void OnChatMessageMessageReceived(MESSAGE msg)
            {
                var messageData = m_chatMessagesMessagePacker.UnpackMessage(msg);

                unsafe
                {
                    string senderStr = StringEncoding.Decode(messageData.sender, messageData.senderByteCount);
                    string msgStr = StringEncoding.Decode(messageData.message, messageData.messageByteCount);

                    m_generalChatChatBox.AddChatLine(senderStr, msgStr);
                }

            }

            private unsafe void OnChatboxSubmit(string msg)
            {
                if (msg.Length > 0)
                {
                    PLAYER_SENT_MESSAGE_MESSAGE_DATA messageData = new PLAYER_SENT_MESSAGE_MESSAGE_DATA("", msg);

                    var message = m_chatMessagesMessagePacker.PackMessage(messageData);
                    message.ChannelID = m_masterServerCommunicationChannel.ChannelID;
                    GameClient.Instance.SendMessage(message);
                }
            }

            protected override void OnStart()
            {
                if (SceneManager.GetActiveScene().buildIndex != 0)
                {
                    SceneManager.LoadScene(0);
                }
                m_menuUIModule = GameClient.Instance.GetManager<UIManager>().GetUIModule<ConnectedToMasterServerMenuUIModule>();
                m_generalChatChatBox = GameClient.Instance.GetManager<UIManager>().GetUIModule<ChatBoxUIModule>();

                m_menuUIModule.OnSingleplayerButtonClick += StartSingleplayerMatch;
                m_menuUIModule.OnMatchmakingButtonClick += StartMatchmaking;

                m_generalChatChatBox.OnMessageSubmission += OnChatboxSubmit;

                m_startMatchmakingMessagePacker = new SimpleDataMessagePacker<PLAYER_START_MATCHMAKING_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_START_MATCHMAKING);
                m_joinChannelMessagePacker = new SimpleDataMessagePacker<EMPTY_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_REQUEST_JOIN_CHANNEL);
                m_leaveChannelMessagePacker = new SimpleDataMessagePacker<EMPTY_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_REQUEST_LEAVE_CHANNEL);
                m_chatMessagesMessagePacker = new SimpleDataMessagePacker<PLAYER_SENT_MESSAGE_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_SENT_MESSAGE);

                if (m_masterServerCommunicationChannel.State == CHANNEL_STATE.READY)
                {
                    MESSAGE joinChannelMsg = m_joinChannelMessagePacker.PackMessage(new EMPTY_MESSAGE_DATA());
                    joinChannelMsg.ChannelID = m_masterServerCommunicationChannel.ChannelID;
                    GameClient.Instance.SendMessage(joinChannelMsg);
                }
                else
                {
                    GameClient.Instance.GetManager<UIManager>().ShowErrorPopup("Connection to Master Server lost.");
                    GameClient.Instance.ChangeState<MainMenuState>();
                }
            }

            protected override void OnStop()
            {
                if (m_masterServerCommunicationChannel.State == CHANNEL_STATE.READY)
                {
                    MESSAGE leaveChannelMsg = m_leaveChannelMessagePacker.PackMessage(new EMPTY_MESSAGE_DATA());
                    leaveChannelMsg.ChannelID = m_masterServerCommunicationChannel.ChannelID;
                    GameClient.Instance.SendMessage(leaveChannelMsg);
                }

                GameObject.Destroy(m_menuUIModule.gameObject);
                GameObject.Destroy(m_generalChatChatBox.gameObject);
            }

            protected override void OnUpdate(float deltaTime)
            {
            }
        }
    }
}


