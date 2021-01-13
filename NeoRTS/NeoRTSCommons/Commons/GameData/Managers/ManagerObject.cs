using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.Tools;
using System;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {

        /// <summary>
        /// Managers Containers manage a collection of Manager objects that inherit from the ManageObject class. (TODO : Add a template argument to be able to hold more specialized subtypes ?)
        /// They allow the retrieval of one of these objects by passing its type. Henceforth the collection should NOT contain two managers of the
        /// same type (NOTE : There is currently no checks upon construction to see if there are two managers of the same type).
        /// Also includes utility functions such as updating / initializing all managers contained within.
        /// </summary>
        public class ManagersContainer
        {
            private ManagerObject[] m_managers;

            public ManagersContainer(params ManagerObject[] managerObjects)
            {
                m_managers = managerObjects;
            }

            /// <summary>
            /// Returns the manager object of the corresponding type. If there is no manager object of that type,
            /// returns null.
            /// 
            /// NOTE : Pretty inefficient. Should not be called multiple times a frame / multiple times by the same object instance (IE Favor caching the managers)
            /// </summary>
            public T Get<T>() where T : ManagerObject
            {
                foreach (var manager in m_managers)
                {
                    if (manager is T) return (T)manager;
                }

                return null;
            }
            public void UpdateManagers(float deltaTime)
            {
                foreach(var manager in m_managers)
                {
                    manager.UpdateManager(deltaTime);
                }
            }
            public void InitializeManagers()
            {
                foreach(var manager in m_managers)
                {
                    manager.InitializeManager();
                }
            }

            public MESSAGE[] RetrieveAllManagersMessages()
            {
                List<MESSAGE> messagesList = new List<MESSAGE>();
                foreach(var manager in m_managers)
                {
                    var messages = manager.RetrieveMessagesToSend();
                    foreach(var msg in messages)
                    {
                        messagesList.Add(msg);
                    }
                }

                // TODO : Current priority for sending is first manager in list on construction sends first. Consider adding (among much more metadata) a notion of "Priority" and reorder the list depending on that (here or elsewhere).
                return messagesList.ToArray();
            }

            public void InitializeManagersMessageReception(MessageDispatcher messageDispatcher)
            {
                for (int i = 0; i < m_managers.Length; i++)
                {
                    ManagerObject manager = m_managers[i];
                    manager.OnManagerInitializeMessageReception(messageDispatcher);
                }
            }

            public void CleanupManagersMessageReception(MessageDispatcher messageDispatcher)
            {
                for (int i = 0; i < m_managers.Length; i++)
                {
                    ManagerObject manager = m_managers[i];
                    manager.OnManagerCleanupMessageReception(messageDispatcher);
                }
            }
        }


        /// <summary>
        /// Manager Objects are pieces of logic that require their own data, and optionally to be initialized and / or ran
        /// over periods of time. They usually represent logical units of code that thus "manage" a certain part of the game as a whole
        /// (gameplay possibly but also non-gameplay aspect like net connectivity, menu navigation...). They are usually found as part of
        /// <see cref="ManagersContainer"/>s.
        /// 
        /// Managers are able to communicate with connected clients / servers through MESSAGE objects in a way that
        /// remains agnostic to what actually "owns" them. This is done by "staging" messages through the <see cref="StageMessageForSending(MESSAGE)"/> function.
        /// STAGED messages are, in principle, regularly "polled" by the owner of this manager and presumably sent to their destination.
        /// 
        /// For message reception, Managers have a chance of "subscribing" to message reception the same way any other game system can during
        /// a specific Initialization function (<see cref="OnManagerInitializeMessageReception(IMessageDispatcher)"/>. The passed IMessageDispatcher is
        /// supposed to be what will be able to dispatch messages to the manager.
        /// 
        /// NOTE : There is NO guarantee that staged messages will actually be sent. A Manager's code should never have its own communication code.
        /// </summary>
        public abstract class ManagerObject
        {
            // TODO Consider moving the Message sending related code to a specialized subsclass : not EVERY manager will be sending messages.
            private List<MESSAGE> m_messagesToSend;
            protected int StagedMessagesCount { get { return m_messagesToSend.Count; } }
            public MESSAGE[] RetrieveMessagesToSend()
            {
                var array = m_messagesToSend.ToArray();
                m_messagesToSend.Clear();
                return array;
            }
            protected void StageMessageForSending(MESSAGE msg)
            {
                m_messagesToSend.Add(msg);
            }

            public void InitializeManager()
            {
                m_messagesToSend = new List<MESSAGE>(10);
                Debug.Log("Manager type <" + this.GetType().ToString() + "> initializing...");
                OnManagerInitialize();
            }
            public void UpdateManager(float deltaTime)
            {
                OnManagerUpdate(deltaTime);
            }

            abstract protected void OnManagerInitialize();
            public abstract void OnManagerInitializeMessageReception(MessageDispatcher dispatcher);
            public abstract void OnManagerCleanupMessageReception(MessageDispatcher dispatcher);
            abstract protected void OnManagerUpdate(float deltaTime);
        }

        /// <summary>
        /// All managers used by a Match object need to inherit from this ManagerObject subtype. This subtype
        /// handles the notion of whether the manager (and by extension, its Match) is authoritative or not.
        /// When not authoritative, it reacts to messages being received in a "submissive" way, with vastly reduced
        /// security & sanity checks. It also does not send any messages (IE it's purely "mimicking".
        /// 
        /// When it IS authoritative, It both reacts AND sends messages. When reacting to a message, it runs checks
        /// meant to prevent cheating and bugs (For example, a player trying to give orders to a unit it does not control).
        /// It assumes that its own representation of the current Game State is the correct one.
        /// </summary>
        public abstract class MatchManagerObject : ManagerObject
        {
            protected bool Authoritative { get; private set; }
            public MatchManagerObject(bool authoritative)
            {
                Authoritative = authoritative;
            }
              
        }
    }
}

