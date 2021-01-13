using UnityEngine;
using NeoRTS.Client.Pawns;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Tools
        {


            [CreateAssetMenu(fileName = "New Object Type", menuName = "Game Data/Objects/Object Type", order = 0)]
            public class ObjectTypeDefinition : ScriptableObject
            {
                [SerializeField]
                private string displayName;
                [SerializeField]
                private ObjectArchetypeDefinition archetype;
                [SerializeField]
                private Actors.Actor actor;
                [SerializeField]
                private ObjectPawnComponent pawn;

                [SerializeField]
                private ObjectDataTypeDefGroup dataTypeOverrideDefGroup;

                public string DisplayName { get { return displayName; } }
                public ObjectArchetypeDefinition ArchetypeDef { get { return archetype; } }
                public Actors.Actor Actor { get { return actor; } }
                public ObjectPawnComponent PawnComponent { get { return pawn; } }

                public ObjectDataTypeDefGroup.ObjectDataTypeDefGroup_DESERIALIZED GenerateDeserializedData()
                {
                    return dataTypeOverrideDefGroup.BuildDeserializedData();
                }

#if UNITY_EDITOR

                public void CreateDefTypeGroupAsset()
                {
                    dataTypeOverrideDefGroup = ScriptableObject.CreateInstance<ObjectDataTypeDefGroup>();
                    string parent = UnityEditor.AssetDatabase.GetAssetPath(this.GetInstanceID());
                    parent = parent.Replace("/" + name + ".asset", "");
                    string path = UnityEditor.AssetDatabase.CreateFolder(parent, name);
                    UnityEditor.AssetDatabase.CreateAsset(dataTypeOverrideDefGroup, UnityEditor.AssetDatabase.GUIDToAssetPath(path) + "/ObjectTypeOverrideDefGroup_" + name + ".asset");
                }
#endif
            }
        }
    }
}