using System.Collections.Generic;
using System;
using NeoRTS.Communication.Messages;
using NeoRTS.Tools;

namespace NeoRTS
{
    namespace Communication
    {
        /// <summary>
        /// Able to dispatch any kind of MESSAGE object, and accept subscriptions to receiving either ANY kind of message
        /// or only messages with a specific header. Can also be given filter objects through which it will "run" every message,
        /// only dispatching them if they get through every single enabled filter.
        /// </summary>
        public class MessageDispatcher
        {
            public interface IFilter
            {
                bool Enabled { get; }
                bool FilterMessage(MESSAGE message);
            }

            // Contains all registered message handlers. Never actually called.
            private Action<MESSAGE> m_allMessageHandlers;


            private Dictionary<MESSAGE_TYPE, Action<MESSAGE>> m_headerSpecificMessageHandlers;
            private Dictionary<int, Action<MESSAGE>> m_channelIDSpecificMessageHandlers;
            private Action<MESSAGE> m_omniTypeMessageHandlers;
            private IFilter[] m_filters;

            public MessageDispatcher(params IFilter[] filters)
            {
                m_filters = filters;
                var allHeaders = (MESSAGE_TYPE[])Enum.GetValues(typeof(MESSAGE_TYPE));
                m_headerSpecificMessageHandlers = new Dictionary<MESSAGE_TYPE, Action<MESSAGE>>();
                m_channelIDSpecificMessageHandlers = new Dictionary<int, Action<MESSAGE>>();
                foreach (var header in allHeaders)
                {
                    m_headerSpecificMessageHandlers.Add(header, delegate { });
                }

                m_omniTypeMessageHandlers = delegate { };
            }
            public void RegisterOnMessageReceivedHandler(Action<MESSAGE> handler)
            {
                m_omniTypeMessageHandlers += handler;
                m_allMessageHandlers += handler;
            }

            public void RegisterOnMessageReceivedHandler(MESSAGE_TYPE header, Action<MESSAGE> handler)
            {
                m_headerSpecificMessageHandlers[header] += handler;
                m_allMessageHandlers += handler;
            }

            public void RegisterOnMessageReceivedHandler(int channelID, Action<MESSAGE> handler)
            {
                if (!m_channelIDSpecificMessageHandlers.ContainsKey(channelID))
                {
                    m_channelIDSpecificMessageHandlers.Add(channelID, handler);
                }
                else
                {
                    m_channelIDSpecificMessageHandlers[channelID] += handler;
                }
                m_allMessageHandlers += handler;
            }

            public void RegisterOnMessageReceivedHandlerOnTypeRange(int min, int max, Action<MESSAGE> handler)
            {
                RegisterOnMessageReceivedHandlerOnTypeRange(new MESSAGE_TYPE_RANGE(min, max), handler);
            }

            public void RegisterOnMessageReceivedHandlerOnTypeRange(MESSAGE_TYPE_RANGE range, Action<MESSAGE> handler)
            {
                MESSAGE_TYPE[] allKeys = new MESSAGE_TYPE[m_headerSpecificMessageHandlers.Count];
                m_headerSpecificMessageHandlers.Keys.CopyTo(allKeys, 0);

                foreach(var key in allKeys)
                {
                    int keyID = (int)key;
                    if (keyID >= range.Min && keyID <= range.Max)
                    {
                        RegisterOnMessageReceivedHandler(key, handler);
                    }
                }
                m_allMessageHandlers += handler;
            }

            public void UnregisterOnMessageReceivedHandler(Action<MESSAGE> handler)
            {
                if (!IsHandlerRegistered(handler))
                {
                    throw new Exception("ERROR : Attempted to unregister handler from Dispatcher to which it wasn't registered.");
                }

                m_omniTypeMessageHandlers -= handler;
                var keyCollection = m_headerSpecificMessageHandlers.Keys;
                MESSAGE_TYPE[] keys = new MESSAGE_TYPE[keyCollection.Count];
                int i = 0;
                foreach(var key in keyCollection)
                {
                    keys[i] = key;
                    i++;
                }
                foreach (var key in keys)
                {
                    m_headerSpecificMessageHandlers[key] -= handler;
                }

                var intKeyCollection = m_channelIDSpecificMessageHandlers.Keys;
                int[] intKeys = new int[intKeyCollection.Count];
                i = 0;
                foreach (var key in intKeyCollection)
                {
                    intKeys[i] = key;
                    i++;
                }
                foreach (var key in intKeys)
                {
                    m_channelIDSpecificMessageHandlers[key] -= handler;
                }

                m_allMessageHandlers -= handler;
            }

            public void DispatchMessage(MESSAGE message)
            {
                try
                {
                    var header = message.Header;
                    bool pastEveryFilter = true;
                    for (int i = 0; i < m_filters.Length && pastEveryFilter; i++)
                    {
                        IFilter filter = m_filters[i];
                        if (filter.Enabled && !filter.FilterMessage(message)) pastEveryFilter = false;
                    }


                    if (pastEveryFilter)
                    {
                        m_omniTypeMessageHandlers(message);
                        m_headerSpecificMessageHandlers[header](message);
                        if (m_channelIDSpecificMessageHandlers.ContainsKey(message.ChannelID))
                            m_channelIDSpecificMessageHandlers[message.ChannelID](message);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error receiving message on the server : " + e.Message + " " + e.StackTrace);
                }
            }

            /// <summary>
            /// Returns true only if the specified handler is registered in at least one way on this dispatcher.
            /// Returns false otherwise.
            /// </summary>
            public bool IsHandlerRegistered(Action<MESSAGE> handler)
            {
                var invocationList = m_allMessageHandlers.GetInvocationList();
                foreach(var invocation in invocationList)
                {
                    if ((Delegate)handler == invocation)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

}

