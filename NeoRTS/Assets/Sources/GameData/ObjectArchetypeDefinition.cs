

using NeoRTS.GameData.ObjectData;
using NeoRTS.GameData.ObjectData.Types;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Tools
        {


            [CreateAssetMenu(fileName = "New Archetype", menuName = "Game Data/Objects/Archetype", order = 0)]
            public class ObjectArchetypeDefinition : ScriptableObject
            {
                [SerializeField]
                private ObjectDataTypeDefGroup dataTypeDefGroup;

                public bool HasDefGroup
                {
                    get { return dataTypeDefGroup != null; }
                }

                public ObjectDataTypeDefGroup.ObjectDataTypeDefGroup_DESERIALIZED GenerateDeserializedData()
                {
                    return dataTypeDefGroup.BuildDeserializedData();
                }
#if UNITY_EDITOR

                public void CreateDefTypeGroupAsset()
                {
                    dataTypeDefGroup = ScriptableObject.CreateInstance<ObjectDataTypeDefGroup>();
                    string parent = UnityEditor.AssetDatabase.GetAssetPath(this.GetInstanceID());
                    parent = parent.Replace("/" + name + ".asset", "");
                    string path = UnityEditor.AssetDatabase.CreateFolder(parent, name);
                    UnityEditor.AssetDatabase.CreateAsset(dataTypeDefGroup, UnityEditor.AssetDatabase.GUIDToAssetPath(path) + "/ArchetypeDefGroup_" + name + ".asset");
                }
#endif

            }
        }
    }
}