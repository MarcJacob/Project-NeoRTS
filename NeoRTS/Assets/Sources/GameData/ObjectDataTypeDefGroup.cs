using NeoRTS.GameData.ObjectData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Tools
        {
            [Serializable]
            public class ObjectDataTypeDefGroup : ScriptableObject, ISerializationCallbackReceiver
            {

                [Serializable]
                public struct ObjectDataTypeDefField_GAMEDATA_REF
                {
                    [SerializeField]
                    public string fieldName;
                    [SerializeField]
                    public UnityEngine.Object fieldRef;

                    public Type refType;
                }

                #region Serialized

                [Serializable]
                public struct ObjectDataTypeDefField_SERIALIZED
                {
                    [SerializeField]
                    public string fieldName;
                    [SerializeField]
                    public string fieldDefaultValue;
                }

                [Serializable]
                public struct ObjectDataTypeDef_SERIALIZED
                {
                    [SerializeField]
                    public string dataTypeName;
                    [SerializeField]
                    public List<ObjectDataTypeDefField_SERIALIZED> fields;
                    [SerializeField]
                    public List<ObjectDataTypeDefField_GAMEDATA_REF> gameDataRefFields;
                }

                [Serializable]
                public struct ObjectDataTypeDefGroup_SERIALIZED
                {
                    [SerializeField]
                    public List<ObjectDataTypeDef_SERIALIZED> dataDefList;
                }

                #endregion

                #region Deserialized


                public struct ObjectDataTypeDefField_DESERIALIZED
                {
                    public string fieldName;
                    public Type fieldType;
                    public byte[] fieldDefaultValue;

                    public unsafe T GetDefaultValue<T>() where T : unmanaged
                    {
                        fixed (byte* ptr = fieldDefaultValue)
                            return *(T*)ptr;
                    }

                    public unsafe void SetDefaultValue(object data)
                    {
                        if (fieldType == typeof(int))
                        {
                            SetDefaultValue((int)data);
                        }
                        else if (fieldType == typeof(uint))
                        {
                            SetDefaultValue((uint)data);
                        }
                        else if (fieldType == typeof(bool))
                        {
                            SetDefaultValue((bool)data);
                        }
                        else if (fieldType == typeof(float))
                        {
                            SetDefaultValue((float)data);
                        }
                        else
                        {
                            throw new Exception("Field data type not supported ! Attempted to set type " + fieldType.Name);
                        }
                    }

                    public unsafe void SetDefaultValue<T>(T data) where T : unmanaged
                    {
                        fieldDefaultValue = new byte[sizeof(T)];
                        fixed (byte* ptr = fieldDefaultValue)
                        {
                            Buffer.MemoryCopy(&data, ptr, sizeof(T), sizeof(T));
                        }
                    }
                }
                public struct ObjectDataTypeDef_DESERIALIZED
                {
                    public Type dataDefType;
                    public List<ObjectDataTypeDefField_DESERIALIZED> fields;
                    public List<ObjectDataTypeDefField_GAMEDATA_REF> gameDataRefFields;
                }

                public struct ObjectDataTypeDefGroup_DESERIALIZED
                {
                    public List<ObjectDataTypeDef_DESERIALIZED> dataDefList;
                }

                #endregion

                public ObjectDataTypeDefGroup()
                {
                    UpdateObjectDataTypesDictionary();
                }

                private void UpdateObjectDataTypesDictionary()
                {
                    objectDataTypesDictionary = Assembly.GetAssembly(typeof(ObjectData.ObjectDataTypeDatabase)).GetTypes().Where(t => t.GetCustomAttribute<ObjectDataTypeIDAttribute>() != null).ToDictionary(t => t.Name);
                }

                [SerializeField]
                private ObjectDataTypeDefGroup_SERIALIZED serializedData;

                public ObjectDataTypeDefGroup_DESERIALIZED? deserializedData;
                public Dictionary<string, Type> objectDataTypesDictionary;

                public void BuildSerializedData()
                {
                    if (deserializedData.HasValue)
                    {
                        serializedData = new ObjectDataTypeDefGroup_SERIALIZED()
                        {
                            dataDefList = new List<ObjectDataTypeDef_SERIALIZED>()
                        };

                        foreach (var def in deserializedData.Value.dataDefList)
                        {
                            List<ObjectDataTypeDefField_SERIALIZED> fieldList = new List<ObjectDataTypeDefField_SERIALIZED>();
                            List<ObjectDataTypeDefField_GAMEDATA_REF> gameDataDefFieldList = new List<ObjectDataTypeDefField_GAMEDATA_REF>();

                            // Standard fields construction
                            foreach (var field in def.fields)
                                {
                                    object val = NeoRTS.Tools.BytesReader.ReadFromBytes(field.fieldDefaultValue, field.fieldType);
                                    fieldList.Add(new ObjectDataTypeDefField_SERIALIZED()
                                    {
                                        fieldName = field.fieldName,
                                        fieldDefaultValue = val.ToString()
                                    });
                                }

                            // Game data ref fields construction
                            foreach(var field in def.gameDataRefFields)
                            {
                                gameDataDefFieldList.Add(field);
                            }

                            serializedData.dataDefList.Add(
                                    new ObjectDataTypeDef_SERIALIZED()
                                    {
                                        dataTypeName = def.dataDefType.Name,
                                        fields = fieldList,
                                        gameDataRefFields = gameDataDefFieldList
                                    }
                            );
                        }

                    }
                }

                public ObjectDataTypeDefGroup_DESERIALIZED BuildDeserializedData()
                {
                    if (serializedData.dataDefList == null)
                    {
                        Debug.Log("Serialized Data was null.");
                        serializedData.dataDefList = new List<ObjectDataTypeDefGroup.ObjectDataTypeDef_SERIALIZED>();
                    }

                    deserializedData = new ObjectDataTypeDefGroup_DESERIALIZED()
                    {
                        dataDefList = new List<ObjectDataTypeDef_DESERIALIZED>()
                    };
                    // TODO : Support loading object data types from other assemblies.
                    UpdateObjectDataTypesDictionary();

                    foreach (var def in serializedData.dataDefList)
                    {
                        // TODO : Handle if the data type name is unknown. Remove current serialized data for it.
                        Type defType = objectDataTypesDictionary[def.dataTypeName];

                        HashSet<string> addedFields = new HashSet<string>();

                        List<ObjectDataTypeDefField_DESERIALIZED> fields = new List<ObjectDataTypeDefField_DESERIALIZED>();
                        List<ObjectDataTypeDefField_GAMEDATA_REF> gameDataRefFields = new List<ObjectDataTypeDefField_GAMEDATA_REF>();
                        
                        // Read fields that are currently serialized
                        foreach (var field in def.fields)
                        {
                            ObjectDataTypeDefField_DESERIALIZED deserializedField = new ObjectDataTypeDefField_DESERIALIZED();
                            deserializedField.fieldName = field.fieldName;
                            deserializedField.fieldType = defType.GetField(field.fieldName).FieldType;
                            object defaultValue = Convert.ChangeType(field.fieldDefaultValue, deserializedField.fieldType);
                            deserializedField.SetDefaultValue(defaultValue);

                            fields.Add(deserializedField);
                            addedFields.Add(field.fieldName);
                        }

                        foreach(var field in def.gameDataRefFields)
                        {
                            var deserialized = field;
                            deserialized.refType = typeof(ObjectTypeDefinition);
                            gameDataRefFields.Add(deserialized);

                            addedFields.Add(field.fieldName);
                        }

                        // Check if we have missing fields and add them if needed
                        FieldInfo[] fieldsInCurrentDefType = defType.GetFields();
                        foreach(var field in fieldsInCurrentDefType)
                        {
                            if (field.FieldType.IsPrimitive && field.IsStatic == false && addedFields.Contains(field.Name) == false)
                            {
                                var newField = new ObjectDataTypeDefField_DESERIALIZED()
                                {
                                    fieldName = field.Name,
                                    fieldType = field.FieldType
                                };
                                newField.SetDefaultValue(Activator.CreateInstance(field.FieldType));
                                fields.Add(newField);
                            }
                        }

                        deserializedData.Value.dataDefList.Add(new ObjectDataTypeDef_DESERIALIZED()
                        {
                            dataDefType = defType,
                            fields = fields,
                            gameDataRefFields = gameDataRefFields
                        });
                    }

                    return deserializedData.Value;
                }

                public void OnBeforeSerialize()
                {
                     BuildSerializedData();
                }

                public void OnAfterDeserialize()
                {
                    BuildDeserializedData();
                }
            }
        }
    }
}