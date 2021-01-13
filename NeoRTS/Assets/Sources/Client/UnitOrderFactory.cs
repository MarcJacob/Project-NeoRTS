using NeoRTS.GameData;
using NeoRTS.GameData.ObjectData;
using System;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// UnitOrderFactory is a static factory class allowing the eased creation of an array of ready-to-use OBJECT_DATA_AI objects
        /// depending on the passed set of unit IDs and one specific data object and type.
        /// 
        /// For example, passing it a Position is assumed to be the user ordering units to move somewhere. As such it outputs
        /// a corresponding set of OBJECT_DATA_AI objects with the MOVE_TO type and the position as Data.
        /// 
        /// // TODO : Consider renaming this class to better reflect its purpose (create Order data for units from one piece
        /// of data and a set of Unit IDs), or moving it to shared code (GameData namespace) or to change the way giving
        /// "default" orders to units with one kind of input works exactly).
        /// </summary>
        public static class UnitOrderFactory
        {
            static public OBJECT_DATA_AI DispatchOrderWithTargetPosition(IEnumerable<uint> selectedUnits, Position targetPosition)
            {
                OBJECT_DATA_AI dataOrder = new OBJECT_DATA_AI()
                {
                    orderType = OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION,
                };
                unsafe
                {
                    Buffer.MemoryCopy(&targetPosition, dataOrder.orderData, OBJECT_DATA_AI.UNIT_ORDER_DATA_BYTE_SIZE, sizeof(Position));
                }

                return dataOrder;
            }
        }
    }
}
