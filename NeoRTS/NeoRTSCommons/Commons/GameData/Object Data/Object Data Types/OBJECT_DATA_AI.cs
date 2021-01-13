using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_STANDALONE
#endif

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            [ObjectDataTypeID(0)]
            [Serializable]
            unsafe public struct OBJECT_DATA_AI : IEquatable<OBJECT_DATA_AI>
            {
                public const int UNIT_ORDER_DATA_BYTE_SIZE = 256;

                #region Data Structures
                public enum ORDER_TYPE
                {
                    NONE,
                    WANDER,
                    MOVE_TO_POSITION,
                    HOLD_POSITION,
                    ATTACK_MOVE_TO_POSITION,
                    ATTACK_TARGET,
                    USE_ABILITY
                }

                public struct ATTACK_TARGET_ORDER_DATA
                {
                    public uint targetID;
                    public bool forced;
                    public bool canAttackTarget;
                    public bool shouldSeekTarget;
                }

                #endregion

                public ORDER_TYPE orderType;


                public bool Equals(OBJECT_DATA_AI other)
                {
                    if (orderType != other.orderType)
                    {
                        return false;
                    }
                    else
                    {
                        switch(orderType)
                        {
                            case (ORDER_TYPE.MOVE_TO_POSITION):
                                return *MoveToPositionOrderData == *other.MoveToPositionOrderData;
                        }
                        return true;
                    }

                    
                }


                /// <summary>
                /// "Freely managed" memory that contains any data needed to understand what exactly the unit has been ordered to do.
                /// What this contains depends on the order type. MOVE_TO_POSITION will have a Position object stored in there.
                /// ATTACK_TARGET might have a unit ID, and so forth...
                /// 
                /// </summary>
                public fixed byte orderData[UNIT_ORDER_DATA_BYTE_SIZE];

                public Position* MoveToPositionOrderData
                {
                    get
                    {
                        fixed (byte* dataPtr = orderData)
                        {
                            return (Position*)dataPtr;
                        }
                    }
                }

                public ATTACK_TARGET_ORDER_DATA* AttackUnitOrderData
                {
                    get
                    {
                        fixed (byte* dataPtr = orderData)
                        {
                            return (ATTACK_TARGET_ORDER_DATA*)dataPtr;
                        }
                    }
                }

            }
        }
    }
}

