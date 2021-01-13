using NeoRTS.Communication;
using NeoRTS.Communication.Messages;
using NeoRTS.GameData.ObjectData;
using NeoRTS.GameData.ObjectData.Types;
using NeoRTS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace NeoRTS
{
    namespace GameData
    {
        public interface IObjectDataContainersHolder
        {
            ObjectDataTypeMemoryContainer<T> GetDataContainer<T>() where T : unmanaged;
        }

        /// <summary>
        /// Manages the memory used to store data about game "objects" (units, buildings, projectiles...).
        /// To get to a specific collection of data, use <see cref="GetDataContainer{T}"/>.
        /// 
        /// All memory is managed through <see cref="ObjectDataTypeMemoryContainer{T}"/>.
        /// 
        /// On top of simply managing the memory, this manager also runs a event system, namely the
        /// DataChangeEvent system. It automatically is able to determine which data type (and thus collection) the event
        /// acts on. If the manager is in Authoritative mode, anytime an event is "accepted" by the collection, it is automatically
        /// broadcast as a message. In the case of it running on a server that means it gets broadcast to every client in the match.
        /// </summary>
        public class ObjectMemoryManager : MatchManagerObject, IObjectDataContainersHolder
        {
            #region Object Data Type & Collection ID Management

            #endregion


#region Data Structures

            public struct OBJECT_SPAWN_DATA
            {
                public Position startPosition;
                public int owner;
                public int objectTypeID;
            }

            private const int MAX_OBJECT_DATA_TABLE_SLOTS = 16;

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct OBJECT
            {
                public bool alive;
                public fixed uint containerMemoryIDs[MAX_OBJECT_DATA_TABLE_SLOTS];
                public fixed byte containerTypeIDs[MAX_OBJECT_DATA_TABLE_SLOTS];

                public void KillAndClearMemory()
                {
                    for(int i = 0; i < MAX_OBJECT_DATA_TABLE_SLOTS; i++)
                    {
                        containerTypeIDs[i] = byte.MaxValue;
                        containerMemoryIDs[i] = uint.MaxValue;
                    }
                    alive = false;
                }
                public void RegisterDataTableSlot<T>(uint slotID) where T : unmanaged
                {
                    byte typeID = ObjectDataTypeDatabase.GetIDFromType<T>();
                    RegisterDataTableSlot(typeID, slotID);
                }

                public void RegisterDataTableSlot(Type type, uint slotID)
                {
                    byte typeID = ObjectDataTypeDatabase.GetIDFromType(type);
                    RegisterDataTableSlot(typeID, slotID);
                }

                public void RegisterDataTableSlot(byte typeID, uint slotID)
                {
                    int tableSlotID = 0;
                    while (tableSlotID < MAX_OBJECT_DATA_TABLE_SLOTS && containerTypeIDs[tableSlotID] != byte.MaxValue)
                    {
                        tableSlotID++;
                    }

                    if (tableSlotID == MAX_OBJECT_DATA_TABLE_SLOTS)
                    {
                        throw new OutOfMemoryException();
                    }

                    containerTypeIDs[tableSlotID] = typeID;
                    containerMemoryIDs[tableSlotID] = slotID;
                }

                public uint GetDataSlotIDForType<T>() where T : unmanaged
                {
                    return GetDataSlotIDForType(ObjectDataTypeDatabase.GetIDFromType<T>());
                }

                public uint GetDataSlotIDForType(uint typeID)
                {
                    for (int i = 0; i < MAX_OBJECT_DATA_TABLE_SLOTS; i++)
                    {
                        if (containerTypeIDs[i] == typeID)
                        {
                            return containerMemoryIDs[i];
                        }
                    }
                    return uint.MaxValue;
                }
            }
            
#endregion

#region Properties
            public ReadOnlyCollection<OBJECT> Objects
            {
                get
                {
                    return Array.AsReadOnly(m_objects);
                }
            }
            public uint MAX_UNIT_COUNT { get; private set; }

#region Request Queues
            public Queue<uint> ObjectDestroyRequestQueue
            {
                get
                {
                    return m_objectDestructionRequests;
                }
            }
            public Queue<OBJECT_SPAWN_DATA> ObjectSpawnRequestQueue
            {
                get
                {
                    return m_objectSpawnRequests;
                }
            }
            #endregion

            #endregion

            private OBJECT[] m_objects;
            private Dictionary<byte, ObjectDataTypeMemoryContainer_Base> m_idToObjectDataContainerDictionary;
            private Dictionary<Type, ObjectDataTypeMemoryContainer_Base> m_typeToObjectDataContainerDictionary;

#region Request & Event Queues & Event Callbacks

            private Queue<uint> m_objectDestructionRequests;
            private Queue<OBJECT_SPAWN_DATA> m_objectSpawnRequests;

            public event Action<uint, OBJECT_SPAWN_DATA> OnObjectSpawned = delegate { };
            public event Action<uint> OnObjectDestroyed = delegate { };
            #endregion

            #region Object Data Management

            public void AddDataToObject<T>(uint objectID, T data) where T : unmanaged
            {
                uint slotID = GetDataContainer<T>().AllocateSlotWithData(data);
                m_objects[objectID].RegisterDataTableSlot<T>(slotID);
            }

            public unsafe void AddDataToObject(uint objectID, Type dataType, object data)
            {
                uint slotID = GetDataContainer(dataType).AllocateSlotWithData(data);
                m_objects[objectID].RegisterDataTableSlot(dataType, slotID);
            }

            #endregion

            #region Unit Lifecycle Management
            public bool IsUnitAlive(uint id)
            {
                if (id >= MAX_UNIT_COUNT) throw new System.Exception("ERROR : Passed ID is not valid as it is outside the MAX_UNIT_COUNT bound !");

                return m_objects[id].alive;
            }
            public bool SpawnObject(out uint spawnedID)
            {
                spawnedID = 0;
                while (spawnedID < MAX_UNIT_COUNT && m_objects[spawnedID].alive == true)
                {
                    spawnedID++;
                }

                return SpawnObjectWithID(spawnedID);
            }

            /// <summary>
            /// Spawns a specific type of object (Unit) using UNIT_SPAWN_DATA. Returns whether the spawning was
            /// successful (if it's not it means we're out of memory). Returns the unit's ID as the out parameter.
            /// </summary>
            public bool SpawnObjectWithArchetype(out uint spawnedID, ObjectArchetype archetype)
            {
                return SpawnObjectWithDataTypes(out spawnedID, archetype.ArchetypeDataDefs);
            }

            public bool SpawnObjectWithDataTypes(out uint spawnedID, IEnumerable<ObjectDataTypeDef> typeDefs)
            {
                if (SpawnObject(out spawnedID))
                {
                    foreach (var def in typeDefs)
                    {
                        AddDataToObject(spawnedID, def.DataType, def.DefaultValue);
                    }
                    return true;
                }
                return false;
            }

            public bool SpawnObjectWithObjectType(out uint spawnedID, ObjectType objectType)
            {
                if (SpawnObjectWithDataTypes(out spawnedID, objectType.DataTypeDefs))
                {
                    if (m_objects[spawnedID].GetDataSlotIDForType<OBJECT_DATA_OBJECT_TYPE>() == uint.MaxValue)
                    {
                        AddDataToObject(spawnedID, new OBJECT_DATA_OBJECT_TYPE()
                        {
                            objectTypeID = ObjectDataTypeDatabase.GetIDFromObjectType(objectType)
                        });
                    }
                    else
                    {
                        Debug.LogWarning("Warning - Do not add the OBJECT_DATA_OBJECT_TYPE data type to object types. It gets added automatically.");
                    }

                    return true;
                }
                return false;
            }

            public bool SpawnObjectWithID(uint spawnedID)
            {
                if (spawnedID < MAX_UNIT_COUNT)
                {
                    m_objects[spawnedID].alive = true;
                    return true;
                }
                else
                {
                    Debug.LogWarning("WARNING : Unit memory is full ! Not a single available ID was found when trying to spawn a new one. Make sure units are still dying properly or consider increasing the Unit memory max unit count.");
                    return false;
                }
            }

            public void DestroyObject(uint id)
            {
                if (id >= MAX_UNIT_COUNT) throw new System.Exception("ERROR : Passed ID is not valid as it is outside the MAX_UNIT_COUNT bound !");

                if (m_objects[id].alive == false)
                {
                    Debug.LogWarning("WARNING : Attempted to kill a unit that was already dead. Its data remains unchanged.");
                    return;
                }

                unsafe
                {
                    for (int i = 0; i < MAX_OBJECT_DATA_TABLE_SLOTS && m_objects[id].containerTypeIDs[i] != byte.MaxValue; i++)
                    {
                        byte typeID = m_objects[id].containerTypeIDs[i];
                        uint slotID = m_objects[id].containerMemoryIDs[i];

                        m_idToObjectDataContainerDictionary[typeID].ClearSlot(slotID);
                    }
                }


                m_objects[id].KillAndClearMemory();
            }
#endregion

#region Data Change Event System

            private ObjectDataChangeEventMessagePacker m_objectDataChangeEventMessagePacker;
            private Stack<OBJECT_DATA_CHANGE_EVENT_DATA> m_eventStack;

#endregion

            public ObjectDataTypeMemoryContainer<T> GetDataContainer<T>() where T : unmanaged
            {
                return (ObjectDataTypeMemoryContainer<T>)m_typeToObjectDataContainerDictionary[typeof(T)];
            }

            public ObjectDataTypeMemoryContainer_Base GetDataContainer(Type dataType)
            {
                return m_typeToObjectDataContainerDictionary[dataType];
            }

            public ObjectMemoryManager(uint maxUnitCount, bool authoritative) : base(authoritative)
            {
                // Initialize memory UNIT_DATA structure types here.

                MAX_UNIT_COUNT = maxUnitCount;
                m_objects = new OBJECT[maxUnitCount];

                // Make sure that the object's datatable memory is fully cleared.
                for (int i = 0; i < maxUnitCount; i++) m_objects[i].KillAndClearMemory();

                m_objectDestructionRequests = new Queue<uint>(10);
                m_objectSpawnRequests = new Queue<OBJECT_SPAWN_DATA>(10);

                m_objectDataChangeEventMessagePacker = new ObjectDataChangeEventMessagePacker();

                // Build data type containers

                m_idToObjectDataContainerDictionary = new Dictionary<byte, ObjectDataTypeMemoryContainer_Base>();
                m_typeToObjectDataContainerDictionary = new Dictionary<Type, ObjectDataTypeMemoryContainer_Base>();

                for (int i = 0; i < ObjectDataTypeDatabase.AllObjectDataTypes.Length; i++)
                {
                    Type type = ObjectDataTypeDatabase.AllObjectDataTypes[i];
                    byte typeID = ObjectDataTypeDatabase.GetIDFromType(type);

                    ObjectDataTypeMemoryContainer_Base newContainer;

                    var containerType = typeof(ObjectDataTypeMemoryContainer<>);
                    var templateType = new Type[] { type };
                    var templatedContainerType = containerType.MakeGenericType(templateType);

                    newContainer = (ObjectDataTypeMemoryContainer_Base)Activator.CreateInstance(templatedContainerType, maxUnitCount);
                    m_idToObjectDataContainerDictionary.Add(typeID, newContainer);
                    m_typeToObjectDataContainerDictionary.Add(type, newContainer);
                }
            }

            protected override void OnManagerUpdate(float deltaTime)
            {

                // Process Death Requests
                while (m_objectDestructionRequests.Count > 0)
                {
                    uint id = m_objectDestructionRequests.Dequeue();
                    DestroyObject(id);
                    OnObjectDestroyed(id);
                }

                // Process Spawn Requests
                while (m_objectSpawnRequests.Count > 0)
                {
                    var spawnData = m_objectSpawnRequests.Dequeue();
                    uint id = 0;
                    
                    SpawnObjectWithObjectType(out id, ObjectDataTypeDatabase.AllObjectTypes[spawnData.objectTypeID]);
                    uint transformID = m_objects[id].GetDataSlotIDForType<OBJECT_DATA_TRANSFORM>();
                    uint ownerID = m_objects[id].GetDataSlotIDForType<OBJECT_DATA_OWNER>();

                    GetDataContainer<OBJECT_DATA_TRANSFORM>().Data[transformID].position = spawnData.startPosition;
                    GetDataContainer<OBJECT_DATA_OWNER>().Data[ownerID].ownerID = spawnData.owner;

                    OnObjectSpawned(id, spawnData);
                }


            }

            protected override void OnManagerInitialize()
            {

            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.OBJECT_DATA_CHANGE_EVENT, OnObjectDataChangeEventMessageReceived);
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.UnregisterOnMessageReceivedHandler(OnObjectDataChangeEventMessageReceived);

            }

            private void OnObjectDataChangeEventMessageReceived(MESSAGE message)
            {
                var eventObject = m_objectDataChangeEventMessagePacker.UnpackMessage(message);

                byte collectionID = eventObject.eventMetadata.eventDataTypeID;

                // Generate list of affected slot IDs for this container.
                List<uint> slotIDs = new List<uint>();

                foreach(var objectID in eventObject.objectIDs)
                {
                    uint slotID = m_objects[objectID].GetDataSlotIDForType(collectionID);
                    if (slotID != uint.MaxValue)
                    {
                        slotIDs.Add(slotID);
                    }
                    else
                    {
                        Debug.LogWarning("WARNING - Data change event tried affecting object " + objectID + " that does not possess the data component " + ObjectDataTypeDatabase.AllObjectDataTypes[collectionID].Name);
                    }
                }
                
                if(m_idToObjectDataContainerDictionary[collectionID].ProcessEventMessage(eventObject, slotIDs) && Authoritative)
                {
                    StageMessageForSending(m_objectDataChangeEventMessagePacker.PackMessage(eventObject));
                }
            }

        }
    }
}

