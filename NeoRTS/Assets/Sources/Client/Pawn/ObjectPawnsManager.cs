using NeoRTS.Client.GameData;
using NeoRTS.Client.UI;
using NeoRTS.Communication;
using NeoRTS.GameData;
using NeoRTS.GameData.Actors;
using NeoRTS.GameData.Matches;
using NeoRTS.GameData.ObjectData;
using NeoRTS.GameData.ObjectData.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace Pawns
        {
            /// <summary>
            /// Manages all Pawns in existence. Reacts to EVENT_SPAWNED and EVENT_DESTROYED events by using them
            /// to spawn new Pawn objects as needed and maintain a ID -> Pawn lookup table.
            /// For Pawns that currently exist, it also dispatches all events directly to them.
            /// 
            /// (TEMPORARY) For now also handles loading all all Pawn Types.
            /// TODO : Consider moving the loading of Pawn types to a Resource Manager.
            /// </summary>
            public class ObjectPawnsManager : ManagerObject
            {
                // SINGLETON
                static public ObjectPawnsManager Instance { get; private set; }

                // STATIC RESOURCES HANDLING
                static private Dictionary<Type, ObjectPawnComponent> ObjectPawnTypeToObjectPawnComponentDictionary;

                static private DatabaseObjectReader<Actor> ActorDatabaseReader;
                static private DatabaseObjectReader<ObjectPawnComponent> PawnDatabaseReader;

                static private void LoadDataFromDBObjects()
                {
                    var databaseObjects = Resources.LoadAll<GameDataDatabaseObject>("GameData DB");

                    ActorDatabaseReader = DatabaseObjectReader<Actor>.FindDBAndCreateReader(databaseObjects);
                    PawnDatabaseReader = DatabaseObjectReader<ObjectPawnComponent>.FindDBAndCreateReader(databaseObjects);

                    if (ActorDatabaseReader == null || PawnDatabaseReader == null)
                    {
                        throw new Exception("Error : Some required database type(s) is (are) absent.");
                    }

                    ObjectPawnTypeToObjectPawnComponentDictionary = new Dictionary<Type, ObjectPawnComponent>();
                    foreach(var pawn in PawnDatabaseReader.GetAllElements())
                    {
                        ObjectPawnTypeToObjectPawnComponentDictionary.Add(pawn.GetType(), pawn);
                    }
                }
                static private T GetPawnComponentType<T>() where T : ObjectPawnComponent
                {
                    return (T)ObjectPawnTypeToObjectPawnComponentDictionary[typeof(T)];
                }

                private ObjectMemoryManager m_objectMemoryManager;
                private Dictionary<uint, ObjectPawnComponent> m_objectIDToObjectPawnDictionary;

                public int LocalPlayerID
                {
                    get; set;
                }

                public ObjectPawnsManager()
                {
                    m_objectIDToObjectPawnDictionary = new Dictionary<uint, ObjectPawnComponent>();
                    Instance = this;
                }

                public void LinkToMatchData(ObjectMemoryManager memManager)
                {
                    m_objectMemoryManager = memManager;
                    m_objectMemoryManager.OnObjectSpawned += OnObjectSpawned;
                    m_objectMemoryManager.OnObjectDestroyed += OnObjectDestruction;
                }
                public ObjectPawnComponent GetPawnFromObjectID(uint id)
                {
                    if (m_objectIDToObjectPawnDictionary.ContainsKey(id))
                    {
                        return m_objectIDToObjectPawnDictionary[id];
                    }
                    else return null;
                }

                public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
                {

                }

                public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
                {

                }

                protected override void OnManagerInitialize()
                {
                    // TODO : Should probably move that and the PAWN resource management to a dedicated Resource Manager.
                    if (ObjectPawnTypeToObjectPawnComponentDictionary == null)
                    {
                        ObjectPawnTypeToObjectPawnComponentDictionary = new Dictionary<Type, ObjectPawnComponent>();
                        LoadDataFromDBObjects();
                    }
                }

                protected override void OnManagerUpdate(float deltaTime)
                {
                    foreach(var keyValuePair in m_objectIDToObjectPawnDictionary)
                    {
                        keyValuePair.Value.UpdatePawn();
                    }
                }

                private void OnObjectSpawned(uint objectID, ObjectMemoryManager.OBJECT_SPAWN_DATA spawnData)
                {
                    int objectTypeID = -1;

                    uint slotID = m_objectMemoryManager.Objects[(int)objectID].GetDataSlotIDForType<OBJECT_DATA_OBJECT_TYPE>();
                    if (slotID != uint.MaxValue)
                    {
                        objectTypeID = m_objectMemoryManager.GetDataContainer<OBJECT_DATA_OBJECT_TYPE>().Data[slotID].objectTypeID;
                    }

                    var chosenPawnType = ChoosePawnType(objectID, objectTypeID);
                    var actor = ChoosePawnActor(objectID, objectTypeID);
                    var spawned = GameObject.Instantiate(chosenPawnType).GetComponent<ObjectPawnComponent>();
                    spawned.LinkToGameData(m_objectMemoryManager, objectID);

                    // Spawn pawn UI components
                    var uiManager = GameClient.Instance.GetManager<UIManager>();
                    for (int i = 0; i < spawned.pawnUIModules.Length; i++)
                    {
                        spawned.pawnUIModules[i] = (ObjectPawnUIModule)uiManager.GetUIModule(spawned.pawnUIModules[i].name);
                    }

                    spawned.InitializePawnUIModules();

                    spawned.AssignActor(actor);

                    m_objectIDToObjectPawnDictionary.Add(objectID, spawned);
                }

                private void OnObjectDestruction(uint id)
                {
                    Debug.Log("Pawn Destroyed");
                    var targetPawn = m_objectIDToObjectPawnDictionary[id];
                    m_objectIDToObjectPawnDictionary.Remove(id);
                    targetPawn.Kill();
                }
                private ObjectPawnComponent ChoosePawnType(uint id, int objectTypeID)
                {
                    if (objectTypeID == -1)
                    {
                        return GetPawnComponentType<UnitPawnComponent>();
                    }
                    else
                    {
                        return PawnDatabaseReader.GetElementFromID(ObjectDataTypeDatabase.AllObjectTypes[objectTypeID].PawnTypeID);
                    }
                }

                private Actor ChoosePawnActor(uint id, int objectTypeID)
                {
                    if (objectTypeID == -1)
                    {
                        return null;
                    }
                    else
                    {
                        return ActorDatabaseReader.GetElementFromID(ObjectDataTypeDatabase.AllObjectTypes[objectTypeID].ActorID);
                    }
                }

            }
        }
    }
}


