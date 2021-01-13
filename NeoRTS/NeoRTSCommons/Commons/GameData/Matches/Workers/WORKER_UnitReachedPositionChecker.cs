using NeoRTS.GameData.ObjectData;
using NeoRTS.Tools;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            /// <summary>
            /// This Worker acts over UNIT_DATA_STATE, UNIT_DATA_TRANSFORM and OBJECT_DATA_AI memory sequences.
            /// Only READS from STATE and TRANSFORM, and WRITES into ORDER.
            /// 
            /// This Worker checks if a unit is alive and if it has a MOVE_TO_POSITION order. If both are true,
            /// then the worker checks if the unit has reached its destination. If true, then the unit's order is reset to NONE.
            /// </summary>
            public class WORKER_UnitReachedPositionChecker : Game_Worker_Base
            {
                private OBJECT_DATA_AI[] objectAIData;
                private OBJECT_DATA_TRANSFORM[] objectTransforms;
                public WORKER_UnitReachedPositionChecker(OBJECT_DATA_TRANSFORM[] transforms, OBJECT_DATA_AI[] orders)
                {
                    objectAIData = orders;
                    objectTransforms = transforms;
                }

                public override void RunWorkOnID(float deltaTime, uint objectID)
                {
                    var transformDataSlot = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(objectID);
                    var aiDataSlot = FetchDataSlotForObject<OBJECT_DATA_AI>(objectID);

                    if (transformDataSlot == uint.MaxValue || aiDataSlot == uint.MaxValue) return;

                    if (objectAIData[aiDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION)
                    {
                        unsafe
                        {
                            var posPtr = objectAIData[aiDataSlot].MoveToPositionOrderData;
                            if (Position.SquaredDistance(objectTransforms[transformDataSlot].position, *posPtr) < 0.1f)
                            {
                                objectAIData[aiDataSlot].orderType = OBJECT_DATA_AI.ORDER_TYPE.NONE;
                            }
                        }

                    }

                }
            }
        }
    }
}

