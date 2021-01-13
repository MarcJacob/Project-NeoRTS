using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_TRANSFORM
            {
                public Position position;
            }
        }
    }
}

