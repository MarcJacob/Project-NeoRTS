using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using NeoRTS.GameData.ObjectData;
using NeoRTS.Server.Players;
using NeoRTS.Tools;
using System.Collections.Generic;
using static NeoRTS.GameData.Matches.Match;

namespace NeoRTS
{
    namespace Server
    {

        /// <summary>
        /// DEPENDENCY : <see cref="MatchesManager"/> and <see cref="ConnectedPlayersManager"/>.
        /// Keeps track of connected players and reacts to them expressing the wish to start matchmaking.
        /// When they do, this manager is responsible for pairing players together (assuming we're doing 1v1)
        /// and feeding the appropriate info into the <see cref="MatchesManager"/> in order to have a Match started
        /// Server-side.
        /// </summary>
        public class MatchmakingManager : ManagerObject
        {
            private const float MATCH_START_DELAY = 2f;

            private MatchesManager m_matchesManager;
            private ConnectedPlayersManager m_connectedPlayersManager;
            private int m_playersPerMatch;

            private HashSet<ConnectedPlayer> m_playersInMatchmaking;

            private SimpleDataMessagePacker<PLAYER_START_MATCHMAKING_MESSAGE_DATA> m_matchFoundMessagePacker;

            private struct MATCH_STARTING_DATA
            {
                public MATCH_STARTED_DATA matchStartData;
                public ConnectedPlayer[] players;
                public float timeBeforeStart;
            }
            private List<MATCH_STARTING_DATA> m_startingMatches;

            public MatchmakingManager(MatchesManager matchesManager, ConnectedPlayersManager connectedPlayersManager, int playersPerMatch)
            {
                m_matchesManager = matchesManager;
                m_connectedPlayersManager = connectedPlayersManager;
                m_playersPerMatch = playersPerMatch;
                m_playersInMatchmaking = new HashSet<ConnectedPlayer>();

                m_matchFoundMessagePacker = new SimpleDataMessagePacker<PLAYER_START_MATCHMAKING_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_MATCH_FOUND);
                m_startingMatches = new List<MATCH_STARTING_DATA>();
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                // TODO Add a "Start matchmaking" system here !
            }

            protected override void OnManagerInitialize()
            {
                m_connectedPlayersManager.OnPlayerConnected += OnPlayerConnected;
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
                // MATCHMAKING
                if (m_playersInMatchmaking.Count >= m_playersPerMatch)
                {
                    ConnectedPlayer[] playersInMatch = new ConnectedPlayer[m_playersPerMatch];
                    int playersInMatchCount = 0;
                    foreach(var player in m_playersInMatchmaking)
                    {
                        playersInMatch[playersInMatchCount] = player;
                        playersInMatchCount++;
                        if (playersInMatchCount == m_playersPerMatch) break;
                    }

                    // Send "match found" message to all involved players.
                    MATCH_STARTING_DATA startingData = new MATCH_STARTING_DATA();
                    startingData.matchStartData = ConstructMatchStartedData();
                    startingData.players = playersInMatch;
                    startingData.timeBeforeStart = MATCH_START_DELAY;

                    m_startingMatches.Add(startingData);

                    foreach(var player in playersInMatch)
                    {
                        MESSAGE msg = m_matchFoundMessagePacker.PackMessage(new PLAYER_START_MATCHMAKING_MESSAGE_DATA());
                        msg.ChannelID = player.CommunicationChannelID;
                        StageMessageForSending(msg);
                    }


                    foreach (var player in playersInMatch)
                        m_playersInMatchmaking.Remove(player);
                }

                // UPDATE STARTING MATCHES
                if (m_startingMatches.Count > 0)
                {
                    for (int i = 0; i < m_startingMatches.Count; i++)
                    {
                        MATCH_STARTING_DATA startingMatch = m_startingMatches[i];
                        startingMatch.timeBeforeStart -= deltaTime;
                        m_startingMatches[i] = startingMatch;

                        if (m_startingMatches[i].timeBeforeStart <= 0)
                        {
                            StartMatch(m_startingMatches[i]);
                            m_startingMatches.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            
            private void StartMatch(MATCH_STARTING_DATA startingData)
            {
                m_matchesManager.StartMatch(startingData.matchStartData, startingData.players);
            }

            private void OnPlayerConnected(ConnectedPlayer player)
            {
                player.PlayerMessagesDispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_START_MATCHMAKING,
                    (message) => { OnPlayerStartMatchmakingMessageReceived(player, message); });
                player.OnPlayerDisconnected += () =>
                {
                    if (m_playersInMatchmaking.Contains(player)) m_playersInMatchmaking.Remove(player);
                };
            }

            private void OnPlayerStartMatchmakingMessageReceived(ConnectedPlayer player, MESSAGE message)
            {
                if (m_playersInMatchmaking.Contains(player) == false)
                {
                    m_playersInMatchmaking.Add(player);
                }
            }

            // TODO : Separate the notion of the "match started" message's data and the "MATCH_STARTED" data structure we don't ONLY need for message sending.
            private MATCH_STARTED_DATA ConstructMatchStartedData()
            {
                var matchStartedData = new MATCH_STARTED_DATA();


                List<ObjectMemoryManager.OBJECT_SPAWN_DATA> unitsSpawnList = new List<ObjectMemoryManager.OBJECT_SPAWN_DATA>();

                for (int i = 0; i < m_playersPerMatch; i++)
                {
                    for (int a = 0; a < 1; a++)
                    {
                        var unitSpawnData = new ObjectMemoryManager.OBJECT_SPAWN_DATA();
                        unitSpawnData.owner = i;
                        unitSpawnData.startPosition = new Position(Random.Range(0f, 40f), 0f, Random.Range(0f, 40f));
                        unitSpawnData.objectTypeID = ObjectDataTypeDatabase.ResolveNameToObjectTypeID("Harvester Hut");
                        unitsSpawnList.Add(unitSpawnData);
                    }
                }

                /*for(int i = 0; i < 100; i++)
                {
                    var unitSpawnData = new ObjectMemoryManager.OBJECT_SPAWN_DATA();
                    unitSpawnData.owner = -1;
                    unitSpawnData.objectTypeID = 1;
                    unitSpawnData.startPosition = new Position(Random.Range(0f, 90f), 0f, Random.Range(0f, 90f));
                    unitsSpawnList.Add(unitSpawnData);
                }
                */

                matchStartedData.startUnits = unitsSpawnList.ToArray();
                return matchStartedData;
            }
        }
    }
}

