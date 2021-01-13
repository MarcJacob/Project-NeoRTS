using NeoRTS.Communication;
using NeoRTS.GameData.ObjectData;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData.Workers;
using System.Collections.Generic;
using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Matches
        {

            /// <summary>
            /// A Match object contains all the required Managers and (TODO : actually put them inside a manager aswell ?) Workers
            /// to run the server-side code necessary for a game of NeoRTS. It manages two (TODO : Add a "end match report" data structure ?)
            /// publicly readable properties that need to be "handled" by whatever manages the Match 
            /// 
            /// (TODO : Write a MatchFactory class that appriopriately contains the code for all different "kinds" of match objects we want to create.
            /// Maybe even offload ALL manager object creation to there ?)
            /// 
            /// (TODO : Limit the max bandwith and CPU usage of a single Match (optionally, no reason to limit it on the Client)).
            /// </summary>
            public class Match
            {
                public struct MATCH_STARTED_DATA
                {
                    public MATCH_METADATA metadata;
                    public ObjectMemoryManager.OBJECT_SPAWN_DATA[] startUnits;
                }

                public struct MATCH_METADATA
                {
                    public int playerCount;
                    public int mapID;
                }

                public bool Ended { get; private set; }
                public Queue<MESSAGE> MessagesQueue { get; private set; }
                public ManagersContainer Managers { get { return m_matchManagers; } }

                private ManagersContainer m_matchManagers;
                private Game_Worker_Base[] m_matchWorkers;
                private MessageDispatcher m_messageDispatcher;


                private bool m_authoritative;

                public void DispatchMessage(MESSAGE msg)
                {
                    m_messageDispatcher.DispatchMessage(msg);
                }

                public Match(bool authoritative)
                {
                    Ended = false;
                    m_authoritative = authoritative;
                    MessagesQueue = new Queue<MESSAGE>();
                    ObjectMemoryManager objectMemoryManager;
                    try
                    {
                       objectMemoryManager = new ObjectMemoryManager(1000, authoritative);
                    }
                    catch (TypeInitializationException e)
                    {
                        throw e;
                    }
                    SynchronizationCheckMatchManager posCheckManager = new SynchronizationCheckMatchManager(objectMemoryManager, authoritative);
                    ObjectPositionGridManager objectPosGridManager = new ObjectPositionGridManager(authoritative, 2, 50, objectMemoryManager.MAX_UNIT_COUNT);
                    m_matchManagers = new ManagersContainer(objectMemoryManager, posCheckManager, objectPosGridManager);

                    m_matchWorkers = new Game_Worker_Base[]
                    {
                        new WORKER_ObjectPositionGridUpdater(objectPosGridManager.GetCellCoordinatesFromPosition, objectPosGridManager.CellGrid, objectMemoryManager.GetDataContainer<OBJECT_DATA_CELL_COORDS>().Data, objectMemoryManager.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data),
                        new WORKER_UnitTargetUpdater(objectPosGridManager.CellGrid, objectMemoryManager),

                        new WORKER_UnitOrderMovementDataUpdater(objectMemoryManager),
                        new WORKER_ObjectMovementProcessor(objectMemoryManager.GetDataContainer<OBJECT_DATA_MOVEMENT>().Data, objectMemoryManager.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data),

                        new WORKER_UnitReachedPositionChecker(objectMemoryManager.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data, objectMemoryManager.GetDataContainer<OBJECT_DATA_AI>().Data),
                        new WORKER_ObjectWeaponUpdater(objectMemoryManager, objectMemoryManager.ObjectDestroyRequestQueue),
                        new WORKER_ProductionQueueUpdater(objectMemoryManager, objectMemoryManager.ObjectSpawnRequestQueue),
                    
                        new WORKER_GroundUnitCollisionsProcessor(objectMemoryManager)
                    };

                    foreach(var worker in m_matchWorkers)
                    {
                        worker.AssignObjectsDataTable(objectMemoryManager.Objects);
                    }

                    m_messageDispatcher = new MessageDispatcher();
                    m_matchManagers.InitializeManagers();
                    m_matchManagers.InitializeManagersMessageReception(m_messageDispatcher);
                }

                public void OnMatchStart(MATCH_STARTED_DATA startData)
                {
                    foreach(var unitSpawnData in startData.startUnits)
                    {
                        Managers.Get<ObjectMemoryManager>().ObjectSpawnRequestQueue.Enqueue(unitSpawnData);
                    }

                }

                public void UpdateMatch(float deltaTime)
                {
                    m_matchManagers.UpdateManagers(deltaTime);

                    // TODO : Move workers (or at least worker management code) to a manager object for which it is acceptable to cache the Unit Memory Manager.

                    foreach (var worker in m_matchWorkers)
                    {
                        if (m_authoritative || worker.RunsOnlyOnAuthoritativeMatch == false)
                            worker.OnFrameBegin(deltaTime);
                    }

                    var objectMemoryManager = m_matchManagers.Get<ObjectMemoryManager>();
                    foreach(var worker in m_matchWorkers)
                    {
                        if (m_authoritative || worker.RunsOnlyOnAuthoritativeMatch == false)
                        {
                            worker.PreWork(deltaTime);
                            for (uint unitID = 0; unitID < objectMemoryManager.MAX_UNIT_COUNT; unitID++)
                            {
                                if (objectMemoryManager.IsUnitAlive(unitID))
                                {
                                    worker.RunWorkOnID(deltaTime, unitID);
                                }
                            }
                            worker.PostWork(deltaTime);
                        }
                    }

                    foreach (var worker in m_matchWorkers)
                    {
                        if (m_authoritative || worker.RunsOnlyOnAuthoritativeMatch == false)
                            worker.OnFrameEnd(deltaTime);
                    }


                    var messages = Managers.RetrieveAllManagersMessages();
                    foreach(var message in messages)
                    {
                        MessagesQueue.Enqueue(message);
                    }
                }

                public void ForceEnd()
                {
                    Ended = true;
                }
            }
        }
    }
}

