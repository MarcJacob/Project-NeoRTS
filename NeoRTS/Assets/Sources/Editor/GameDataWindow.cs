using NeoRTS.GameData.Tools;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using NeoRTS.GameData.Resources;
using System;
using NeoRTS.GameData.Actors;
using NeoRTS.Client.Pawns;
using System.Text;
using System.Diagnostics;

namespace NeoRTS
{
    namespace EditorTools
    {

        public class GameDataWindow : EditorWindow
        {
            static private string m_actorsFolder = "";
            public const string ACTOR_FOLDER_PATH_PLAYER_PREF_NAME = "EDITOR_GAMEDATA_ACTORS_FOLDER_PATH";

            static private string m_pawnTypesFolder = "";
            public const string PAWNS_FOLDER_PATH_PLAYER_PREF_NAME = "EDITOR_GAMEDATA_PAWNS_FOLDER_PATH";

            static private List<string> m_gameDataExportTargetFolders = new List<string>();
            public const string GAMEDATA_EXPORT_TARGET_FOLDERS = "EDITOR_GAMEDATA_EXPORT_TARGET_FOLDERS";

            public const string CLIENT_GAME_DATA_PATH = "/Resources/GameData DB";

            [MenuItem("Tools/Neo-RTS/Game Data")]
            static void OpenGameDataWindow()
            {
                EditorWindow.GetWindow(typeof(GameDataWindow), true, "Game Data");
            }

            private ObjectArchetypeDefinition[] allArchetypes;
            private ObjectTypeDefinition[] allObjectTypes;

            private void LoadObjectDataAssets()
            {
                List<ObjectArchetypeDefinition> archetypes = new List<ObjectArchetypeDefinition>();
                List<ObjectTypeDefinition> objectTypes = new List<ObjectTypeDefinition>();
                IEnumerable<string> archetypeFiles = Directory.GetFiles(Application.dataPath + "/Bulk/Game Data Building/Object Archetype Defs/").Where(fileName => fileName.EndsWith(".meta") == false);
                IEnumerable<string> objectTypeFiles = Directory.GetFiles(Application.dataPath + "/Bulk/Game Data Building/Object Types/").Where(fileName => fileName.EndsWith(".meta") == false);

                string dataPathNoAssetFolder = Application.dataPath.Replace("/Assets", "");
                
                foreach(var file in archetypeFiles)
                {
                    string fileTrimmed = file.Replace(dataPathNoAssetFolder + "/", "");
                    archetypes.Add(AssetDatabase.LoadAssetAtPath<ObjectArchetypeDefinition>(fileTrimmed));
                }

                foreach (var file in objectTypeFiles)
                {
                    string fileTrimmed = file.Replace(dataPathNoAssetFolder + "/", "");
                    objectTypes.Add(AssetDatabase.LoadAssetAtPath<ObjectTypeDefinition>(fileTrimmed));
                }

                allArchetypes = archetypes.ToArray();
                allObjectTypes = objectTypes.ToArray();
            }

            private void LoadClientGameDataAssets()
            {
                ClientGameDataEditorDatabase<Actor>.Refresh(m_actorsFolder);
                ClientGameDataEditorDatabase<ObjectPawnComponent>.Refresh(m_pawnTypesFolder);
            }

            private Vector2 m_archetypeListScrollPos;
            private Vector2 m_objectTypeListScrollPos;
            private Vector2 m_exportTargetFoldersScrollPos;

            private void OnEnable()
            {
                if (m_actorsFolder == "" && PlayerPrefs.HasKey(ACTOR_FOLDER_PATH_PLAYER_PREF_NAME))
                {
                    m_actorsFolder = PlayerPrefs.GetString(ACTOR_FOLDER_PATH_PLAYER_PREF_NAME);
                    m_pawnTypesFolder = PlayerPrefs.GetString(PAWNS_FOLDER_PATH_PLAYER_PREF_NAME);

                    string playerPrefString = PlayerPrefs.GetString(GAMEDATA_EXPORT_TARGET_FOLDERS);
                    var splitResult = playerPrefString.Split('|');
                    foreach(var result in splitResult)
                    {
                        m_gameDataExportTargetFolders.Add(result);
                    }
                }
            }

