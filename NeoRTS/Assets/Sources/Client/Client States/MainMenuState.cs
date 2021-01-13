using NeoRTS.Client.UI;
using NeoRTS.Communication.Messages;
using NeoRTS.Communication.Networking;
using NeoRTS.GameData;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// Active while the player is in the Main Menu.
        /// As of now, is the starting state.
        /// 
        /// TODO : Have a "log in screen" state BEFORE the main menu ? Or only after clicking "Multiplayer" ?
        /// </summary>
        public class MainMenuState : ClientState
        {
            // TODO : Make these more easily configurable than changing the code.
            public const int MASTER_SERVER_PORT = 27015;
            public const string MASTER_SERVER_ADDRESS = "127.0.0.1";

            public override ManagersContainer StateManagers { get; protected set; }

            private MainMenuUIModule m_mainMenuButtonsModule;

            private SimpleDataMessagePacker<PLAYER_AUTHENTIFICATION_MESSAGE_DATA> m_playerAuthentificationMessagePacker;
            private SimpleDataMessagePacker<PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA> m_playerAuthentificationResponseMessagePacker;

            public MainMenuState()
            {
                StateManagers = new ManagersContainer();
                m_playerAuthentificationMessagePacker = new SimpleDataMessagePacker<PLAYER_AUTHENTIFICATION_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION);
                m_playerAuthentificationResponseMessagePacker = new SimpleDataMessagePacker<PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION_RESPONSE);
            }

            protected override void OnStart()
            {
                if (SceneManager.GetActiveScene().buildIndex != 0)
                {
                    SceneManager.LoadScene(0);
                }

                m_mainMenuButtonsModule = GameClient.Instance.GetManager<UIManager>().GetUIModule<MainMenuUIModule>();
                m_mainMenuButtonsModule.OnSingleplayerButtonClick += StartSingleplayerMatch;
                m_mainMenuButtonsModule.OnMultiplayerButtonClick += ConnectToMasterServer;
            }

            protected override void OnStop()
            {
                GameObject.Destroy(m_mainMenuButtonsModule.gameObject);
            }

            protected override void OnUpdate(float deltaTime)
            {

            }

            public override void MessageReceptionSetup(Communication.MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_AUTHENTIFICATION_RESPONSE, OnAuthentificationResponseReceived);
            }

            public override void MessageReceptionCleanup(Communication.MessageDispatcher dispatcher)
            {
                dispatcher.UnregisterOnMessageReceivedHandler(OnAuthentificationResponseReceived);
            }

            public void StartSingleplayerMatch()
            {
                // TODO : Switch to "SP match creation" state ?

                // Start PlayingState with default constructor which creates a singleplayer game.
                GameClient.Instance.ChangeState<PlayingState>();
            }

            public void ConnectToMasterServer(MainMenuUIModule.LoginButtonClickEventData eventData)
            {
                // Check validity of request

                if (eventData.username.Length <= 0 || eventData.username.Length > 32)
                {
                    GameClient.Instance.GetManager<UIManager>().ShowErrorPopup("Please enter a Username between 1 and 32 characters long");
                    return;
                }

                string addressText = eventData.address;
                string portText = eventData.port;
                if (addressText.Length == 0) addressText = MASTER_SERVER_ADDRESS;
                int port;
                if (portText.Length == 0) port = MASTER_SERVER_PORT;
                else port = int.Parse(portText);
                try
                {
                    var channel = GameClient.Instance.ConnectToServerWithChannel((int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER, addressText, port);
                    
                    if (channel != null)
                    {
                        // TODO : Add intermediate state during which authentification to the server is "negotiated".

                        // TODO : Generate the authentification data somewhere else.

                        PLAYER_AUTHENTIFICATION_MESSAGE_DATA authentificationData = new PLAYER_AUTHENTIFICATION_MESSAGE_DATA();

                        // AUTHENTIFICATION MESSAGE BUILDING
                        unsafe {
                            char[] characters = eventData.username.ToCharArray();
                            fixed(char* charPtr = characters)
                            {
                                Buffer.MemoryCopy(charPtr, authentificationData.playerName, PLAYER_AUTHENTIFICATION_MESSAGE_DATA.PLAYER_NAME_MAX_CHAR_LENGTH * 4, characters.Length * 4);
                            }
                            
                        }


                        MESSAGE authentificationMessage = m_playerAuthentificationMessagePacker.PackMessage(authentificationData);
                        authentificationMessage.ChannelID = (int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER;
                        
                        GameClient.Instance.SendMessage(authentificationMessage);

                        PlayerPrefs.SetString("MASTER_SERVER_ADDRESS", addressText);
                        PlayerPrefs.SetString("MASTER_SERVER_PORT", portText);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("ERROR CONNECTING TO MASTER SERVER : " + e.Message);
                    GameClient.Instance.GetManager<UIManager>().ShowErrorPopup("ERROR CONNECTING TO MASTER SERVER : " + e.Message);
                    // TODO : While connecting (IE before starting the actual connection), load a specific piece of UI and disable the other buttons.
                    // If connection fails, simply log the error message somehow (popup on screen perhaps ?) and just return to the initial state of the MainMenuState.
                }
            }

            private void OnAuthentificationResponseReceived(MESSAGE message)
            {
                Debug.Log("Authentifcation accepted.");
                GameClient.Instance.ChangeState(new MultiplayerMenuState(GameClient.Instance.GetCommunicationChannelFromID(GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER)));
            }
        }
    }
}


