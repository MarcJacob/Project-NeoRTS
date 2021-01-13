using NeoRTS.Communication;
using NeoRTS.GameData;
using System.Collections.Generic;
using UnityEngine;
using NeoRTS.Communication.Messages;
using UnityEngine.SceneManagement;
using NeoRTS.Client.LocalMatch;
using NeoRTS.Communication.Networking;
using NeoRTS.GameData.ObjectData;
using System;

namespace NeoRTS
{
    namespace Client
    {

        /// <summary>
        /// GameClient is the "nerve center" of the client application. It receives and handles messages through
        /// a list of CommunicationChannel objects. It dispatches messages appropriatly to
        /// other game systems with a MessageDispatcher.
        /// </summary>
        public class GameClient : MonoBehaviour
        {
            // Channel ID Constants
            public enum CLIENT_CHANNEL_ID
            {
                SELF = 0,
                MASTER_SERVER = 1,
                LOCALMATCH_SEND = 2 // When playing SP, the local Match object will send its data through here and not receive from here.
            }

            public static GameClient Instance { get; private set; }

            private ChannelCollection m_communicationChannels;
            private MessageDispatcher m_messageDispatcher;
            private ManagersContainer m_managers;

            private ClientState m_currentState;

            private void Awake()
            {
                if (Instance != null) 
                { 
                    Destroy(this);
                    Debug.LogError("ERROR - More than one ClientManager were present in the scene ! Destroying current instance...");
                    return; 
                }

                m_communicationChannels = new ChannelCollection();
                m_messageDispatcher = new MessageDispatcher();

                InputManager m_inputManager;
                NetworkingManager networkingManager;
                UIManager uiManager;
                {
                    m_inputManager = new InputManager();
                    networkingManager = new NetworkingManager();
                    uiManager = new UIManager();
                    m_managers = new ManagersContainer(
                        m_inputManager,
                        networkingManager,
                        uiManager);
                }

                DontDestroyOnLoad(gameObject);
                Instance = this;

                // Tools initialization
                // TODO : Move that to a specific class ? We might be building a lot of different tools in the future

                NeoRTS.Tools.Random.Initialize(0);
            }

            private void Start()
            {

                m_managers.InitializeManagers();
                m_managers.InitializeManagersMessageReception(m_messageDispatcher);


                // Initialize local communication channels.
                // Server channel(s) get initialized whenever connection is attempted.
                {
                    LocalAppCommunicationChannel selfCommChannel = new LocalAppCommunicationChannel();
                    LocalAppCommunicationChannel matchSendCommChannel = new LocalAppCommunicationChannel();
                    LocalAppCommunicationChannel.LinkCommunicationChannels(selfCommChannel, selfCommChannel);
                    LocalAppCommunicationChannel.LinkCommunicationChannels(matchSendCommChannel, matchSendCommChannel);

                    selfCommChannel.AssignMessageDispatcher(m_messageDispatcher);
                    matchSendCommChannel.AssignMessageDispatcher(m_messageDispatcher);

                    m_communicationChannels.AddChannel((int)CLIENT_CHANNEL_ID.SELF, selfCommChannel);
                    m_communicationChannels.AddChannel((int)CLIENT_CHANNEL_ID.LOCALMATCH_SEND, matchSendCommChannel);
                }

                // Static initializations
                {
                    ObjectDataTypeDatabase.BuildDatabase();
                }

                // State initialization. Start out in the Main Menu state.

                ChangeState<MainMenuState>();
            }

            private void Update()
            {
                m_managers.UpdateManagers(Time.deltaTime);
                
                if (m_currentState != null)
                    m_currentState.Update(Time.deltaTime);

                // Send GameClient managers messages
                var messages = m_managers.RetrieveAllManagersMessages();
                foreach (var message in messages)
                    SendMessage(message);

                // Send Client State managers messages
                if (m_currentState != null)
                {
                    messages = m_currentState.GetPendingMessages();
                    foreach (var message in messages)
                        SendMessage(message);
                }

                m_communicationChannels.RemoveClosedAndFailedChannels();

                string msg;
                while (Tools.Debug.PollMessage(out msg))
                {
                    Debug.Log(msg);
                }

                Tools.Debug.DEBUG_DRAW_REQUEST drawRequest;
                while(Tools.Debug.PollDrawRequest(out drawRequest))
                {
                    switch(drawRequest.drawType)
                    {
                        case (Tools.Debug.DEBUG_DRAW_REQUEST.DRAW_TYPE.LINE):
                            Debug.DrawLine(drawRequest.positions[0], drawRequest.positions[1], drawRequest.color);
                            break;
                    }
                }
            }

            #region STATE MANAGEMENT

            public void ChangeState<T>() where T : ClientState, new()
            {
                ChangeState(new T());
            }

            public void ChangeState(ClientState state)
            {
                if (m_currentState != null)
                {
                    m_currentState.MessageReceptionCleanup(m_messageDispatcher);
                    m_currentState.Stop();
                }
                m_currentState = state;
                if (m_currentState != null)
                {
                    m_currentState.MessageReceptionSetup(m_messageDispatcher);
                    m_currentState.Start();
                }
            }

            #endregion

            #region MESSAGING

            public void SendMessage(MESSAGE message)
            {
                if (message.channelIDs.Count > 1) throw new System.Exception("ERROR : Client should (probably ?) not send messages accross multiple channels.");
                
                m_communicationChannels[message.ChannelID].SendMessage(message);
            }

            public CommunicationChannel GetCommunicationChannelFromID(CLIENT_CHANNEL_ID ID)
            {
                return m_communicationChannels[(int)ID];
            }
            #endregion

            #region MANAGERS ACCESS

            /// <summary>
            /// Returns a ManagerObject of the given type. First looks through the GameClient itself, and then through
            /// whatever State object that is currently driving the GameClient.
            /// </summary>
            public T GetManager<T>() where T : ManagerObject
            {
                // Look inside the GameClient wide collection first, then the current State's.
                var manager = m_managers.Get<T>();
                if (manager == null && m_currentState != null)
                {
                    manager = m_currentState.StateManagers.Get<T>();
                }

                return manager;
            }
            #endregion

            #region NETWORKING
            public NetworkCommunicationChannel ConnectToServerWithChannel(int channelID, string address, int port)
            {
                // Issues :
                // - If we can't connect to the server for whatever reason, this is going to break. We don't properly handle connection failure.
                var channel = m_managers.Get<NetworkingManager>().CreateNetworkChannelObject(address, port);
                channel.AssignMessageDispatcher(m_messageDispatcher);
                m_communicationChannels.AddChannel(channelID, channel);

                if (channel.State == CHANNEL_STATE.READY)
                {
                    return channel;
                }
                else return null;
            }
            #endregion
        }
    }
}


