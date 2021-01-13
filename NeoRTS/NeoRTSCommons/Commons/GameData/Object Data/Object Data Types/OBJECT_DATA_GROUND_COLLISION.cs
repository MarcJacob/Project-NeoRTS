#if UNITY_STANDALONE
#endif


namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            public struct OBJECT_DATA_GROUND_COLLISION
            {
                public float collisionSize;
                public bool allowPushback;
            }
        }
    }
}

