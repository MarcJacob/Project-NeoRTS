#if UNITY_STANDALONE
#endif

using NeoRTS.GameData.ObjectData.Types;
using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            /// <summary>
            /// Automatically added component to any object that was created using an <see cref="ObjectType"/>.
            /// Allows finding out what object type an object is. For example, needed by the client to figure out
            /// which actor and pawn to use to display that object.
            /// </summary>
            [ObjectDataTypeID(0)]
            public struct OBJECT_DATA_OBJECT_TYPE
            {
                public int objectTypeID;
            }
        }
    }
}

