using System;
using System.Collections.Generic;
namespace NeoRTS
{
    namespace GameData
    {
        namespace CellSystem
        {
            /// <summary>
            /// CellGrid is a spatial partition of our Objects on the map. Objects get placed and maintained
            /// within a grid of square cells. Their position in that grid depends on their position in the world.
            /// A CellGrid allows for efficient querying of objects located in close proximity to a certain area (or other object).
            /// 
            /// The actual Cells are dynamic, flexible "owners" of a collective memory whose size is equal to an object's ID
            /// * the max amount of Objects we can support as defined in the constructor.
            /// 
            /// Currently, the grid gets updated by <see cref="Workers.WORKER_ObjectPositionGridUpdater"/>. An object's position
            /// in the grid is stored in its <see cref="ObjectData.OBJECT_DATA_CELL_COORDS"/>.
            /// </summary>
            public unsafe class CellGrid
            {
                /// <summary>
                /// Represents a change to a specific cell. Contains the cell's coordinates in the grid
                /// aswell as its new memory state (IE all the objects it should contain).
                /// TODO : Make it work with a delta-based system instead ? 
                /// </summary>
                public class CellChange
                {
                    public Cell.Coordinates coordinates;
                    public HashSet<uint> newMemoryState;
                }

                /// <summary>
                /// "Unwrapped" representation of the memory distribution structure of our Cells Grid.
                /// For a given (x;y) index, this contains the cell's position in the collective IDs memory,
                /// how many "slots" it owns and among those, how many are free slots that can be taken by other cells.
                /// </summary>
                private struct CellDistributionStructure
                {
                    public CellDistributionStructure(Cell[,] grid)
                    {
                        cellPositions = new int[grid.GetLength(0), grid.GetLength(1)];
                        cellOwnedSlotsCount = new int[grid.GetLength(0), grid.GetLength(1)];
                        cellFreeSlotsCount = new int[grid.GetLength(0), grid.GetLength(1)];
                        for (int x = 0; x < grid.GetLength(0); x++)
                        {
                            for (int y = 0; y < grid.GetLength(1); y++)
                            {
                                cellPositions[x, y] = grid[x, y].ownedIDMemoryChunkIndex;
                                cellOwnedSlotsCount[x, y] = grid[x, y].TotalOwnedSlots;
                                cellFreeSlotsCount[x, y] = grid[x, y].freeSlots;
                            }
                        }
                    }

                    public int[,] cellPositions;
                    public int[,] cellOwnedSlotsCount;
                    public int[,] cellFreeSlotsCount;
                }

                public Cell[,] cellGrid;
                private uint[] m_idSlots;
                private int m_gridSize;

                public int GridSize { get { return m_gridSize; } }

                public CellGrid(uint maxUnitCount, int gridSize)
                {
                    m_idSlots = new uint[maxUnitCount];
                    m_gridSize = gridSize;
                    cellGrid = new Cell[gridSize, gridSize];

                    float unitsPerCell = (float)maxUnitCount / (gridSize * gridSize);
                    int euclideanResult = (int)unitsPerCell;
                    int remainder = (int)(maxUnitCount - (euclideanResult * gridSize * gridSize));

                    int positionCursor = 0;
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int y = 0; y < gridSize; y++)
                        {
                            int assignedSlots = euclideanResult;
                            if (remainder > 0)
                            {
                                assignedSlots++;
                                remainder--;
                            }
                            cellGrid[x, y].freeSlots = assignedSlots;
                            cellGrid[x, y].ownedIDMemoryChunkIndex = positionCursor;
                            positionCursor += assignedSlots;
                        }
                    }

                    for(int i = 0; i < m_idSlots.Length; i++)
                    {
                        m_idSlots[i] = uint.MaxValue;
                    }
                }
                public uint[] GetObjectsInCell(Cell.Coordinates coordinates, Func<uint, bool> conditionalReturn = null)
                {
                    Cell cell = cellGrid[coordinates.x, coordinates.y];
                    List<uint> idsList = new List<uint>();
                    for (int idSlot = cell.ownedIDMemoryChunkIndex; idSlot < cell.ownedIDMemoryChunkIndex + cell.usedSlots; idSlot++)
                    {
                        if (conditionalReturn == null || conditionalReturn(m_idSlots[idSlot]))
                        {
                            idsList.Add(m_idSlots[idSlot]);
                        }
                    }

                    return idsList.ToArray();
                }


