using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using NeoRTS.GameData.Matches;
using NeoRTS.Server.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using static NeoRTS.GameData.Matches.Match;

namespace NeoRTS
{
    namespace Server
    {

        /// <summary>
        /// Manages a collection of Match objects. Able to Update all currently running matches given a Deltatime.
        /// Able to Start / End a match on demand with the proper data.
        /// </summary>
        public class MatchesManager : ManagerObject
        {
            public struct ServerSideMatchData
            {
                public Match match;
                public ConnectedPlayer[] playersInMatch;
            }

            private HashSet<ServerSideMatchData> m_matchesData;
            private int m_maxMessageCountPerMessagePoll;

            // Message Packers
            private MatchStartedDataMessagePacker m_matchStartedMessagePacker;
            private SimpleDataMessagePacker<EMPTY_MESSAGE_DATA> m_matchEndedMessagePacker;

            public int MatchCount { get { return m_matchesData.Count; } }
            public MatchesManager(int maxMessageCountPerMessagePoll)
            {
                m_maxMessageCountPerMessagePoll = maxMessageCountPerMessagePoll;
            }

            protected override void OnManagerInitialize()
            {
                m_matchesData = new HashSet<ServerSideMatchData>();
                m_matchStartedMessagePacker = new MatchStartedDataMessagePacker();
                m_matchEndedMessagePacker = new SimpleDataMessagePacker<EMPTY_MESSAGE_DATA>(MESSAGE_TYPE.MATCH_ENDED);
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandlerOnTypeRange(MESSAGE_TYPE_RANGE.MATCH_MESSAGES, OnMessageReceived);
            }

            private void OnMessageReceived(MESSAGE message)
            {
                var channelID = message.ChannelID;
                foreach(var match in m_matchesData)
                {
                    if (match.playersInMatch.Any(p => p.CommunicationChannelID == channelID))
                    {
                        match.match.DispatchMessage(message);
                    }
                }
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
                // TODO : This MIGHT be where some multithreading would be required.
                // Although all multithreading COULD be done on the Worker-level aswell considering they run most of the logic.
                HashSet<ServerSideMatchData> endedMatches = new HashSet<ServerSideMatchData>();
                foreach(var matchData in m_matchesData)
                {
                    matchData.match.UpdateMatch(deltaTime);
                    if (matchData.match.Ended)
                    {
                        endedMatches.Add(matchData);
                    }
                }

                foreach(var match in endedMatches)
                {
                    m_matchesData.Remove(match);
                    OnMatchEnded(match);
                }

                // Send messages "upward" read from Matches. Make sure one match doesn't use up all the resources.
                {
                    bool messagesLeft = true;
                    while (StagedMessagesCount < m_maxMessageCountPerMessagePoll && messagesLeft)
                    {
                        messagesLeft = false;
                        foreach (var matchData in m_matchesData)
                        {
                            if (matchData.match.MessagesQueue.Count > 0)
                            {
                                var nextMessage = matchData.match.MessagesQueue.Dequeue();

                                nextMessage.channelIDs.Clear();

                                foreach (var playerInfo in matchData.playersInMatch)
                                {
                                    nextMessage.channelIDs.Add(playerInfo.CommunicationChannelID);
                                }

                                if (matchData.match.MessagesQueue.Count == 0)
                                {
                                    StageMessageForSending(nextMessage);
                                }
                                else if (matchData.match.MessagesQueue.Count > 0)
                                {
                                    StageMessageForSending(nextMessage);
                                    messagesLeft = true;
                                }
                            }

                        }
                    }

                }

                if (StagedMessagesCount == m_maxMessageCountPerMessagePoll)
                {
                    Console.WriteLine("WARNING - Matches manager is overloaded with messages ! Try reducing the amount of matches this server runs or optimize the amount of messages each match sends.");
                }
            }
            public void StartMatch(MATCH_STARTED_DATA matchStartedData, params ConnectedPlayer[] players)
            {
                var match = new Match(true);
                var serverMatchData = new ServerSideMatchData();

                serverMatchData.match = match;
                serverMatchData.playersInMatch = players;

                foreach(var player in players)
                {
                        player.OnPlayerDisconnected += match.ForceEnd;
                }

                m_matchesData.Add(serverMatchData);

                // Send MATCH STARTED message to all players
                {
                    // Send "Match Started" message to all players with all the needed info.
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].ConnectionChannelAlive)
                        {
                            int playerChannelID = players[i].CommunicationChannelID;

                            MATCH_STARTED_MESSAGE_DATA matchStartedMessageData = new MATCH_STARTED_MESSAGE_DATA();
                            matchStartedMessageData.matchStartData = matchStartedData;

                            matchStartedMessageData.localPlayerID = i;

                            MESSAGE msg;
                            msg = m_matchStartedMessagePacker.PackMessage(matchStartedMessageData, 10240);

                            msg.ChannelID = playerChannelID;

                            StageMessageForSending(msg);
                        }
                    }
                }

                match.OnMatchStart(matchStartedData);
            }

            private void OnMatchEnded(ServerSideMatchData matchData)
            {
                // TODO : Generate a "End of match" report that gets sent to the master server.
                // TODO : Kick players from Match Server. Right now just send a "MATCH_ENDED" message.

                EMPTY_MESSAGE_DATA matchEndedData = new EMPTY_MESSAGE_DATA();
                MESSAGE msg = m_matchEndedMessagePacker.PackMessage(matchEndedData);
                msg.channelIDs.Clear();

                foreach(var player in matchData.playersInMatch)
                {
                    if (player.ConnectionChannelAlive)
                    {
                        msg.channelIDs.Add(player.CommunicationChannelID);
                    }
                }

                StageMessageForSending(msg);
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {

            }
        }
    }
}

