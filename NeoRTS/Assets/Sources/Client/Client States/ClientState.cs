using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData;
using System;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// Client States turn the <see cref="GameClient"/> into a State machine. They can contain their own managers, aswell as their own logic
        /// and data. They need to implement <see cref="OnStart"/>, <see cref="OnUpdate(float)"/>and <see cref="OnStop"/> to be usable.
        /// They also get, before Starting and Stopping the chance to setup and cleanup message reception with a passed Message Dispatcher
        /// (usually the Client's main dispatcher).
        /// 
        /// Client States will get "asked" for pending messages, and by default they return their managers'. That behavior can be overriden
        /// (<see cref="GetPendingMessages()"/> function) so that the Client State can, for example, "patch" the messages before the Client has a chance
        /// to send them.
        /// 
        /// NOTE : ClientState managers are accessible globally through the <see cref="GameClient"/> itself. When calling <see cref="GameClient.GetManager{T}"/> on the <see cref="GameClient"/>,
        /// after looking through managers directly owned by the GameClient, it will look through managers owned by the currently used State's.
        /// </summary>
        public abstract class ClientState
        {
            public abstract ManagersContainer StateManagers { get; protected set; }

            public void Start()
            {
                Debug.Log("Starting Client State " + this.GetType().Name);
                StateManagers.InitializeManagers();
                OnStart();
            }
            public void Update(float deltaTime)
            {
                StateManagers.UpdateManagers(deltaTime);
                OnUpdate(deltaTime);
            }
            public void Stop()
            {
                OnStop();
                Debug.Log("Stopped Client State " + this.GetType().ToString());
            }

            protected abstract void OnStart();
            protected abstract void OnUpdate(float deltaTime);
            protected abstract void OnStop();

            public virtual void MessageReceptionSetup(MessageDispatcher dispatcher)
            {
                StateManagers.InitializeManagersMessageReception(dispatcher);
            }

            public virtual void MessageReceptionCleanup(MessageDispatcher dispatcher)
            {
                StateManagers.CleanupManagersMessageReception(dispatcher);
            }

            /// <summary>
            /// By default, simply returns the messages the managers want to send.
            /// By overriding this, client states have a chance to "patch" messages before they are entrusted to the client itself
            /// for sending.
            /// </summary>
            public virtual MESSAGE[] GetPendingMessages()
            {
                return StateManagers.RetrieveAllManagersMessages();
            }
        }
    }
}


