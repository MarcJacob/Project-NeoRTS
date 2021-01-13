#if UNITY_STANDALONE
#endif

using System;
using System.Runtime.InteropServices;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            /// <summary>
            /// Unit Data type that contains the Player ID of its owner. Only the owner can control this unit.
            /// For now changing a unit's owner "dynamically" is not supported.
            /// 
            /// A Unit's owner ID also determines which other units it will be hostile / friendly to.
            /// </summary>
            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_OWNER
            {
                public int ownerID;
            }
        }
    }
}

