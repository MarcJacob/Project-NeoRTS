using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace GameData
    {

        /// <summary>
        /// This Match Manager object is used to confirm synchronicity between the Client and the server.
        /// If it is Authoritative, then it sends out messages regularily with data the client can compare its own
        /// data against and detect synchronization problems.
        /// 
        /// If it is Submissive, then it only reacts to synchronization check messages and runs the appropriate checks
        /// comparing the received data against the local data. Outputs a warning message (TODO : Stop the match ? Trigger a lag event ?) if it detects any.
        /// </summary>
        public class SynchronizationCheckMatchManager : MatchManagerObject
        {
            const float CHECK_COOLDOWN = 0.1f;

            private ObjectMemoryManager m_unitDataManager;
            private float m_clock = 0f;


            public SynchronizationCheckMatchManager(ObjectMemoryManager unitDataManager, bool authoritative) : base(authoritative)
            {
                m_unitDataManager = unitDataManager;
            }

            protected override void OnManagerInitialize()
            {
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.SYNCH_CHECK, OnSynchCheckMessage);
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.UnregisterOnMessageReceivedHandler(OnSynchCheckMessage);
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
                if (!Authoritative) return;

                m_clock -= deltaTime;
                if (m_clock < 0f)
                {
                   /* UNIT_DATA_CHANGED_MESSAGE_DATA<Position> messageData = new UNIT_DATA_CHANGED_MESSAGE_DATA<Position>();
                    List<uint> livingUnitIDs = new List<uint>();
                    LinkedList<uint> test = new LinkedList<uint>();
                    for (uint i = 0; i < m_unitDataManager.UnitStates.Count; i++)
                    {
                        UNIT_DATA_STATE unitState = m_unitDataManager.UnitStates[(int)i];
                        if (unitState.isAlive) livingUnitIDs.Add(i);
                    }

                    messageData.unitIDs = livingUnitIDs.ToArray();
                    messageData.unitData = new Position[livingUnitIDs.Count];

                    for(int i = 0; i < livingUnitIDs.Count; i++)
                    {
                        messageData.unitData[i] = m_unitDataManager.UnitTransforms[livingUnitIDs[i]].position;
                    }

                    //StageMessageForSending(m_synchCheckMessagePacker.PackMessage(messageData));*/

                    m_clock = CHECK_COOLDOWN;
                }
            }

            private void OnSynchCheckMessage(MESSAGE msg)
            {
               /* var data = m_synchCheckMessagePacker.UnpackMessage(msg);
                for (int i = 0; i < data.unitIDs.Length; i++)
                {
                    Position diff = m_unitDataManager.UnitTransforms[data.unitIDs[i]].position - data.unitData[i];
                    if (((Vector3)diff).magnitude > 0.1f)
                    {
#if UNITY_STANDALONE
                        Debug.LogWarning("DESYNCH AND YOU DON'T EVEN HAVE NETWORK LATENCY YOU ABSOLUTE COCONUT OF A DEV");
#endif
                    }
                }*/
            }
        }
    }
}

