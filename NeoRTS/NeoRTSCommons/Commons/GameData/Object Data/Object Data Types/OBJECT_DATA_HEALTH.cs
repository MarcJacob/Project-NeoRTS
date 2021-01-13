#if UNITY_STANDALONE
#endif

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_HEALTH : IEquatable<OBJECT_DATA_HEALTH>
            {
                public int HP;

                public bool Equals(OBJECT_DATA_HEALTH other)
                {
                    return HP == other.HP;
                }
            }
        }
    }
}

