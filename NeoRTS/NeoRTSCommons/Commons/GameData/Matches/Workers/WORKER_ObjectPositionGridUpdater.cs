using NeoRTS.GameData.ObjectData;
using System;
using NeoRTS.GameData.CellSystem;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            /// <summary>
            /// This Worker is responsible for rebuilding the Unit Grid every frame using a Object Coordinates -> Cell Coordinates
            /// function it gets fed at construction. 
            /// </summary>
            public class WORKER_ObjectPositionGridUpdater : Game_Worker_Base
            {
                
                private Func<Position, Cell.Coordinates> m_getCellCoordinatesFromPosFunc;

                // WRITE INTO
                private OBJECT_DATA_CELL_COORDS[] m_objectCellCoords;
                private CellGrid m_targetGrid;

                // READ FROM
                private OBJECT_DATA_TRANSFORM[] m_objectTransforms;

                public WORKER_ObjectPositionGridUpdater(Func<Position, Cell.Coordinates> getCellCoordsFromPosFunc, 
                                                        CellGrid targetCellGridPtr,
                                                        OBJECT_DATA_CELL_COORDS[] objectCellCoords,
                                                        OBJECT_DATA_TRANSFORM[] objectTransforms)
                {
                    m_targetGrid = targetCellGridPtr;
                    m_getCellCoordinatesFromPosFunc = getCellCoordsFromPosFunc;
                    m_objectCellCoords = objectCellCoords;
                    m_objectTransforms = objectTransforms;
                }

                private Dictionary<Cell.Coordinates, CellGrid.CellChange> m_changesDictionary = new Dictionary<Cell.Coordinates, CellGrid.CellChange>();

                public override void PreWork(float deltaTime)
                {
                    m_changesDictionary.Clear();
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    var cellCoordinatesDataSlot = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(ID);
                    Cell.Coordinates cellCoords = m_getCellCoordinatesFromPosFunc(m_objectTransforms[cellCoordinatesDataSlot].position);
                    
                    if (cellCoords != m_objectCellCoords[cellCoordinatesDataSlot].Coords)
                    {
                        // Register change over the source cell and the destination cell
                        // Source (only if object was previously placed and didn't just get spawned)
                        if (m_objectCellCoords[cellCoordinatesDataSlot].placed)
                        {
                            if (m_changesDictionary.ContainsKey(m_objectCellCoords[cellCoordinatesDataSlot].Coords) == false)
                            {
                                InitializeCellChangeObjectWithID(m_objectCellCoords[cellCoordinatesDataSlot].Coords);
                            }
                            m_changesDictionary[m_objectCellCoords[cellCoordinatesDataSlot].Coords].newMemoryState.Remove(ID);
                        }

                        if (m_changesDictionary.ContainsKey(cellCoords) == false)
                        {
                            InitializeCellChangeObjectWithID(cellCoords);
                        }
                        m_changesDictionary[cellCoords].newMemoryState.Add(ID);
                        m_objectCellCoords[cellCoordinatesDataSlot] = new OBJECT_DATA_CELL_COORDS(cellCoords);
                    }
                }

                private void InitializeCellChangeObjectWithID(Cell.Coordinates coords)
                {
                    HashSet<uint> currentMemoryState = new HashSet<uint>();

                    foreach (var objectInCellID in m_targetGrid.GetObjectsInCell(coords))
                    {
                        currentMemoryState.Add(objectInCellID);
                    }


                    m_changesDictionary.Add(coords, new CellGrid.CellChange()
                    {
                        newMemoryState = currentMemoryState,
                        coordinates = coords
                    });
                }

                public override void PostWork(float deltaTime)
                {
                    m_targetGrid.ApplyCellChanges(m_changesDictionary.Values);
                }
            }
        }
    }
}

