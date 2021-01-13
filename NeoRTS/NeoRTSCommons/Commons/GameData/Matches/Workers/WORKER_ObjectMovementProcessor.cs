using NeoRTS.GameData.ObjectData;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            public class WORKER_ObjectMovementProcessor : Game_Worker_Base
            {
                private const float MOVE_VECTOR_EPSILON = 0.01f;

                // Read & Write
                private OBJECT_DATA_MOVEMENT[] m_objectMovementDatas;

                // Write
                private OBJECT_DATA_TRANSFORM[] m_objectTransformDatas;

                public WORKER_ObjectMovementProcessor(OBJECT_DATA_MOVEMENT[] objectMovements, OBJECT_DATA_TRANSFORM[] objectTransforms)
                {
                    m_objectMovementDatas = objectMovements;
                    m_objectTransformDatas = objectTransforms;
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    var objectMovementID = FetchDataSlotForObject<OBJECT_DATA_MOVEMENT>(ID);
                    var objectTransformID = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(ID);

                    if (objectMovementID == uint.MaxValue || objectTransformID == uint.MaxValue) return;
                    if (m_objectMovementDatas[objectMovementID].Moving == false) return;

                    Position movementThisFrame = m_objectMovementDatas[objectMovementID].movementVectorNormalized * OBJECT_DATA_MOVEMENT.OBJECT_SPEED_PER_SECOND * deltaTime;

                    m_objectTransformDatas[objectTransformID].position += movementThisFrame;
                    m_objectMovementDatas[objectMovementID].movementVector -= movementThisFrame;

                    if (Position.IsEqualToWithEpsilon(m_objectMovementDatas[objectMovementID].movementVector, Position.Zero, MOVE_VECTOR_EPSILON))
                    {
                        m_objectMovementDatas[objectMovementID].movementVector = Position.Zero;
                        m_objectMovementDatas[objectMovementID].movementVectorNormalized = Position.Zero;
                    }
                }
            }
        }
    }
}

