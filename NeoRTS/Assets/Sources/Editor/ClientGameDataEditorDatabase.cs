using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.ObjectModel;
using NeoRTS.Client.GameData;

namespace NeoRTS
{
    namespace EditorTools
    {

        namespace GoogleDriveExtension
        {
        }
        public static class ClientGameDataEditorDatabase<T> where T : UnityEngine.Object
        {
            static private T[] m_allElements;
            static private Dictionary<T, int> m_ElementToIDDictionary;

            static public ReadOnlyCollection<T> All
            {
                get { return new ReadOnlyCollection<T>(m_allElements); }
            }

            static public T GetElementByID(int id)
            {
                return m_allElements[id];
            }

            static public int GetElementID(T element)
            {
                return m_ElementToIDDictionary[element];
            }

            static public void Refresh(string path)
            {
                IEnumerable<string> files = Directory.GetFiles(path).Where(fileName => fileName.EndsWith(".meta") == false);

                string dataPathNoAssetFolder = Application.dataPath.Replace("/Assets", "");

                List<T> loadedElements = new List<T>();

                foreach (var file in files)
                {
                    string fileTrimmed = file.Replace(dataPathNoAssetFolder + "/", "");
                    var loadedAsset = AssetDatabase.LoadAssetAtPath<T>(fileTrimmed);
                    if (loadedAsset != null)
                    {
                        loadedElements.Add(loadedAsset);
                    }
                    
                }
                m_allElements = loadedElements.ToArray();

                int counter = 0;
                m_ElementToIDDictionary = new Dictionary<T, int>();
                foreach(var element in m_allElements)
                {
                    m_ElementToIDDictionary.Add(element, counter);
                    counter++;
                }
            }

            static public void OutputDataToDatabaseObject(string path)
            {
                GameDataDatabaseObject databaseObject = ScriptableObject.CreateInstance<GameDataDatabaseObject>();
                databaseObject.Initialize(m_allElements);
                AssetDatabase.CreateAsset(databaseObject, "Assets/" + path + "/" + databaseObject.GetSuitableName() + ".asset");

            }
        }
    }
}