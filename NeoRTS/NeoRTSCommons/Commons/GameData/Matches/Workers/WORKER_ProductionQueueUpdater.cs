using NeoRTS.GameData.ObjectData;
using NeoRTS.Tools;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            public class WORKER_ProductionQueueUpdater : Game_Worker_Base
            {
                // READ
                private OBJECT_DATA_TRANSFORM[] m_transformData;
                private OBJECT_DATA_OWNER[] m_ownerData;

                // READ & WRITE
                private OBJECT_DATA_PERIODIC_SPAWNER[] m_spawnerData;
                

                private Queue<ObjectMemoryManager.OBJECT_SPAWN_DATA> m_spawnQueue;

                public WORKER_ProductionQueueUpdater(IObjectDataContainersHolder dataContainersHolder, Queue<ObjectMemoryManager.OBJECT_SPAWN_DATA> spawnQueue)
                {
                    m_spawnerData = dataContainersHolder.GetDataContainer<OBJECT_DATA_PERIODIC_SPAWNER>().Data;
                    m_transformData = dataContainersHolder.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data;
                    m_ownerData = dataContainersHolder.GetDataContainer<OBJECT_DATA_OWNER>().Data;
                    
                    m_spawnQueue = spawnQueue;
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    uint periodicSpawnerDataID = FetchDataSlotForObject<OBJECT_DATA_PERIODIC_SPAWNER>(ID);
                    if (periodicSpawnerDataID == uint.MaxValue) return;

                    m_spawnerData[periodicSpawnerDataID].spawnClock -= deltaTime;
                    if (m_spawnerData[periodicSpawnerDataID].spawnClock <= 0f)
                    {
                        m_spawnerData[periodicSpawnerDataID].spawnClock = m_spawnerData[periodicSpawnerDataID].spawnCooldown;
                        // Spawn object !
                        uint transformDataID, ownerDataID;
                        transformDataID = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(ID);
                        ownerDataID = FetchDataSlotForObject<OBJECT_DATA_OWNER>(ID);

                        Position spawnPosition;
                        int ownerID;

                        // TODO : Make these two data components optional.
                        if (transformDataID == uint.MaxValue || ownerDataID == uint.MaxValue) return;

                        spawnPosition = m_transformData[transformDataID].position;
                        spawnPosition += new Position(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                        ownerID = m_ownerData[ownerDataID].ownerID;

                        ObjectMemoryManager.OBJECT_SPAWN_DATA spawnData = new ObjectMemoryManager.OBJECT_SPAWN_DATA()
                        {
                            startPosition = spawnPosition,
                            owner = ownerID,
                            objectTypeID = m_spawnerData[periodicSpawnerDataID].spawnedObjectTypeID
                        };

                        m_spawnQueue.Enqueue(spawnData);
                    }
                }
            }
        }
    }
}

