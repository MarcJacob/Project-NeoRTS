using NeoRTS.GameData.ObjectData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {

            /// <summary>
            /// Base class of all Game Workers.
            /// A "Game Worker" is a independent piece of logic that acts over a certain amount of memory spaces that get fed
            /// to it upon construction. The Template parameters define the type of data that each of these spaces correspond to.
            /// Game workers are ran by whatever "owns" them, such as the Server Manager. They MUST override the "Run" function
            /// that will give them which data IDs to process and the delta time since the last time it was called.
            /// 
            /// Whether Workers are parallelized, whether they run before or after other workers... is to be handled by the OWNER,
            /// NOT the worker itself. Workers do NOT know about one another.
            /// </summary>
            public unsafe abstract class Game_Worker_Base
            {
                // Direct reference to the OBJECT array in (currently) the ObjectMemoryManager.
                // The point of this reference is to avoid giving a worker a reference to a manager.
                private ReadOnlyCollection<ObjectMemoryManager.OBJECT> m_objectsDataTable;

                protected ReadOnlyCollection<ObjectMemoryManager.OBJECT> ObjectsDataTable
                {
                    get { return m_objectsDataTable; }
                }

                public void AssignObjectsDataTable(ReadOnlyCollection<ObjectMemoryManager.OBJECT> objectsDataTable)
                {
                    m_objectsDataTable = objectsDataTable;
                }

                protected uint FetchDataSlotForObject<T>(uint objectID) where T : unmanaged
                {
                    var objectDataTable = m_objectsDataTable[(int)objectID];

                    return objectDataTable.GetDataSlotIDForType<T>();
                }

                protected bool ObjectIsAlive(uint objectID)
                {
                    return m_objectsDataTable[(int)objectID].alive;
                }

                public bool RunsOnlyOnAuthoritativeMatch = false;

                public Game_Worker_Base(bool runsOnlyOnAuthoritativeMatch)
                {
                    RunsOnlyOnAuthoritativeMatch = runsOnlyOnAuthoritativeMatch;
                }
                public Game_Worker_Base()
                {
                    RunsOnlyOnAuthoritativeMatch = false;
                }

                // Runs before any worker has run its Work.
                public virtual void OnFrameBegin(float deltaTime)
                {

                }

                // Runs before this specific worker type starts doing work over every living object ID.
                public virtual void PreWork(float deltaTime)
                {

                }

                // Runs work over a certain object ID.
                public virtual void RunWorkOnID(float deltaTime, uint ID)
                {

                }

                // Runs after this specific worker type finishes doing work over every living object ID.
                public virtual void PostWork(float deltaTime)
                {

                }

                // Runs after every worker has run its work.
                public virtual void OnFrameEnd(float deltaTime)
                {

                }
            }


        }
    }
}

