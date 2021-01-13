using UnityEngine;
using UnityEditor;
using NeoRTS.GameData.Tools;
using System.Linq;
using System.Collections.Generic;
using System;
using NeoRTS.GameData.ObjectData;

namespace NeoRTS
{
    namespace EditorTools
    {
        [CustomEditor(typeof(ObjectTypeDefinition))]
        public class ObjectTypeCustomInspector : Editor
        {
            public override void OnInspectorGUI()
            {

                if (serializedObject.FindProperty("dataTypeOverrideDefGroup").objectReferenceValue != null)
                {
                    DrawDefaultInspector();
                }
                else
                {
                    if (GUILayout.Button("Create Object Data Types Definition Group"))
                    {
                        (target as ObjectTypeDefinition).CreateDefTypeGroupAsset();
                        serializedObject.Update();
                    }
                }
            }
        }

        [CustomEditor(typeof(ObjectArchetypeDefinition))]
        public class ObjectArchetypeCustomInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                if (serializedObject.FindProperty("dataTypeDefGroup").objectReferenceValue != null)
                {
                    DrawDefaultInspector();
                }
                else
                {
                    if (GUILayout.Button("Create Object Data Types Definition Group"))
                    {
                        (target as ObjectArchetypeDefinition).CreateDefTypeGroupAsset();
                        serializedObject.Update();
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }

        [CustomEditor(typeof(ObjectDataTypeDefGroup))]
        public class ObjectDataTypeDefGroupCustomInspector : Editor
        {
            int selectedObjectDataTypeID = 0;

            public override void OnInspectorGUI()
            {
                var deserializedData = (target as ObjectDataTypeDefGroup).deserializedData;
                var objectDataTypesDictionary = (target as ObjectDataTypeDefGroup).objectDataTypesDictionary;

                if (deserializedData.HasValue == false)
                {
                    deserializedData = new ObjectDataTypeDefGroup.ObjectDataTypeDefGroup_DESERIALIZED()
                    {
                        dataDefList = new List<ObjectDataTypeDefGroup.ObjectDataTypeDef_DESERIALIZED>()
                    };
                    (target as ObjectDataTypeDefGroup).deserializedData = deserializedData;
                }
                else
                {
                    for (int dataDefID = 0; dataDefID < deserializedData.Value.dataDefList.Count; dataDefID++)
                    {

                        ObjectDataTypeDefGroup.ObjectDataTypeDef_DESERIALIZED def = deserializedData.Value.dataDefList[dataDefID];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(def.dataDefType.Name);
                        bool removeButtonPressed = GUILayout.Button("-");
                        EditorGUILayout.EndHorizontal();
                        if (removeButtonPressed)
                        {
                            deserializedData.Value.dataDefList.RemoveAt(dataDefID);
                            dataDefID--;
                        }
                        else
                        {
                            for (int i = 0; i < def.fields.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();

                                var field = def.fields[i];
                                object data = DrawEditorFieldForField(def.fields[i]);
                                if (data != null)
                                {
                                    field.SetDefaultValue(data);
                                }
                                
                                def.fields[i] = field;

                                EditorGUILayout.EndHorizontal();
                            }
                            for(int i = 0; i < def.gameDataRefFields.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();

                                UnityEngine.Object data = DrawEditorFieldForGameDataField(def.gameDataRefFields[i]);

                                var field = def.gameDataRefFields[i];
                                field.fieldRef = data;
                                def.gameDataRefFields[i] = field;

                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }

                }

                string[] objectDataTypeNames = objectDataTypesDictionary.Keys.ToArray();
                EditorGUILayout.BeginHorizontal();
                selectedObjectDataTypeID = EditorGUILayout.Popup(selectedObjectDataTypeID, objectDataTypeNames);

                if (GUILayout.Button("+"))
                {
                    List<ObjectDataTypeDefGroup.ObjectDataTypeDefField_DESERIALIZED> fields = new List<ObjectDataTypeDefGroup.ObjectDataTypeDefField_DESERIALIZED>();
                    List<ObjectDataTypeDefGroup.ObjectDataTypeDefField_GAMEDATA_REF> gameDataRefFields = new List<ObjectDataTypeDefGroup.ObjectDataTypeDefField_GAMEDATA_REF>();
                    var type = objectDataTypesDictionary[objectDataTypeNames[selectedObjectDataTypeID]];
                    
                    foreach (var field in type.GetFields())
                    {
                        if (!field.IsStatic && field.FieldType.IsPrimitive)
                        {
                            if (field.FieldType == typeof(int) && field.GetCustomAttributes(typeof(ObjectTypeReferenceAttribute), false).Length > 0)
                            {
                                var newField = new ObjectDataTypeDefGroup.ObjectDataTypeDefField_GAMEDATA_REF()
                                {
                                    fieldName = field.Name,
                                    fieldRef = null,
                                    refType = typeof(ObjectTypeDefinition)
                                };
                                gameDataRefFields.Add(newField);
                            }
                            else
                            {
                                var newField = new ObjectDataTypeDefGroup.ObjectDataTypeDefField_DESERIALIZED()
                                {
                                    fieldName = field.Name,
                                    fieldType = field.FieldType
                                };
                                newField.SetDefaultValue(Activator.CreateInstance(field.FieldType));
                                fields.Add(newField);
                            }
                        }

                    }

                    deserializedData.Value.dataDefList.Add(new ObjectDataTypeDefGroup.ObjectDataTypeDef_DESERIALIZED()
                    {
                        dataDefType = type,
                        fields = fields,
                        gameDataRefFields = gameDataRefFields
                    });
                }

                EditorGUILayout.EndHorizontal();

                EditorUtility.SetDirty(target);
            }

            private object DrawEditorFieldForField(ObjectDataTypeDefGroup.ObjectDataTypeDefField_DESERIALIZED field)
            {
                
                if (field.fieldType == typeof(int))
                {
                    return EditorGUILayout.IntField(field.fieldName, field.GetDefaultValue<int>());
                }
                else if (field.fieldType == typeof(uint))
                {
                    return EditorGUILayout.IntField(field.fieldName, field.GetDefaultValue<int>());
                }
                else if (field.fieldType == typeof(bool))
                {
                    return EditorGUILayout.Toggle(field.fieldName, field.GetDefaultValue<bool>());
                }
                else if (field.fieldType == typeof(float))
                {
                    return EditorGUILayout.FloatField(field.fieldName, field.GetDefaultValue<float>());
                }
                else
                {
                    return null;
                }
            }
        
            private UnityEngine.Object DrawEditorFieldForGameDataField(ObjectDataTypeDefGroup.ObjectDataTypeDefField_GAMEDATA_REF field)
            {
                if (field.refType == typeof(ObjectTypeDefinition))
                {
                    return EditorGUILayout.ObjectField(field.fieldName, field.fieldRef, field.refType, false);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}