                #region Cells Memory Management
                /// <summary>
                /// Applies a set of changes to the grid.
                /// NOTE : The grid does NOT make necessary changes automatically to keep a "valid" state.
                /// If the necessary changes are not passed, you might end up with some cells thinking they own
                /// memory that's also owned by another cell, which could cause issues of overwriting.
                /// </summary>
                /// <param name="changes"></param>
                public void ApplyCellChanges(IEnumerable<CellChange> changes)
                {
                    fixed (Cell* cellGridPtr = cellGrid)
                    {
                        fixed (uint* idsMemoryPtr = m_idSlots)
                        {
                            // Step 1 : Resolve changes and if necessary move entire cells to free needed memory.
                            CellDistributionStructure cellStructure = new CellDistributionStructure(cellGrid);
                            // FIRST PASS : Resolve all changes that don't trigger a rippling structural change (IE changes that don't require a cell to get memory from a neighbour)
                            // Put all the other changes that do trigger a structural change in a list.
                            List<CellChange> structuralChangeSources = new List<CellChange>();
                            {
                                foreach (var change in changes)
                                {
                                    if (cellStructure.cellOwnedSlotsCount[change.coordinates.x, change.coordinates.y] < change.newMemoryState.Count)
                                    {
                                        structuralChangeSources.Add(change);
                                    }
                                    else
                                    {
                                        cellStructure.cellFreeSlotsCount[change.coordinates.x, change.coordinates.y] = cellStructure.cellOwnedSlotsCount[change.coordinates.x, change.coordinates.y] - change.newMemoryState.Count;
                                    }
                                }
                            }

                            // SECOND PASS : Resolve structural changes.
                            {
                                foreach (var change in structuralChangeSources)
                                {
                                    int neededExtraMemory = change.newMemoryState.Count - cellStructure.cellOwnedSlotsCount[change.coordinates.x, change.coordinates.y];
                                    // TODO : Try to "look in both directions" at once because ultimately what matters is that the least amount of data get moved around.

                                    int slotsFound = AskForSlotsBackwards(ref cellStructure, change.coordinates, neededExtraMemory);
                                    if (slotsFound < neededExtraMemory) slotsFound += AskForSlotsForward(ref cellStructure, change.coordinates, neededExtraMemory);
                                    
                                    if (slotsFound == neededExtraMemory)
                                    {
                                        cellStructure.cellFreeSlotsCount[change.coordinates.x, change.coordinates.y] = 0;
                                    }
                                    else
                                    {
                                        throw new Exception("ERROR : Somehow not enough memory for a unit to be placed.");
                                    }
                                }
                            }

                            CheckForMemoryIntersections(ref cellStructure);
                            ApplyCellDistributionStructure(ref cellStructure);

                            // Step 2 : Write new memory for relevant cells.
                            foreach (var change in changes)
                            {
                                Cell* cellPtr = cellGridPtr + change.coordinates.x * m_gridSize + change.coordinates.y;

                                Tools.Debug.Assert(cellPtr->TotalOwnedSlots >= change.newMemoryState.Count, "ASSERT ERROR : Cell memory was not big enough in Cell Changes transaction");
                                WriteData(idsMemoryPtr + cellPtr->ownedIDMemoryChunkIndex, cellPtr, change.newMemoryState);
                            }
                        }
                    }
                }

