using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            public struct OBJECT_DATA_CHANGE_EVENT_METADATA
            {
                public byte eventDataTypeID;
                public float secondsSinceGameStart;
                public int originatingPlayerID;
            }
            public struct OBJECT_DATA_CHANGE_EVENT_DATA
            {
                public OBJECT_DATA_CHANGE_EVENT_METADATA eventMetadata;
                public uint[] objectIDs;
                public byte[] data;
            }

            public static class DataChangeEventFactory
            {
                static unsafe public OBJECT_DATA_CHANGE_EVENT_DATA BuildDataChangeEventData<T>(T data, uint[] objectIDs, float timeStamp) where T : unmanaged
                {
                    OBJECT_DATA_CHANGE_EVENT_DATA eventData = new OBJECT_DATA_CHANGE_EVENT_DATA();

                    eventData.objectIDs = objectIDs;
                    eventData.eventMetadata.secondsSinceGameStart = timeStamp;

                    eventData.data = new byte[sizeof(T)];

                    fixed (byte* dataPtr = eventData.data)
                    {
                        Buffer.MemoryCopy(&data, dataPtr, sizeof(T), sizeof(T));
                    }

                    eventData.eventMetadata.eventDataTypeID = ObjectDataTypeDatabase.GetIDFromType<T>();
                    return eventData;
                }
            }
        }
    }
}

