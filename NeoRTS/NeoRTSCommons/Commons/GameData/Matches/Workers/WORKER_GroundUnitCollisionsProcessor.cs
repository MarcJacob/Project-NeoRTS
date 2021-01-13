using NeoRTS.GameData.ObjectData;
using NeoRTS.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static NeoRTS.GameData.ObjectMemoryManager;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            public class WORKER_GroundUnitCollisionsProcessor : Game_Worker_Base
            {
                private const float COLLISION_PUSHBACK_FORCE_MULTIPLIER = 5f;

                // READ
                private readonly OBJECT_DATA_GROUND_COLLISION[] m_collisionData;
                private readonly OBJECT_DATA_CELL_COORDS[] m_cellCoordsData;

                // READ & WRITE
                private OBJECT_DATA_TRANSFORM[] m_transformData;

                public WORKER_GroundUnitCollisionsProcessor(IObjectDataContainersHolder containersHolder)
                {
                    m_collisionData = containersHolder.GetDataContainer<OBJECT_DATA_GROUND_COLLISION>().Data;
                    m_transformData = containersHolder.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data;
                    m_cellCoordsData = containersHolder.GetDataContainer<OBJECT_DATA_CELL_COORDS>().Data;
                    m_collisionWorld = new List<Collider>();
                }

                private struct Collider
                {
                    public uint objectID;
                    public OBJECT_DATA_GROUND_COLLISION collision;
                    public Position position;
                }

                private List<Collider> m_collisionWorld; 

                public override void PreWork(float deltaTime)
                {
                    m_collisionWorld.Clear();

                    for (int objectID = 0; objectID < ObjectsDataTable.Count; objectID++)
                    {
                        OBJECT obj = ObjectsDataTable[objectID];
                        if (obj.alive)
                        {
                            uint collisionID = obj.GetDataSlotIDForType<OBJECT_DATA_GROUND_COLLISION>();
                            uint transformID = obj.GetDataSlotIDForType<OBJECT_DATA_TRANSFORM>();

                            if (collisionID != uint.MaxValue && transformID != uint.MaxValue)
                            {
                                Collider newCollider = new Collider()
                                {
                                    objectID = (uint)objectID,
                                    collision = m_collisionData[collisionID],
                                    position = m_transformData[transformID].position
                                };

                                m_collisionWorld.Add(newCollider);
                            }
                        }
                    }

                    Debug.Log("Collision world size : " + m_collisionWorld.Count);
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    uint collisionID = FetchDataSlotForObject<OBJECT_DATA_GROUND_COLLISION>(ID);
                    uint transformID = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(ID);
                    if (collisionID == uint.MaxValue || transformID == uint.MaxValue) return;

                    OBJECT_DATA_GROUND_COLLISION myCollision = m_collisionData[collisionID];
                    if (myCollision.allowPushback == false) return;

                    List<Position> collisionVectors = new List<Position>();
                    Position myPosition = m_transformData[transformID].position;
                    

                    foreach (var collider in m_collisionWorld)
                    {
                        if (collider.objectID != ID)
                        {
                            float squaredDistance = Position.SquaredDistance(myPosition, collider.position);
                            float collisionDist = collider.collision.collisionSize + myCollision.collisionSize;
                            if (squaredDistance < collisionDist * collisionDist)
                            {
                                Position collisionVector = (myPosition - collider.position).GetNormalized() * (1 / (squaredDistance));
                                collisionVectors.Add(collisionVector);
                            }
                        }
                    }

                    if (collisionVectors.Count > 0)
                    {
                        Position finalCollisionVector = Position.Zero;
                        
                        foreach(var vector in collisionVectors)
                        {
                            finalCollisionVector += vector;
                        }

                        //finalCollisionVector *= (float)1 / collisionVectors.Count;

                        m_transformData[transformID].position += finalCollisionVector * deltaTime * COLLISION_PUSHBACK_FORCE_MULTIPLIER;
                    }
                    

                }
            }
        }
    }
}