            void OnGUI()
            {
                LoadObjectDataAssets();
                LoadClientGameDataAssets();
                GUIStyle titleStyle = new GUIStyle();
                titleStyle.fontSize = 24;
                titleStyle.alignment = TextAnchor.MiddleCenter;

                GUIStyle gameDataListStyle = new GUIStyle();
                gameDataListStyle.padding.left = 50;
                gameDataListStyle.border.left = 5;
                gameDataListStyle.border.right = 5;
                gameDataListStyle.border.top = 5;
                gameDataListStyle.border.bottom = 5;
                gameDataListStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bulk/Editor/NeoRTS_TOOL_LIST_BACKGROUND_TEX.png");

                GUILayout.Space(50);
                GUILayout.BeginHorizontal();

                GUILayout.Label(m_actorsFolder.Length > 0 ? m_actorsFolder : "<ACTORS FOLDER PATH>");
                if (GUILayout.Button("..."))
                {
                    m_actorsFolder = EditorUtility.OpenFolderPanel("Actors folder", m_actorsFolder.Length > 0 ? m_actorsFolder : Application.dataPath, "");
                    PlayerPrefs.SetString(ACTOR_FOLDER_PATH_PLAYER_PREF_NAME, m_actorsFolder);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.Label(m_pawnTypesFolder.Length > 0 ? m_pawnTypesFolder : "<PAWN TYPES FOLDER PATH>");
                if (GUILayout.Button("..."))
                {
                    m_pawnTypesFolder = EditorUtility.OpenFolderPanel("Actors folder", m_pawnTypesFolder.Length > 0 ? m_pawnTypesFolder : Application.dataPath, "");
                    PlayerPrefs.SetString(PAWNS_FOLDER_PATH_PLAYER_PREF_NAME, m_pawnTypesFolder);
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(50);

                if (GUILayout.Button("Export Objects Data"))
                {
                    OnExportObjectData();
                }

                m_exportTargetFoldersScrollPos = EditorGUILayout.BeginScrollView(m_exportTargetFoldersScrollPos, gameDataListStyle, GUILayout.ExpandHeight(true));

                for (int targetFolderID = 0; targetFolderID < m_gameDataExportTargetFolders.Count; targetFolderID++)
                {
                    string targetFolder = (string)m_gameDataExportTargetFolders[targetFolderID];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(targetFolder);

                    if (GUILayout.Button("-"))
                    {
                        m_gameDataExportTargetFolders.RemoveAt(targetFolderID);
                        targetFolderID--;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Target Folder"))
                {
                    string path = EditorUtility.SaveFolderPanel("Export Target", Application.dataPath, "");
                    if (path.Length > 0)
                    {
                        m_gameDataExportTargetFolders.Add(path);
                    }

                    StringBuilder playerPrefString = new StringBuilder();
                    for (int i = 0; i < m_gameDataExportTargetFolders.Count; i++)
                    {
                        string target = m_gameDataExportTargetFolders[i];

                        if (i < m_gameDataExportTargetFolders.Count - 1)
                            playerPrefString.Append(target + "|");
                        else
                            playerPrefString.Append(target);
                    }

                    PlayerPrefs.SetString(GAMEDATA_EXPORT_TARGET_FOLDERS, playerPrefString.ToString());
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField("Object Archetypes", titleStyle);

                m_archetypeListScrollPos = GUILayout.BeginScrollView(m_archetypeListScrollPos, gameDataListStyle, GUILayout.ExpandHeight(true));

                foreach(var archetype in allArchetypes)
                {
                    GUILayout.Label(archetype.name);
                }

                GUILayout.EndScrollView();

                GUILayout.Space(50);
                EditorGUILayout.LabelField("Object Types", titleStyle);


                m_objectTypeListScrollPos = GUILayout.BeginScrollView(m_objectTypeListScrollPos, gameDataListStyle, GUILayout.ExpandHeight(true));

                foreach (var objectType in allObjectTypes)
                {
                    GUILayout.Label(objectType.DisplayName + " (" + objectType.name + ")");
                }

                GUILayout.EndScrollView();

                if (GUILayout.Button("Output Client Data DB Objects"))
                {
                    OnOutputClientGameData();
                }
            }

            private void OnExportObjectData()
            {
                string fileName = "Objects.neoData";

                OBJECT_TYPES_FILE_DATA objectTypesFileData;
                ARCHETYPE_FILE_DATA archetypesFileData;

                // ARCHETYPES EXPORT
                {
                    archetypesFileData = new ARCHETYPE_FILE_DATA();
                    archetypesFileData.archetypes = new List<ARCHETYPE_FILE_DATA.ARCHETYPE_DATA>();
                    archetypesFileData.archetypeCount = allArchetypes.Length;
                    foreach (var archetype in allArchetypes)
                    {
                        ARCHETYPE_FILE_DATA.ARCHETYPE_DATA fileArchetype = new ARCHETYPE_FILE_DATA.ARCHETYPE_DATA();
                        var archetypeData = archetype.GenerateDeserializedData();

                        fileArchetype.dataDefsCount = archetypeData.dataDefList.Count;
                        fileArchetype.dataDefs = new List<OBJECT_DATA_TYPE_DEF_FILE_DATA>();
                        foreach (var def in archetypeData.dataDefList)
                        {
                            OBJECT_DATA_TYPE_DEF_FILE_DATA objectDataTypeDef = GenerateDataTypeDefGroupFileData(def);

                            fileArchetype.dataDefs.Add(objectDataTypeDef);
                        }

                        archetypesFileData.archetypes.Add(fileArchetype);
                    }
                }

                // OBJECT TYPES EXPORT
                {
                    objectTypesFileData = new OBJECT_TYPES_FILE_DATA();
                    objectTypesFileData.objectTypes = new List<OBJECT_TYPES_FILE_DATA.OBJECT_TYPE_DATA>();
                    objectTypesFileData.objectTypeCount = allObjectTypes.Length;
                    foreach (var objectType in allObjectTypes)
                    {
                        OBJECT_TYPES_FILE_DATA.OBJECT_TYPE_DATA fileObjectType = new OBJECT_TYPES_FILE_DATA.OBJECT_TYPE_DATA();
                        var objectTypeDataDefs = objectType.GenerateDeserializedData();
                        int archetypeID, actorID, pawnTypeID;
                        int nameLength;
                        char[] name;

                        // IDs resolve
                        {
                            // Archetype ID
                            {
                                int archID;
                                archetypeID = -1;
                                for (archID = 0; archID < allArchetypes.Length; archID++)
                                {
                                    ObjectArchetypeDefinition arch = allArchetypes[archID];
                                    if (arch == objectType.ArchetypeDef)
                                    {
                                        archetypeID = archID;
                                        break;
                                    }
                                }
                                if (archID == allArchetypes.Length)
                                {
                                    throw new Exception("Error : Failed to resolve Archetype ID on Object Type '" + objectType.name + "' !");
                                }

                            }
                            // Actor ID
                            {
                                actorID = ClientGameDataEditorDatabase<Actor>.GetElementID(objectType.Actor);
                            }
                            // Pawn ID
                            {
                                pawnTypeID = ClientGameDataEditorDatabase<ObjectPawnComponent>.GetElementID(objectType.PawnComponent);
                            }
                        }

                        nameLength = objectType.DisplayName.Length;
                        name = objectType.DisplayName.ToCharArray();

                        fileObjectType.nameLength = nameLength;
                        fileObjectType.name = name;
                        fileObjectType.actorID = actorID;
                        fileObjectType.archetypeID = archetypeID;
                        fileObjectType.pawnTypeID = pawnTypeID;

                        fileObjectType.dataDefOverridesCount = objectTypeDataDefs.dataDefList.Count;
                        fileObjectType.dataDefOverrides = new List<OBJECT_DATA_TYPE_DEF_FILE_DATA>();
                        foreach (var def in objectTypeDataDefs.dataDefList)
                        {
                            var objectDataTypeDef = GenerateDataTypeDefGroupFileData(def);

                            fileObjectType.dataDefOverrides.Add(objectDataTypeDef);
                        }

                        objectTypesFileData.objectTypes.Add(fileObjectType);
                    }
                }


                foreach(var target in m_gameDataExportTargetFolders)
                {
                    FileStream file = File.OpenWrite(target + "/" + fileName);
                    if (file != null && file.CanWrite)
                    {
                        archetypesFileData.WriteToStream(file);
                        objectTypesFileData.WriteToStream(file);

                        file.Dispose();
                    }
                }

                Process.Start("Explorer", m_gameDataExportTargetFolders[0].Replace("/", "\\"));

            }

            private OBJECT_DATA_TYPE_DEF_FILE_DATA GenerateDataTypeDefGroupFileData(ObjectDataTypeDefGroup.ObjectDataTypeDef_DESERIALIZED def)
            {
                var objectDataTypeDef = new OBJECT_DATA_TYPE_DEF_FILE_DATA();

                objectDataTypeDef.typeNameLength = def.dataDefType.Name.Length;
                objectDataTypeDef.typeName = def.dataDefType.Name.ToCharArray();
                objectDataTypeDef.fieldCount = def.fields.Count;
                objectDataTypeDef.fields = new List<OBJECT_DATA_TYPE_DEF_FILE_DATA.FIELD>();

                foreach (var field in def.fields)
                {
                    objectDataTypeDef.fields.Add(new OBJECT_DATA_TYPE_DEF_FILE_DATA.FIELD()
                    {
                        fieldNameLength = field.fieldName.Length,
                        fieldName = field.fieldName.ToCharArray(),
                        defaultValueSize = field.fieldDefaultValue.Length,
                        defaultValue = field.fieldDefaultValue
                    });
                }

                foreach(var field in def.gameDataRefFields)
                {
                    int fieldValueSize = sizeof(int);
                    byte[] fieldDefaultValue = null;
                    if (field.fieldRef.GetType() == typeof(ObjectTypeDefinition))
                    {
                        // Find type ID
                        int id = 0;
                        while(id < allObjectTypes.Length && allObjectTypes[id] != field.fieldRef)
                        {
                            id++;
                        }

                        if (id < allObjectTypes.Length)
                        {
                            fieldDefaultValue = BitConverter.GetBytes(id);
                        }
                        else
                        {
                            throw new Exception("Error : Could not resolve ID of Object Type " + field.fieldRef.name);
                        }
                    }
                    else
                    {
                        throw new Exception("Error : Unsupported reference type '" + field.refType.Name + "' !");
                    }

                    objectDataTypeDef.fields.Add(new OBJECT_DATA_TYPE_DEF_FILE_DATA.FIELD()
                    {
                        fieldNameLength = field.fieldName.Length,
                        fieldName = field.fieldName.ToCharArray(),
                        defaultValueSize = fieldValueSize,
                        defaultValue = fieldDefaultValue
                    });
                }

                return objectDataTypeDef;
            }

            private void OnOutputClientGameData()
            {
                ClientGameDataEditorDatabase<Actor>.OutputDataToDatabaseObject(CLIENT_GAME_DATA_PATH);
                ClientGameDataEditorDatabase<ObjectPawnComponent>.OutputDataToDatabaseObject(CLIENT_GAME_DATA_PATH);
            }
        }
    }
}