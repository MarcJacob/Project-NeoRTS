using NeoRTS.Communication;
using NeoRTS.GameData;
using NeoRTS.GameData.Matches;
using NeoRTS.Communication.Messages;
using UnityEngine.SceneManagement;
using UnityEngine;
using static NeoRTS.GameData.Matches.Match;
using System.Collections.Generic;
using NeoRTS.GameData.ObjectData;

namespace NeoRTS
{
    namespace Client
    {
        namespace LocalMatch
        {
            /// <summary>
            /// Dedicated management object for the unique Match Object the Client runs locally, either as a "mimic"
            /// of the Server's or as its local "Gameplay server" when playing single player.
            /// 
            /// When playing locally, works with a specific reception channel and a specific (different !) emission channel,
            /// as to avoid creating message reception -> emission loops with the Match object.
            /// </summary>
            public class LocalMatchManager : ManagerObject
            {
                private Match m_localMatch;
                public Match LocalMatch { get { return m_localMatch; } }

                private bool m_sendMatchMessages = false;
                private int m_messageSendingChannelID;
                private MessageDispatcher m_dispatcher;
                private ReceivedMessageChannelIDFilter m_dispatcherChannelIDFilter;
                private MatchStartedDataMessagePacker m_matchStartedMessagePacker;
                private int m_localPlayerID;

                public int LocalPlayerID
                {
                    get { return m_localPlayerID; }
                }

                protected override void OnManagerInitialize()
                {
                    m_dispatcherChannelIDFilter = new ReceivedMessageChannelIDFilter(-1);
                    m_dispatcher = new MessageDispatcher(m_dispatcherChannelIDFilter);
                    m_dispatcher.RegisterOnMessageReceivedHandler(DispatchFilteredMessage);
                    m_matchStartedMessagePacker = new MatchStartedDataMessagePacker ();
                }

                public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
                {
                    dispatcher.RegisterOnMessageReceivedHandler(OnAnyMessageReceived);
                    
                }
                public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
                {
                    dispatcher.UnregisterOnMessageReceivedHandler(OnAnyMessageReceived);
                }

                protected override void OnManagerUpdate(float deltaTime)
                {
                    if (m_localMatch != null)
                    {
                        m_localMatch.UpdateMatch(deltaTime);

                        while (m_localMatch.MessagesQueue.Count > 0)
                        {
                            if (m_sendMatchMessages == false) m_localMatch.MessagesQueue.Clear();
                            else
                            {
                                var msg = m_localMatch.MessagesQueue.Dequeue();
                                msg.ChannelID = m_messageSendingChannelID;
                                StageMessageForSending(msg);
                            }
                        }
                    }
                }

                public void SetupLocalMatchForOnlineGameplay()
                {
                    m_dispatcherChannelIDFilter.requiredChannelID = (int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER;
                    m_messageSendingChannelID = (int)GameClient.CLIENT_CHANNEL_ID.MASTER_SERVER;
                    m_sendMatchMessages = false;
                    m_localMatch = new Match(false);
                }

                public void SetupLocalMatchForLocalGameplay()
                {
                    m_messageSendingChannelID = (int)GameClient.CLIENT_CHANNEL_ID.LOCALMATCH_SEND;
                    m_dispatcherChannelIDFilter.requiredChannelID = (int)GameClient.CLIENT_CHANNEL_ID.SELF;
                    m_sendMatchMessages = true;
                    m_localMatch = new Match(true);
                }

                private void OnAnyMessageReceived(MESSAGE message)
                {
                    m_dispatcher.DispatchMessage(message);
                }

                private void DispatchFilteredMessage(MESSAGE message)
                {
                    if (m_localMatch != null)
                    {
                        m_localMatch.DispatchMessage(message);
                    }
                }

                // This triggers when the server sends us a "match started" message. Only relevant for Online play.
                public void StartLocalMatch(MATCH_STARTED_MESSAGE_DATA matchStartedData)
                {
                    m_localPlayerID = matchStartedData.localPlayerID;
                    m_localMatch.OnMatchStart(matchStartedData.matchStartData);
                }


                // TODO : Refactoring work.
                // Match started data should NOT be generated in here under any circumstance.
                // It should be generated by whatever triggers the match to start, be it the PlayingState
                // itself when playing solo (later a proper CreateSingleplayerMatch state) or the server.
                // 
                // When the data is generated, send a MATCH_LOADING message. This manager reacts to it by
                // priming itself to start the local match with the received data either in SP or MP mode.
                // A Loading state handles the actual data loading necessary and telling the LocalMatchManager
                // to actually start "ticking" the match forward.
                public void GenerateLocalMatchStartedMessage()
                {
                    var matchStartedData = new MATCH_STARTED_MESSAGE_DATA();

                    List<ObjectMemoryManager.OBJECT_SPAWN_DATA> units = new List<ObjectMemoryManager.OBJECT_SPAWN_DATA>();

                    units.Add(new ObjectMemoryManager.OBJECT_SPAWN_DATA()
                    {
                        objectTypeID = ObjectDataTypeDatabase.ResolveNameToObjectTypeID("Harvester Hut"),
                        owner = 0,
                        startPosition = new Position(5f, 0f, 5f)
                    });

                    for (int i = 0; i < 10; i++)
                    {
                        units.Add(new ObjectMemoryManager.OBJECT_SPAWN_DATA()
                        {
                            objectTypeID = 0,
                            owner = 1,
                            startPosition = new Position(Tools.Random.Range(30f, 50f), 0f, Tools.Random.Range(30f, 50f))
                        });
                    }

                    matchStartedData.matchStartData.startUnits = units.ToArray();

                    // TODO : Find some way of filling matchStartedData with appropriate data such as the map, the local player's player ID...

                    MESSAGE matchStartedMessage;
                    matchStartedData.localPlayerID = 0;
                    matchStartedData.localMatch = true;

                    matchStartedMessage = m_matchStartedMessagePacker.PackMessage(matchStartedData);
                    matchStartedMessage.ChannelID = (int)GameClient.CLIENT_CHANNEL_ID.SELF;

                    StageMessageForSending(matchStartedMessage);
                }

            }
        }
    }
}


