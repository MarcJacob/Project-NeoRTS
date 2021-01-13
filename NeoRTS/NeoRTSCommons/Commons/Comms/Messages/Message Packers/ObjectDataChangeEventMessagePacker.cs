using NeoRTS.GameData.ObjectData;
using System;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            /// <summary>
            /// Implementation of MessagePacker for structures of type <see cref="OBJECT_DATA_CHANGE_EVENT_DATA{T}"/> where T = DATA_TYPE.
            /// It contains the common encoding and decoding logic of all possible types of UnitDataChangedMessageData.
            /// </summary>
            public class ObjectDataChangeEventMessagePacker : MessagePacker<OBJECT_DATA_CHANGE_EVENT_DATA>
            {
                public ObjectDataChangeEventMessagePacker() : base(MESSAGE_TYPE.OBJECT_DATA_CHANGE_EVENT)
                {

                }

                protected override unsafe void PackData(OBJECT_DATA_CHANGE_EVENT_DATA data, byte* packInto, int byteCountForData)
                {
                    var eventMetadata = data.eventMetadata;
                    int objectCount = data.objectIDs.Length;

                    int bytesForEventMetadata = sizeof(OBJECT_DATA_CHANGE_EVENT_METADATA);
                    int bytesForObjectCount = sizeof(int);
                    int bytesForObjectIDs = data.objectIDs.Length * sizeof(uint);
                    int bytesForData = byteCountForData - bytesForEventMetadata - bytesForObjectCount - bytesForObjectIDs;

                    // Encode the type of data we're changing in this event

                    Buffer.MemoryCopy(&eventMetadata, packInto, byteCountForData, bytesForEventMetadata);
                    byteCountForData -= bytesForEventMetadata;
                    packInto += bytesForEventMetadata;

                    // Encode the amount of units (and thus the length of the UnitIDs array and Data array) as the first data element of the message.
                    Buffer.MemoryCopy(&objectCount, packInto, byteCountForData, bytesForObjectCount);
                    byteCountForData -= bytesForObjectCount;
                    packInto += bytesForObjectCount;

                    fixed (uint* unitIDsPtr = data.objectIDs)
                    {
                        Buffer.MemoryCopy(unitIDsPtr, packInto, byteCountForData, bytesForObjectIDs);
                        byteCountForData -= bytesForObjectIDs;
                        packInto += bytesForObjectIDs;
                    }
                    fixed (byte* dataPtr = data.data)
                    Buffer.MemoryCopy(dataPtr, packInto, byteCountForData, bytesForData);
                }

                protected override unsafe OBJECT_DATA_CHANGE_EVENT_DATA UnpackData(byte* dataPtr, int dataByteSize)
                {
                    OBJECT_DATA_CHANGE_EVENT_METADATA* metadataPtr = (OBJECT_DATA_CHANGE_EVENT_METADATA*)dataPtr;

                    int* objectCountPtr = (int*)(dataPtr + sizeof(OBJECT_DATA_CHANGE_EVENT_METADATA));
                    int objectCount = *objectCountPtr;

                    var objectIDsArray = new uint[objectCount];

                    int bytesForObjectIDs = objectCount * sizeof(uint);
                    int bytesForData = dataByteSize - bytesForObjectIDs - sizeof(OBJECT_DATA_CHANGE_EVENT_METADATA) - sizeof(int);
                    byte[] data = new byte[bytesForData];


                    fixed (uint* ObjectIDsPtr = objectIDsArray)
                    {
                        Buffer.MemoryCopy(dataPtr + sizeof(int) + sizeof(OBJECT_DATA_CHANGE_EVENT_METADATA), ObjectIDsPtr, bytesForObjectIDs, bytesForObjectIDs);
                    }

                    fixed(byte* dataReceivingPtr = data)
                    Buffer.MemoryCopy(dataPtr + sizeof(int) + sizeof(OBJECT_DATA_CHANGE_EVENT_METADATA) + bytesForObjectIDs, dataReceivingPtr, bytesForData, bytesForData);

                    var messageDataStruct = default(OBJECT_DATA_CHANGE_EVENT_DATA);
                    messageDataStruct.objectIDs = objectIDsArray;
                    messageDataStruct.data = data;
                    messageDataStruct.eventMetadata = *metadataPtr;

                    return messageDataStruct;
                }
            }
        }
    }
}

