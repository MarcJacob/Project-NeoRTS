using NeoRTS.GameData.ObjectData;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {

            /// <summary>
            /// This Worker acts over OBJECT_DATA_AI and UNIT_DATA_TRANSFORM memory sequences.
            /// Only READS from Orders, and WRITES into TRANSFORM.
            /// 
            /// // TODO : Change this so that instead of directly moving a unit, it just sets its "movement goal"
            /// and something else manages the actual movement.
            /// </summary>
            public class WORKER_UnitOrderMovementDataUpdater : Game_Worker_Base
            {
                private OBJECT_DATA_AI[] m_objectAIDatas;
                private OBJECT_DATA_MOVEMENT[] m_objectMovementDatas;
                private OBJECT_DATA_TRANSFORM[] m_objectTransformDatas;
                private OBJECT_DATA_WEAPON[] m_objectWeaponDatas;

                // TODO : Consider moving this to a different worker

                public WORKER_UnitOrderMovementDataUpdater(IObjectDataContainersHolder containersHolder)
                {
                    m_objectAIDatas = containersHolder.GetDataContainer<OBJECT_DATA_AI>().Data;
                    m_objectMovementDatas = containersHolder.GetDataContainer<OBJECT_DATA_MOVEMENT>().Data;
                    m_objectTransformDatas = containersHolder.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data;
                    m_objectWeaponDatas = containersHolder.GetDataContainer<OBJECT_DATA_WEAPON>().Data;
                }

                public override void RunWorkOnID(float deltaTime, uint objectID)
                {
                    var aiDataID = FetchDataSlotForObject<OBJECT_DATA_AI>(objectID);
                    var movementDataID = FetchDataSlotForObject<OBJECT_DATA_MOVEMENT>(objectID);
                    var transformDataID = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(objectID);

                    if (aiDataID == uint.MaxValue || movementDataID == uint.MaxValue || transformDataID == uint.MaxValue) return;

                    var myPosition = m_objectTransformDatas[transformDataID].position;

                    if (m_objectAIDatas[aiDataID].orderType == OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION)
                    {
                        unsafe
                        {
                            Position* targetPositionPtr = m_objectAIDatas[aiDataID].MoveToPositionOrderData;
                            m_objectMovementDatas[movementDataID].SetMovementTarget(*targetPositionPtr, myPosition);
                        }
                    }
                    else if (m_objectAIDatas[movementDataID].orderType == OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                    {
                        uint weaponDataID = FetchDataSlotForObject<OBJECT_DATA_WEAPON>(objectID);
                        if (weaponDataID != uint.MaxValue)
                        unsafe
                        {
                            OBJECT_DATA_AI.ATTACK_TARGET_ORDER_DATA* orderDataPtr = m_objectAIDatas[aiDataID].AttackUnitOrderData;

                            var targetTransformID = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(orderDataPtr->targetID);
                            if (targetTransformID != uint.MaxValue)
                            {
                                var targetPosition = m_objectTransformDatas[targetTransformID].position;

                                float squaredDistToTarget = Position.SquaredDistance(targetPosition, myPosition);
                                bool moving = m_objectMovementDatas[movementDataID].Moving;
                                float squaredRange = m_objectWeaponDatas[weaponDataID].weaponRange * m_objectWeaponDatas[weaponDataID].weaponRange;
                                if ( !moving && squaredDistToTarget >= squaredRange
                                    || moving && squaredDistToTarget >= squaredRange - 0.1f)
                                {
                                    m_objectMovementDatas[movementDataID].SetMovementTarget(targetPosition, myPosition);
                                }
                                else
                                {
                                    m_objectMovementDatas[movementDataID].StopMoving();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (m_objectMovementDatas[movementDataID].Moving)
                            m_objectMovementDatas[movementDataID].StopMoving();
                    }
                }
            }
        }
    }
}

