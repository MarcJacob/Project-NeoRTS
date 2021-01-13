using NeoRTS.GameData.CellSystem;
using NeoRTS.GameData.ObjectData;
using NeoRTS.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            /// <summary>
            /// This Worker updates a unit's Order data in certain conditions to make it use its "weapon" 
            /// the closest / most suitable target.
            /// </summary>
            public class WORKER_UnitTargetUpdater : Game_Worker_Base
            {
                private const int MAX_SEARCH_RANGE = 5;

                // READ FROM
                private CellGrid m_cellGrid;
                private OBJECT_DATA_TRANSFORM[] m_objectTransforms;
                private OBJECT_DATA_CELL_COORDS[] m_objectCellCoords;
                private OBJECT_DATA_OWNER[] m_objectOwners;
                private OBJECT_DATA_WEAPON[] m_weaponDatas;

                // WRITE INTO
                private OBJECT_DATA_AI[] m_unitAIs;

                public WORKER_UnitTargetUpdater(CellGrid cellGrid,
                                                IObjectDataContainersHolder containersHolder)
                {
                    m_cellGrid = cellGrid;
                    m_objectTransforms = containersHolder.GetDataContainer<OBJECT_DATA_TRANSFORM>().Data;
                    m_objectCellCoords = containersHolder.GetDataContainer<OBJECT_DATA_CELL_COORDS>().Data;
                    m_objectOwners = containersHolder.GetDataContainer<OBJECT_DATA_OWNER>().Data;
                    m_unitAIs = containersHolder.GetDataContainer<OBJECT_DATA_AI>().Data;
                    m_weaponDatas = containersHolder.GetDataContainer<OBJECT_DATA_WEAPON>().Data;
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    // Check for target death
                    var AIDataSlot = FetchDataSlotForObject<OBJECT_DATA_AI>(ID);
                    if (AIDataSlot == uint.MaxValue) return;

                    if (m_unitAIs[AIDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                        {
                            unsafe
                            {
                                var data = m_unitAIs[AIDataSlot].AttackUnitOrderData;
                                if (ObjectIsAlive(data->targetID) == false)
                                {
                                    // TODO : In the case this attack was started by a ATTACK_MOVE order, revert to that.
                                    m_unitAIs[AIDataSlot].orderType = OBJECT_DATA_AI.ORDER_TYPE.NONE;
                                }
                            }
                        }

                    var cellCoordinatesDataSlot = FetchDataSlotForObject<OBJECT_DATA_CELL_COORDS>(ID);
                    var transformDataSlot = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(ID);
                    var ownerDataSlot = FetchDataSlotForObject<OBJECT_DATA_OWNER>(ID);
                    var weaponDataSlot = FetchDataSlotForObject<OBJECT_DATA_WEAPON>(ID);
                    // TARGET SEARCH
                    // TODO : Change algorithm to better make sure we don't miss out on actually closer targets.
                    if (ownerDataSlot != uint.MaxValue 
                        && cellCoordinatesDataSlot != uint.MaxValue 
                        && transformDataSlot != uint.MaxValue
                        && weaponDataSlot != uint.MaxValue
                        && UnitCanSwitchTarget(AIDataSlot))
                    {
                        bool SearchForTargetsAtRange(int xIncrease, int yIncrease, List<uint> potentialTargetsList, Cell.Coordinates searchCoords1, Cell.Coordinates searchCoords2)
                        {
                            bool targetFoundAtCurrentSearchRange = false;
                            for (int x = (int)searchCoords1.x; x != searchCoords2.x + xIncrease; x += xIncrease)
                            {
                                for (int y = (int)searchCoords1.y; y != searchCoords2.y + yIncrease; y += yIncrease)
                                {
                                    if (x >= 0 && x < m_cellGrid.GridSize && y >= 0 && y < m_cellGrid.GridSize)
                                        if (x == searchCoords1.x || x == searchCoords2.x
                                            || y == searchCoords1.y || y == searchCoords2.y)
                                        {
                                            uint[] potentialTargetsArray = m_cellGrid.GetObjectsInCell(new Cell.Coordinates(x, y), (potentialTargetID) => IsPotentialTarget(ID, potentialTargetID));

                                            if (potentialTargetsArray.Length > 0)
                                            {
                                                potentialTargetsList.AddRange(potentialTargetsArray);
                                                targetFoundAtCurrentSearchRange = true;
                                            }
                                        }
                                }
                            }

                            return targetFoundAtCurrentSearchRange;
                        }
                        // TODO : Add a system to disable target seeking when it is not necessary.
                        //if (m_unitTargets[ID].shouldSeekTarget == false) return;

                        var cellCoords = m_objectCellCoords[cellCoordinatesDataSlot].Coords;
                        Cell.Coordinates cellCoordsCornerOffsetPerRange;
                        cellCoordsCornerOffsetPerRange = new Cell.Coordinates(1, 1);

                        int xIncreaseForSearch = cellCoordsCornerOffsetPerRange.x < 0 ? 1 : -1;
                        int yIncreaseForSearch = cellCoordsCornerOffsetPerRange.y < 0 ? 1 : -1;

                        List<uint> potentialTargets = new List<uint>();
                        Cell.Coordinates searchSquareCoords1, searchSquareCoords2;
                        // Search for targets at closest range.
                        int searchRange;
                        for (searchRange = 0; searchRange < MAX_SEARCH_RANGE; searchRange++)
                        {
                            searchSquareCoords1 = cellCoords + (cellCoordsCornerOffsetPerRange * searchRange);
                            searchSquareCoords2 = cellCoords - (cellCoordsCornerOffsetPerRange * (searchRange));

                            bool targetFoundAtCurrentSearchRange = SearchForTargetsAtRange(xIncreaseForSearch, yIncreaseForSearch, potentialTargets, searchSquareCoords1, searchSquareCoords2);

                            if (targetFoundAtCurrentSearchRange) break;
                        }
                        // Search one more range
                        searchRange++;
                        searchSquareCoords1 = cellCoords + (cellCoordsCornerOffsetPerRange * searchRange);
                        searchSquareCoords2 = cellCoords - (cellCoordsCornerOffsetPerRange * (searchRange));
                        SearchForTargetsAtRange(xIncreaseForSearch, yIncreaseForSearch, potentialTargets, searchSquareCoords1, searchSquareCoords2);

                        uint targetID;
                        if (FindClosestAmong(transformDataSlot, potentialTargets.ToArray(), out targetID))
                        {
                            m_unitAIs[AIDataSlot].orderType = OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET;
                            unsafe
                            {
                                *m_unitAIs[AIDataSlot].AttackUnitOrderData = new OBJECT_DATA_AI.ATTACK_TARGET_ORDER_DATA()
                                {
                                    targetID = targetID,
                                    forced = false,
                                    shouldSeekTarget = true,
                                    canAttackTarget = false
                                };
                            }
                        }
                    }
                    
                    // TODO : Consider moving this to a different Worker
                    // CAN WE ATTACK TARGET RIGHT NOW ?
                    if (weaponDataSlot != uint.MaxValue && m_unitAIs[AIDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                    {
                        unsafe
                        {
                            var data = m_unitAIs[AIDataSlot].AttackUnitOrderData;
                            var targetTransformDataSlot = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(data->targetID);
                            float distSquared = Position.SquaredDistance(m_objectTransforms[transformDataSlot].position, m_objectTransforms[targetTransformDataSlot].position);
                            data->canAttackTarget = distSquared < m_weaponDatas[weaponDataSlot].weaponRange * m_weaponDatas[weaponDataSlot].weaponRange;
                        }
                    }
                }

                private bool FindClosestAmong(uint myTransformDataSlot, uint[] ids, out uint closestID)
                {
                    float closestSquareDistance = 0f;
                    closestID = uint.MaxValue;
                    foreach(var id in ids)
                    {
                        var potentialTargetTransformDataSlot = FetchDataSlotForObject<OBJECT_DATA_TRANSFORM>(id);
                        float distSquared = Position.SquaredDistance(m_objectTransforms[myTransformDataSlot].position, m_objectTransforms[potentialTargetTransformDataSlot].position);
                        if (closestID == uint.MaxValue || distSquared < closestSquareDistance)
                        {
                            closestID = id;
                            closestSquareDistance = distSquared;
                        }
                    }
                    if (closestID == uint.MaxValue)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                // TODO : For now assumes we want to check if something is "enemy". Change that to something
                // more dynamic depending on the kind of "weapon" we're looking to use.
                private bool IsPotentialTarget(uint myID, uint potentialTargetID)
                {
                    if (potentialTargetID == uint.MaxValue)
                    {
                        throw new System.Exception("Error : Potential target ID is equal to MaxValue !");
                    }

                    var potentialTargetOwnerDataSlot = FetchDataSlotForObject<OBJECT_DATA_OWNER>(potentialTargetID);

                    if (potentialTargetOwnerDataSlot == uint.MaxValue) return false;

                    var myOwnerDataSlot = FetchDataSlotForObject<OBJECT_DATA_OWNER>(myID);
                    return m_objectOwners[myOwnerDataSlot].ownerID != m_objectOwners[potentialTargetOwnerDataSlot].ownerID;
                }

                private bool UnitCanSwitchTarget(uint AIDataSlot)
                {
                    if (m_unitAIs[AIDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.NONE)
                    {
                        return true;
                    }
                    else if (m_unitAIs[AIDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION)
                    {
                        return false;
                    }
                    else if (m_unitAIs[AIDataSlot].orderType == OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                    {
                        unsafe
                        {
                            var data = m_unitAIs[AIDataSlot].AttackUnitOrderData;
                            return !data->forced;
                        }
                    }

                    return true;
                }
            }
        }
    }
}

