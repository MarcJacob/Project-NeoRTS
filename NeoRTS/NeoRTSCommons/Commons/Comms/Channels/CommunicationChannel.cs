using System.Collections;
using System;
using NeoRTS.Communication.Messages;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace Communication
    {
        public enum CHANNEL_STATE
        {
            INITIALIZED, // Has just been constructed, no opening has been attempted yet.
            OPENING, // Channel is in the process of establishing connection.
            READY, // Channel is ready to transmit and receive messages.
            CLOSED, // Channel has been voluntarily closed in some way.
            FAILED // Channel has failed to open. The reason why can be found in the FailedReason string.
        }
        /// <summary>
        /// IClientServerCommInterface is a specification (interface) for classes that are to be used
        /// as a means of communication between a client app and a (many ?) server app(s).
        /// It doesn't NECESSARILY contain networking code. Only the guarantee that it's able to somehow send
        /// messages (+ Encode) to something that should hopefully receive them, and receive messages (+ Decode) if it
        /// actually receives anything.
        /// </summary>
        public abstract class CommunicationChannel
        {
            public CHANNEL_STATE State { get; protected set; }
            public string FailReason { get; protected set; } // TODO consider changing this (or adding) to an enum with all possible reasons rather than a string.
            public int ChannelID { get; private set; } = -1;

            public event Action<CHANNEL_STATE> OnChannelClosedOrFailed = delegate { };

            private MessageDispatcher m_messageDispatcher;
            public void AssignMessageDispatcher(MessageDispatcher dispatcher)
            {
                if (m_messageDispatcher != null) throw new Exception("ERROR : Attempted to assign a MessageDispatcher to a channel that already has one !");

                m_messageDispatcher = dispatcher;
            }
            public void AssignChannelID(int ID)
            {
                ChannelID = ID;
            }
            protected void FailChannel(string reason)
            {
                State = CHANNEL_STATE.FAILED;
                FailReason = reason;
                OnChannelClosedOrFailed(CHANNEL_STATE.FAILED);
            }
            protected void DispatchReceivedMessage(MESSAGE message)
            {
                if (ChannelID == -1) throw new Exception("ERROR : Receiving channel has not been assigned an ID !");

                message.ChannelID = ChannelID;

                m_messageDispatcher.DispatchMessage(message);
            }
            public void OpenChannel()
            {
                if (State != CHANNEL_STATE.INITIALIZED) return;
                if (m_messageDispatcher == null) throw new Exception("ERROR : Attempted to open a channel without assigning a Dispatcher to it.");

                // At this point we assume the Opening was successful.
                State = CHANNEL_STATE.OPENING;

                OnOpen();
            }
            abstract protected void OnOpen();

            protected abstract void Send(MESSAGE message);

            protected abstract void OnClose();
            public void CloseChannel()
            {
                if (State == CHANNEL_STATE.READY)
                {
                    OnClose();
                    State = CHANNEL_STATE.CLOSED;
                    OnChannelClosedOrFailed(CHANNEL_STATE.CLOSED);
                }
                else
                {
                    throw new Exception("ERROR : Cannot close a channel that wasn't successfully opened.");
                }
            }

            public void SendMessage(MESSAGE message)
            {
                if (State != CHANNEL_STATE.READY) throw new Exception("ERROR : Cannot send a message through a channel that hasn't been successfully opened !");

                Send(message);
            }
        }

        /// <summary>
        /// Wrapper for a list of Communication Channels. Gives access to utility functions to quickly add & remove a channel
        /// by ID while properly managing the channel object itself.
        /// To access a channel of a given ID, use the [] operator.
        /// </summary>
        public class ChannelCollection
        {
            private List<CommunicationChannel> m_commChannels = new List<CommunicationChannel>(); // Internal channels list. When removing a channel, to keep IDs consistent we just set that member of the list to "null".

            public int ChannelCount
            {
                get { return m_commChannels.Count; }
            }

            public void AddChannel(int ID, CommunicationChannel newChannelObject, bool openChannel = true)
            {
                if (m_commChannels.Count <= ID)
                {
                    for (int i = m_commChannels.Count; i < ID; i++)
                    {
                        m_commChannels.Add(null);
                    }

                    m_commChannels.Add(newChannelObject);
                }
                else
                {
                    if (m_commChannels[ID] == null)
                    {
                        m_commChannels[ID] = newChannelObject;
                    }
                    else
                    {
                        // TODO : Consider checking whether the channel in place is Failed or Closed and automatically take its place.
                        // For now, throw an exception if anything other than null is present at this ID.
                        throw new Exception("ERROR : Attempted to add a channel to an ID which already has a channel.");
                    }
                }

                newChannelObject.AssignChannelID(ID);
                if (openChannel) newChannelObject.OpenChannel();
            }
            public int AddChannel(CommunicationChannel newChannelObject, bool openChannel = true)
            {
                // OPTIMIZE : Track null channels in a list somewhere so as not to have to find them in a "dumb" way.
                // This might become necessary for the server for cases where it might have thousands of players connected and connecting.
                for (int id = 0; id < m_commChannels.Count; id++)
                {
                    if (m_commChannels[id] == null)
                    {
                        m_commChannels[id] = newChannelObject;
                        newChannelObject.AssignChannelID(id);
                        if (openChannel) newChannelObject.OpenChannel();
                        return id;
                    }
                }

                m_commChannels.Add(newChannelObject);
                newChannelObject.AssignChannelID(m_commChannels.Count - 1);
                if (openChannel) newChannelObject.OpenChannel();
                return m_commChannels.Count - 1;
            }
            public void RemoveChannel(int ID)
            {
                if (ID >= m_commChannels.Count || m_commChannels[ID] == null)
                {
                    throw new Exception("ERROR : Attempted to remove invalid Channel ID from Channel Collection.");
                }
                else if (m_commChannels[ID].State != CHANNEL_STATE.CLOSED && m_commChannels[ID].State != CHANNEL_STATE.FAILED)
                {
                    throw new Exception("ERROR : Attempted to remove non-closed & non-failed channel from Channel Collection.");
                }

                m_commChannels[ID] = null;
            }
            public void CloseChannel(int ID)
            {
                if (ID >= m_commChannels.Count || m_commChannels[ID] == null)
                {
                    throw new Exception("ERROR : Attempted to remove invalid Channel ID from Channel Collection.");
                }

                m_commChannels[ID].CloseChannel();
            }

            public bool ChannelOpened(int ID)
            {
                return (ID < m_commChannels.Count && m_commChannels[ID] != null && m_commChannels[ID].State == CHANNEL_STATE.READY);
            }

            public void RemoveClosedAndFailedChannels()
            {
                for(int i = 0; i < m_commChannels.Count; i++)
                {
                    if (m_commChannels[i] != null)
                    {
                        CHANNEL_STATE state = m_commChannels[i].State;
                        if (state == CHANNEL_STATE.CLOSED || state == CHANNEL_STATE.FAILED)
                        {
                            m_commChannels[i] = null;
                        }
                    }
                }
            }

           
            public CommunicationChannel this[int index]
            {
                get
                {
                    if (m_commChannels.Count <= index) throw new IndexOutOfRangeException();
                    else return m_commChannels[index];
                }
            }
        }
    }

}

