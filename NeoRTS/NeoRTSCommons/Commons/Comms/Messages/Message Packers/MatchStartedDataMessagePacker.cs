using NeoRTS.GameData;
using System;
using static NeoRTS.GameData.Matches.Match;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            public class MatchStartedDataMessagePacker : MessagePacker<MATCH_STARTED_MESSAGE_DATA>
            {
                public MatchStartedDataMessagePacker() : base(MESSAGE_TYPE.MATCH_STARTED)
                {

                }

                protected override unsafe void PackData(MATCH_STARTED_MESSAGE_DATA data, byte* packInto, int byteCountForData)
                {
                    int bytesForLocalPlayerID = sizeof(int);
                    int* localPlayerIDPtr = &data.localPlayerID;

                    int bytesForLocalMatchBool = sizeof(bool);
                    bool* localMatchBoolPtr = &data.localMatch;

                    int bytesForMatchMetadata = sizeof(MATCH_METADATA);
                    MATCH_METADATA* matchMetadataPtr = &data.matchStartData.metadata;

                    int bytesForUnitCount = sizeof(int);
                    int unitCount = data.matchStartData.startUnits.Length;
                    int* unitCountPtr = &unitCount;

                    // TODO : Pass in the player names.

                    int bytesForUnits = sizeof(ObjectMemoryManager.OBJECT_SPAWN_DATA) * unitCount;
                    fixed(ObjectMemoryManager.OBJECT_SPAWN_DATA* unitsPtr = data.matchStartData.startUnits)
                    {
                        int totalBytesEncoded = 0;
                        
                        Buffer.MemoryCopy(localPlayerIDPtr, packInto + totalBytesEncoded, byteCountForData - totalBytesEncoded, bytesForLocalPlayerID);
                        totalBytesEncoded += bytesForLocalPlayerID;
                        
                        Buffer.MemoryCopy(localMatchBoolPtr, packInto + totalBytesEncoded, byteCountForData - totalBytesEncoded, bytesForLocalMatchBool);
                        totalBytesEncoded += bytesForLocalMatchBool;
                        
                        Buffer.MemoryCopy(matchMetadataPtr, packInto + totalBytesEncoded, byteCountForData - totalBytesEncoded, bytesForMatchMetadata);
                        totalBytesEncoded += bytesForMatchMetadata;

                        Buffer.MemoryCopy(unitCountPtr, packInto + totalBytesEncoded, byteCountForData - totalBytesEncoded, bytesForUnitCount);
                        totalBytesEncoded += bytesForUnitCount;

                        Buffer.MemoryCopy(unitsPtr, packInto + totalBytesEncoded, byteCountForData - totalBytesEncoded, bytesForUnits);
                        totalBytesEncoded += bytesForUnits;
                    }
                }

                protected override unsafe MATCH_STARTED_MESSAGE_DATA UnpackData(byte* dataPtr, int dataByteSize)
                {
                    MATCH_STARTED_MESSAGE_DATA data = new MATCH_STARTED_MESSAGE_DATA();

                    int bytesForLocalPlayerID = sizeof(int);
                    int* localPlayerIDPtr = &data.localPlayerID;

                    int bytesForLocalMatchBool = sizeof(bool);
                    bool* localMatchBoolPtr = &data.localMatch;

                    int bytesForMatchMetadata = sizeof(MATCH_METADATA);
                    MATCH_METADATA* matchMetadataPtr = &data.matchStartData.metadata;

                    int bytesForUnitCount = sizeof(int);
                    int unitCount = 0;
                    int* unitCountPtr = &unitCount;

                    int totalBytesDecoded = 0;

                    Buffer.MemoryCopy(dataPtr + totalBytesDecoded, localPlayerIDPtr, bytesForLocalPlayerID, bytesForLocalPlayerID);
                    totalBytesDecoded += bytesForLocalPlayerID;

                    Buffer.MemoryCopy(dataPtr + totalBytesDecoded, localMatchBoolPtr, bytesForLocalMatchBool, bytesForLocalMatchBool);
                    totalBytesDecoded += bytesForLocalMatchBool;

                    Buffer.MemoryCopy(dataPtr + totalBytesDecoded, matchMetadataPtr, bytesForMatchMetadata, bytesForMatchMetadata);
                    totalBytesDecoded += bytesForMatchMetadata;

                    Buffer.MemoryCopy(dataPtr + totalBytesDecoded, unitCountPtr, bytesForUnitCount, bytesForUnitCount);
                    totalBytesDecoded += bytesForUnitCount;

                    data.matchStartData.startUnits = new ObjectMemoryManager.OBJECT_SPAWN_DATA[unitCount];
                    fixed(ObjectMemoryManager.OBJECT_SPAWN_DATA* unitsPtr = data.matchStartData.startUnits)
                    {
                        int bytesForUnits = sizeof(ObjectMemoryManager.OBJECT_SPAWN_DATA) * unitCount;
                        Buffer.MemoryCopy(dataPtr + totalBytesDecoded, unitsPtr, bytesForUnits, bytesForUnits);
                        totalBytesDecoded += bytesForUnits;
                    }


                    return data;
                }
            }
        }
    }
}

