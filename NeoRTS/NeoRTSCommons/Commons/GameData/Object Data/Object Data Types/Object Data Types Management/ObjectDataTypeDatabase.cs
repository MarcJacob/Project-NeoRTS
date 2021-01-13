using NeoRTS.GameData.ObjectData.Types;
using System;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            static public class ObjectDataTypeDatabase
            {
                // TODO : Have that file path in a config file instead of hardcoding it.
                private const string OBJECT_TYPES_FILE_PATH = "Game Data/Objects.neoData";

                static private bool m_databaseBuilt = false;

                static private Type[] m_allObjectDataTypes;
                static public Type[] AllObjectDataTypes { get { return m_allObjectDataTypes; } }


                #region OBJECT DATA TYPES ID MANAGEMENT

                static private Dictionary<byte, Type> m_IDToObjectDataTypeDictionary = new Dictionary<byte, Type>();
                static private Dictionary<Type, byte> m_ObjectDataTypeToIDDictionary = new Dictionary<Type, byte>();

                static public byte GetIDFromType<T>() where T : unmanaged
                {
                    return GetIDFromType(typeof(T));
                }

                static public byte GetIDFromType(Type type)
                {
                    return m_ObjectDataTypeToIDDictionary[type];
                }

                static public Type GetTypeFromID(byte id)
                {
                    return m_IDToObjectDataTypeDictionary[id];
                }

                static public void BuildDataTypeIDs()
                {
                    List<Type> validTypes = new List<Type>();
                    byte assignedID = 0;
                    // Initialize object data type IDs in this assembly
                    // TODO : When we get to supporting external assemblies declaring their own Object data types, find some deterministic way of assigning them an automatic ID.
                    {
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            ObjectDataTypeIDAttribute[] dataTypeIDAttributesArray = (ObjectDataTypeIDAttribute[])type.GetCustomAttributes(typeof(ObjectDataTypeIDAttribute), false);
                            if (dataTypeIDAttributesArray.Length > 0)
                            {
                                validTypes.Add(type);
                            }

                        }
                    }
                    Type[] allObjectDataTypes = validTypes.ToArray();

                    for (int i = 0; i < allObjectDataTypes.Length; i++)
                    {
                        Type type = (Type)allObjectDataTypes[i];
                        // Check if type has ObjectDataTypeID attribute.
                        byte typeID;

                        ObjectDataTypeIDAttribute dataTypeIDAttribute;
                        dataTypeIDAttribute = (ObjectDataTypeIDAttribute)type.GetCustomAttributes(typeof(ObjectDataTypeIDAttribute), false)[0];

                        if (dataTypeIDAttribute.ID == 0)
                        {
                            typeID = assignedID;
                            while (m_IDToObjectDataTypeDictionary.ContainsKey(typeID))
                            {
                                typeID++;
                            }
                            assignedID = (byte)(typeID + 1);
                        }
                        else
                        {
                            typeID = dataTypeIDAttribute.ID;
                            if (m_IDToObjectDataTypeDictionary.ContainsKey(typeID))
                            {
                                throw new Exception("Error : Attempted to assign ID " + typeID + " to type '" + type.Name + "'. Already taken by '" + m_IDToObjectDataTypeDictionary[typeID] + "' !");
                            }
                        }

                        m_IDToObjectDataTypeDictionary.Add(typeID, type);
                        m_ObjectDataTypeToIDDictionary.Add(type, typeID);
                    }

                    m_allObjectDataTypes = allObjectDataTypes;

                    // TODO : Clear up the code and move it to separate functions.
                }

                #endregion

                #region OBJECT TYPES MANAGEMENT

                static private ObjectArchetype[] m_allObjectArchetypes;
                static private ObjectType[] m_allObjectTypes;
                static private Dictionary<ObjectType, int> m_objectTypeToIDDictionary;
                static private Dictionary<string, int> m_objectTypeNameToIDDictionary;


                static public ObjectArchetype[] AllArchetypes
                {
                    get { return m_allObjectArchetypes; }
                }

                static public ObjectType[] AllObjectTypes
                {
                    get { return m_allObjectTypes; }
                }

                static public int GetIDFromObjectType(ObjectType obj)
                {
                    return m_objectTypeToIDDictionary[obj];
                }

                static public int ResolveNameToObjectTypeID(string name)
                {
                    if (m_objectTypeNameToIDDictionary.ContainsKey(name))
                    {
                        return m_objectTypeNameToIDDictionary[name];
                    }
                    else
                        throw new Exception("Error : Invalid Object Type Name !");
                }

                static private void BuildObjectTypes()
                {
                    ObjectTypesDatabaseBuilder dbBuilder = new ObjectTypesDatabaseBuilder();
                    dbBuilder.dataFilePath = OBJECT_TYPES_FILE_PATH;
                    dbBuilder.objectDataTypes = AllObjectDataTypes;
                    dbBuilder.LoadDatabaseFromFile();
                    m_allObjectArchetypes = dbBuilder.archetypesArray;
                    m_allObjectTypes = dbBuilder.objectTypesArray;

                    m_objectTypeToIDDictionary = new Dictionary<ObjectType, int>();
                    m_objectTypeNameToIDDictionary = new Dictionary<string, int>();
                    for (int i = 0; i < m_allObjectTypes.Length; i++)
                    {
                        ObjectType objectType = (ObjectType)m_allObjectTypes[i];
                        m_objectTypeToIDDictionary.Add(objectType, i);
                        m_objectTypeNameToIDDictionary.Add(objectType.Name, i);
                    }
                }

                #endregion

                static public void BuildDatabase()
                {
                    if (m_databaseBuilt) throw new Exception("ERROR : Object category database was already built !");

                    try
                    {
                        //BuildCategories();
                        BuildDataTypeIDs();
                        BuildObjectTypes();
                    }
                    catch(Exception e)
                    {
                        throw new Exception("ERROR While building ObjectDataTypeDatabase : " + e.Message + "\nCallstack : " + e.StackTrace + "\n Inner: " + e.InnerException) ;
                    }


                    m_databaseBuilt = true;
                }


            }
        }
    }
}

