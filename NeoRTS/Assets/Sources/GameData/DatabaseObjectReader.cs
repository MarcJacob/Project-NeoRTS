using System;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace Client
    {
        namespace GameData
        {
            public class DatabaseObjectReader<T> where T : UnityEngine.Object
            {
                public static DatabaseObjectReader<T> FindDBAndCreateReader(IEnumerable<GameDataDatabaseObject> databases)
                {
                    foreach(var db in databases)
                    {
                        if (db.ContainsCastableTo<T>())
                        {
                            return new DatabaseObjectReader<T>(db);
                        }
                    }
                    return null;
                }

                private GameDataDatabaseObject m_target;

                public DatabaseObjectReader(GameDataDatabaseObject target)
                {
                    if (!target.ContainsCastableTo<T>())
                    {
                        throw new Exception("Error : Attempted to create Database Object Reader of the wrong type !");
                    }
                    m_target = target;
                }

                public T GetElementFromID(int id)
                {
                    return (T)m_target.GetElementFromID(id);
                }

                public IEnumerable<T> GetAllElements()
                {
                    List<T> castElements = new List<T>();
                    foreach(var element in m_target.GetAllElements())
                    {
                        castElements.Add((T)element);
                    }
                    return castElements;
                }
            }
        }
    }
}