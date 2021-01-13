#if UNITY_STANDALONE
#endif


namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            public struct OBJECT_DATA_PERIODIC_SPAWNER
            {
                [ObjectTypeReference]
                public int spawnedObjectTypeID;

                public float spawnClock;
                public float spawnCooldown;
            }
        }
    }
}

