using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using NeoRTS.Server.Players;
using System.Collections.Generic;
using NeoRTS.Communication.Messages.Chat;
using System;
using NeoRTS.Tools;

namespace NeoRTS
{
    namespace Server
    {
        public class ChatChannelsManager : ManagerObject
        {
            private struct PlayerChatConnection
            {
                public ConnectedPlayer player;
                // TODO : What channel are we connected to ?
                public Action<MESSAGE> onPlayerSentMessageRegisteredCallback;
                public Action<MESSAGE> onPlayerLeaveChannelRegisteredCallback;

                public void OpenChatConnection(Action<MESSAGE> onPlayerSentMessageCallback, Action<MESSAGE> onPlayerLeaveCallback)
                {
                    onPlayerSentMessageRegisteredCallback = onPlayerSentMessageCallback;
                    onPlayerLeaveChannelRegisteredCallback = onPlayerLeaveCallback;

                    player.PlayerMessagesDispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_SENT_MESSAGE, onPlayerSentMessageRegisteredCallback);
                    player.PlayerMessagesDispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_REQUEST_LEAVE_CHANNEL, onPlayerLeaveChannelRegisteredCallback);
                }

                public void CloseChatConnection()
                {
                    player.PlayerMessagesDispatcher.UnregisterOnMessageReceivedHandler(onPlayerSentMessageRegisteredCallback);
                    player.PlayerMessagesDispatcher.UnregisterOnMessageReceivedHandler(onPlayerLeaveChannelRegisteredCallback);
                }

                static public implicit operator PlayerChatConnection(ConnectedPlayer player)
                {
                    return new PlayerChatConnection()
                    {
                        player = player,
                        onPlayerSentMessageRegisteredCallback = null
                    };
                }

                public override bool Equals(object obj)
                {
                    return (obj is PlayerChatConnection) && ((PlayerChatConnection)obj).player.Equals(player);
                }

                public override int GetHashCode()
                {
                    return player.GetHashCode();
                }
            }

            private HashSet<PlayerChatConnection> m_playersInChat;
            private SimpleDataMessagePacker<EMPTY_MESSAGE_DATA> m_joinChannelMessagePacker;
            private SimpleDataMessagePacker<EMPTY_MESSAGE_DATA> m_leaveChannelMessagePacker;
            private SimpleDataMessagePacker<PLAYER_SENT_MESSAGE_MESSAGE_DATA> m_chatMessageSentMessagePacker;



            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
            }

            public unsafe void SendChatMessageToAllPlayers(string sender, string msgContent)
            {
                PLAYER_SENT_MESSAGE_MESSAGE_DATA messageData = new PLAYER_SENT_MESSAGE_MESSAGE_DATA(sender, msgContent);

                var message = m_chatMessageSentMessagePacker.PackMessage(messageData);

                message.channelIDs.Clear();
                foreach(var connection in m_playersInChat)
                {
                    if (connection.player.ConnectionChannelAlive)
                    message.channelIDs.Add(connection.player.CommunicationChannelID);
                }

                StageMessageForSending(message);

                Debug.Log(sender + " : " + msgContent);
            }

            public void SendServerMessage(string message)
            {
                SendChatMessageToAllPlayers("[SERVER]", message);
            }

            private void RemovePlayerFromChat(ConnectedPlayer player, bool sendNotice = true)
            {
                if (m_playersInChat.Contains(player))
                    m_playersInChat.Remove(player);

                if (sendNotice)
                {
                    SendServerMessage("Player '" + player.Name + "' has left chat.");
                }
            }

            private void OnPlayerConnected(ConnectedPlayer connectedPlayer)
            {
                connectedPlayer.PlayerMessagesDispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.PLAYER_REQUEST_JOIN_CHANNEL, 
                    (message) => OnPlayerRequestedJoinChannel(connectedPlayer, message));

                connectedPlayer.OnPlayerDisconnected += () =>
                {
                    RemovePlayerFromChat(connectedPlayer, false);
                    SendServerMessage("Player '" + connectedPlayer.Name + "' disconnected.");
                };

                SendServerMessage("Player '" + connectedPlayer.Name + "' connected.");
            }

            private void OnPlayerRequestedJoinChannel(ConnectedPlayer player, MESSAGE message)
            {
                if (m_playersInChat.Contains(player) == false)
                {
                    PlayerChatConnection newConnection = new PlayerChatConnection();
                    newConnection.player = player;

                    newConnection.OpenChatConnection((msg) => OnPlayerSentMessage(player, msg), (msg) => OnPlayerRequestedLeaveChannel(newConnection, msg));

                    m_playersInChat.Add(newConnection);
                    
                    SendServerMessage("Player '" + player.Name + "' has joined chat.");
                }
            }

            private void OnPlayerRequestedLeaveChannel(PlayerChatConnection connection, MESSAGE message)
            {
                if (m_playersInChat.Contains(connection))
                {
                    m_playersInChat.Remove(connection);
                    connection.CloseChatConnection();
                    SendServerMessage("Player '" + connection.player.Name + " has left chat.");
                }
            }

            private void OnPlayerSentMessage(ConnectedPlayer player, MESSAGE message)
            {
                var messageData = m_chatMessageSentMessagePacker.UnpackMessage(message);
                unsafe
                {
                    string msg = StringEncoding.Decode(messageData.message, messageData.messageByteCount);
                    SendChatMessageToAllPlayers(player.Name, msg);
                }
            }

            protected override void OnManagerInitialize()
            {
                m_playersInChat = new HashSet<PlayerChatConnection>();
                m_joinChannelMessagePacker = new SimpleDataMessagePacker<EMPTY_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_REQUEST_JOIN_CHANNEL);
                m_leaveChannelMessagePacker = new SimpleDataMessagePacker<EMPTY_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_REQUEST_LEAVE_CHANNEL);
                m_chatMessageSentMessagePacker = new SimpleDataMessagePacker<PLAYER_SENT_MESSAGE_MESSAGE_DATA>(MESSAGE_TYPE.PLAYER_SENT_MESSAGE);

                ServerManager.Instance.Managers.Get<ConnectedPlayersManager>().OnPlayerConnected += OnPlayerConnected;
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
            }
        }
    }
}

