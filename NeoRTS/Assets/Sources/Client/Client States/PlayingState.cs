using NeoRTS.Client.LocalMatch;
using NeoRTS.Client.Pawns;
using NeoRTS.Client.UI;
using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NeoRTS.GameData.Matches.Match;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// Active while the player is currently playing in a match. As of now, becomes active upon "wanting" to start a match,
        /// meaning the second the player clicks the Singleplayer or Multiplayer button : thus it handles loading the correct
        /// scene among other things we might (TODO) want to move to an intermediate state.
        /// 
        /// // TODO : Split into multiplayer playing state and singleplayer playing state.
        /// </summary>
        public class PlayingState : ClientState
        {
            public override ManagersContainer StateManagers { get; protected set; }

            private bool m_playingSingleplayer;

            private MatchStartedDataMessagePacker m_matchStartedMessageUnpacker;

            private ObjectPawnUIRootUIModule m_objectPawnUIRoot;
            private ClientState m_postGameClientState;

            public PlayingState() : this(null)
            {

            }

            /// <summary>
            /// Initializes the Playing State in "Singleplayer play" mode.
            /// </summary>
            public PlayingState(ClientState postgameClientState)
            {
                m_playingSingleplayer = true;
                m_postGameClientState = postgameClientState;
                LocalMatchManager localMatchManager;
                RTSModeUnitControlManager rtsModeManager;
                ObjectPawnsManager objectPawnsManager;
                {
                    localMatchManager = new LocalMatchManager();
                    rtsModeManager = new RTSModeUnitControlManager();
                    objectPawnsManager = new ObjectPawnsManager();
                    StateManagers = new ManagersContainer(
                        localMatchManager,
                        rtsModeManager,
                        objectPawnsManager);
                }

                m_matchStartedMessageUnpacker = new MatchStartedDataMessagePacker();

            }

            /// <summary>
            /// Creates the PlayingState in "Online play" mode. Requires passing in the channel leading to the match
            /// server. The PlayingState will leave and send the client back to the MainMenuState if connection is lost.
            /// </summary>
            public PlayingState(CommunicationChannel matchServerCommunicationChannel, ClientState postgameClientState = null) : this()
            {
                m_playingSingleplayer = false;
                m_postGameClientState = postgameClientState;
                matchServerCommunicationChannel.OnChannelClosedOrFailed += OnConnectionToMatchServerLost;
            }

            public override void MessageReceptionSetup(MessageDispatcher dispatcher)
            {
                base.MessageReceptionSetup(dispatcher);
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.MATCH_STARTED, OnMatchStartedMessageReceived);
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.MATCH_ENDED, OnMatchEndedMessageReceived);
            }

            public override void MessageReceptionCleanup(MessageDispatcher dispatcher)
            {
                base.MessageReceptionCleanup(dispatcher);
                dispatcher.UnregisterOnMessageReceivedHandler(OnMatchStartedMessageReceived);
                dispatcher.UnregisterOnMessageReceivedHandler(OnMatchEndedMessageReceived);
            }

           

            protected override void OnStart()
            {
                // TODO : For now we immediately load the scene and THEN wait for the match to begin.
                // We should be in an intermediate state or at least show a special UI when loading in.
                GameClient.Instance.StartCoroutine(InitializeGameplaySceneCoroutine());

            }

            private IEnumerator InitializeGameplaySceneCoroutine()
            {
                SceneManager.LoadScene(1);
                yield return null;
                m_objectPawnUIRoot = GameClient.Instance.GetManager<UIManager>().GetUIModule<ObjectPawnUIRootUIModule>();
                m_objectPawnUIRoot.SetCamera(Camera.main);


                LocalMatchManager localMatchManager = StateManagers.Get<LocalMatchManager>();
                if (m_playingSingleplayer)
                {
                    localMatchManager.GenerateLocalMatchStartedMessage();
                }
            }

            protected override void OnStop()
            {
                GameObject.Destroy(m_objectPawnUIRoot.gameObject);
            }

            protected override void OnUpdate(float deltaTime)
            {
                
            }

            private void OnConnectionToMatchServerLost(CHANNEL_STATE channelState)
            {
                GameClient.Instance.GetManager<UIManager>().ShowErrorPopup("Connection to Match Server lost. Returning to Main Menu.",
                    SwitchToPostGameState);
            }

            private void SwitchToPostGameState()
            {
                if (m_postGameClientState == null)
                {
                    m_postGameClientState = new MainMenuState();
                }

                GameClient.Instance.ChangeState(m_postGameClientState);
            }

            private void OnMatchStartedMessageReceived(MESSAGE message)
            {
                var matchStartedMessageData = m_matchStartedMessageUnpacker.UnpackMessage(message);
                LocalMatchManager localMatchManager = StateManagers.Get<LocalMatchManager>();
                if (m_playingSingleplayer)
                {
                    localMatchManager.SetupLocalMatchForLocalGameplay();
                }
                else
                {
                    localMatchManager.SetupLocalMatchForOnlineGameplay();
                }

                ObjectPawnsManager objectPawnsManager = StateManagers.Get<ObjectPawnsManager>();
                objectPawnsManager.LinkToMatchData(
                    localMatchManager.LocalMatch.Managers.Get<ObjectMemoryManager>()
                );
                objectPawnsManager.LocalPlayerID = matchStartedMessageData.localPlayerID;

                localMatchManager.StartLocalMatch(matchStartedMessageData);
            }

            private void OnMatchEndedMessageReceived(MESSAGE message)
            {
                SwitchToPostGameState();
            }

            public override MESSAGE[] GetPendingMessages()
            {
                var messages = base.GetPendingMessages();

                if (m_playingSingleplayer == false)
                    for (int i = 0; i < messages.Length; i++)
                    {
                        messages[i].ChannelID = (int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER;
                    }
                return messages;
            }
        }
    }
}


