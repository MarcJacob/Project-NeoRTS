using NeoRTS.Client.Pawns;
using NeoRTS.GameData.Actors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace GameData
        {
            public class GameDataDatabaseObject : ScriptableObject
            {
                [Serializable]
                public class GameDataDatabaseObjectRow
                {
                    public int ID;
                    public UnityEngine.Object Element;
                }

                [SerializeReference]
                private GameDataDatabaseObjectRow[] rows;

                public void Initialize(UnityEngine.Object[] elements)
                {
                    if (rows != null) throw new System.Exception("Error : Attempted to initialize database object more than once !");
                    rows = new GameDataDatabaseObjectRow[elements.Length];
                    for (int i = 0; i < elements.Length; i++)
                    {
                        UnityEngine.Object element = elements[i];
                        rows[i] = new GameDataDatabaseObjectRow()
                        {
                            ID = i,
                            Element = element
                        };
                    }
                }

                public IEnumerable<UnityEngine.Object> GetAllElements()
                {
                    List<UnityEngine.Object> allElements = new List<UnityEngine.Object>();
                    foreach(var row in rows)
                    {
                        allElements.Add(row.Element);
                    }

                    return allElements;
                }

                public UnityEngine.Object GetElementFromID(int id)
                {
                    return rows[id].Element;
                }

                public string GetSuitableName()
                {
                    if (rows == null || rows.Length == 0)
                    {
                        throw new Exception("Error : Used database object without it being populated.");
                    }
                    return "DB_" + rows[0].Element.GetType().Name;
                }

                public bool ContainsCastableTo<T>()
                {
                    if (rows == null || rows.Length == 0)
                    {
                        throw new Exception("Error : Used database object without it being populated.");
                    }

                    var firstElement = rows[0];
                    return firstElement.Element is T;
                }
            }
        }
    }
}