                private void WriteData(uint* startWritePtr, Cell* cellPtr, HashSet<uint> newMemoryState)
                {
                    int written = 0;
                    uint* ownedMemoryPtr = startWritePtr;
                    foreach (var id in newMemoryState)
                    {
                        if (written < cellPtr->TotalOwnedSlots)
                        {
                            *(ownedMemoryPtr + written) = id;
                            written++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    int memorySize = cellPtr->TotalOwnedSlots;
                    cellPtr->usedSlots = written;
                    cellPtr->freeSlots = memorySize - written;
                    if (cellPtr->freeSlots > 0)
                    {
                        ownedMemoryPtr = startWritePtr + cellPtr->usedSlots;

                        for (int i = 0; i < cellPtr->freeSlots; i++)
                        {
                            ownedMemoryPtr[i] = uint.MaxValue;
                        }
                    }
                }

                private void CheckForMemoryIntersections(ref CellDistributionStructure structure)
                {
                    for (int x = 0; x < m_gridSize; x++)
                    {
                        for (int y = 0; y < m_gridSize; y++)
                        {
                            if (x == m_gridSize -1 && y == m_gridSize - 1)
                            {
                                break;
                            }

                            int nextX, nextY;
                            if (y == m_gridSize-1)
                            {
                                nextY = 0;
                                nextX = x + 1;
                            }
                            else
                            {
                                nextY = y + 1;
                                nextX = x;
                            }
                            System.Diagnostics.Debug.Assert(structure.cellPositions[x, y] + structure.cellOwnedSlotsCount[x, y] == structure.cellPositions[nextX, nextY]);
                        }
                    }
                }

                private void ApplyCellDistributionStructure(ref CellDistributionStructure newStructure)
                {
                    uint[] oldIDSlots = (uint[])m_idSlots.Clone();
                    for (int x = 0; x < m_gridSize; x++)
                    {
                        for (int y = 0; y < m_gridSize; y++)
                        {
                            // APPLY NEW MEMORY SIZE TO CELL.
                            // NOTE : Remember that "memory sizes" are NOT in bytes but in number of object IDs contained.
                            int newSize = newStructure.cellOwnedSlotsCount[x, y];
                            if (newSize != cellGrid[x, y].TotalOwnedSlots)
                            {
                                int memorySizeChange = newSize - cellGrid[x, y].TotalOwnedSlots;
                                int newFreeMemorySize = cellGrid[x, y].freeSlots + memorySizeChange;
                                int newUsedMemorySize = cellGrid[x, y].usedSlots + Math.Min(0, newFreeMemorySize);
                                
                                if (newFreeMemorySize < 0)
                                    newFreeMemorySize = 0;

                                // POSSIBLE BUG CAUSE : Is truncation of data at this point normal ?

                                cellGrid[x, y].usedSlots = newUsedMemorySize;
                                cellGrid[x, y].freeSlots = newFreeMemorySize;
                            }

                            // MOVE CELL AND COPY DATA IF NEEDED.
                            int newPosition = newStructure.cellPositions[x, y];
                            if (cellGrid[x, y].ownedIDMemoryChunkIndex != newPosition)
                            {
                                if (cellGrid[x, y].usedSlots > 0)
                                {
                                    Array.Copy(oldIDSlots, cellGrid[x, y].ownedIDMemoryChunkIndex, m_idSlots, newPosition, cellGrid[x, y].usedSlots);
                                }
                                cellGrid[x, y].ownedIDMemoryChunkIndex = newPosition;
                            }

                        }
                    }
                }

                /// <summary>
                /// Asks for memory "backwards" starting from a certain index.
                /// If the next cell "to the left" has enough memory, then we simply directly apply the structural change and return true.
                /// Otherwise we have that next cell call this function on its own neighbour until the recursion either returns false
                /// (in which case we simply don't have enough memory in this direction as a whole) or true (in which case the structural change
                /// has been applied to the left and needs to be applied here).
                /// </summary>
                private int AskForSlotsBackwards(ref CellDistributionStructure distributionStructure, Cell.Coordinates startCoordinates, int askedSlotsCount)
                {
                    List<Cell.Coordinates> cellsToMoveCoordinatesList = new List<Cell.Coordinates>()
                    {
                        startCoordinates
                    };
                    int slotsFound = 0;

                    // Fix for possible Stack Overflow issues : Don't remember every coordinate we've been through
                    // but rather just how many cells we've been through and recalculate the coordinates as needed.
                    Cell.Coordinates currentSearchCoordinates = startCoordinates;
                    while ((currentSearchCoordinates.x > 0 || currentSearchCoordinates.y > 0) && slotsFound < askedSlotsCount)
                    {
                        Cell.Coordinates leftNeighbourCoordinates = currentSearchCoordinates;
                        if (currentSearchCoordinates.y > 0)
                        {
                            leftNeighbourCoordinates.y--;
                        }
                        else
                        {
                            leftNeighbourCoordinates.x--;
                            leftNeighbourCoordinates.y = m_gridSize - 1;
                        }
                        int leftNeighbourFreeSlots = distributionStructure.cellFreeSlotsCount[leftNeighbourCoordinates.x, leftNeighbourCoordinates.y];
                        int slotsTakenFromFreeMemory = Math.Min(askedSlotsCount - slotsFound, leftNeighbourFreeSlots);
                        slotsFound += slotsTakenFromFreeMemory;

                        distributionStructure.cellFreeSlotsCount[leftNeighbourCoordinates.x, leftNeighbourCoordinates.y] -= slotsTakenFromFreeMemory;
                        distributionStructure.cellOwnedSlotsCount[leftNeighbourCoordinates.x, leftNeighbourCoordinates.y] -= slotsTakenFromFreeMemory;

                        if (slotsTakenFromFreeMemory > 0)
                        {
                            foreach (var searchedCoordinates in cellsToMoveCoordinatesList)
                            {
                                distributionStructure.cellPositions[searchedCoordinates.x, searchedCoordinates.y] -= slotsTakenFromFreeMemory;
                            }
                        }

                        cellsToMoveCoordinatesList.Add(leftNeighbourCoordinates);
                        currentSearchCoordinates = leftNeighbourCoordinates;
                    }
                    distributionStructure.cellFreeSlotsCount[startCoordinates.x, startCoordinates.y] += slotsFound;
                    distributionStructure.cellOwnedSlotsCount[startCoordinates.x, startCoordinates.y] += slotsFound;

                    return slotsFound;
                }

                private int AskForSlotsForward(ref CellDistributionStructure distributionStructure, Cell.Coordinates startCoordinates, int askedSlotsCount)
                {
                    List<Cell.Coordinates> cellsToMoveCoordinatesList = new List<Cell.Coordinates>()
                    {
                        
                    };
                    int slotsFound = 0;

                    // Fix for possible Stack Overflow issues : Don't remember every coordinate we've been through
                    // but rather just how many cells we've been through and recalculate the coordinates as needed.
                    Cell.Coordinates currentSearchCoordinates = startCoordinates;
                    while ((currentSearchCoordinates.x < m_gridSize - 1 || currentSearchCoordinates.y < GridSize - 1) && slotsFound < askedSlotsCount)
                    {
                        Cell.Coordinates rightNeighbourCoordinates = currentSearchCoordinates;
                        if (currentSearchCoordinates.y < m_gridSize - 1)
                        {
                            rightNeighbourCoordinates.y++;
                        }
                        else
                        {
                            rightNeighbourCoordinates.x++;
                            rightNeighbourCoordinates.y = 0;
                        }
                        int rightNeighbourFreeSlots = distributionStructure.cellFreeSlotsCount[rightNeighbourCoordinates.x, rightNeighbourCoordinates.y];
                        int slotsTakenFromFreeMemory = Math.Min(askedSlotsCount - slotsFound, rightNeighbourFreeSlots);
                        slotsFound += slotsTakenFromFreeMemory;

                        distributionStructure.cellFreeSlotsCount[rightNeighbourCoordinates.x, rightNeighbourCoordinates.y] -= slotsTakenFromFreeMemory;
                        distributionStructure.cellOwnedSlotsCount[rightNeighbourCoordinates.x, rightNeighbourCoordinates.y] -= slotsTakenFromFreeMemory;

                        cellsToMoveCoordinatesList.Add(rightNeighbourCoordinates);
                        if (slotsTakenFromFreeMemory > 0)
                        {
                            foreach (var searchedCoordinates in cellsToMoveCoordinatesList)
                            {
                                distributionStructure.cellPositions[searchedCoordinates.x, searchedCoordinates.y] += slotsTakenFromFreeMemory;
                            }
                        }

                        
                        currentSearchCoordinates = rightNeighbourCoordinates;
                    }

                    distributionStructure.cellFreeSlotsCount[startCoordinates.x, startCoordinates.y] += slotsFound;
                    distributionStructure.cellOwnedSlotsCount[startCoordinates.x, startCoordinates.y] += slotsFound;

                    return slotsFound;
                }

                #endregion
            }
        }
    }
}

