using NeoRTS.Client.UI;
using NeoRTS.GameData;
using NeoRTS.GameData.Actors;
using NeoRTS.GameData.ObjectData;
using System;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace Pawns
        {

            public class UnitPawnComponent : ObjectPawnComponent
            {

                // TODO : Overhaul data watcher system so that we're able to watch for any change within
                // a member value of the data we're watching.
                private DataWatcher<OBJECT_DATA_AI> m_orderDataWatcher;
                private DataWatcher<OBJECT_DATA_HEALTH> m_healthDataWatcher;
                private DataWatcher<OBJECT_DATA_MOVEMENT> m_movementDataWatcher;
                private DataWatcher<OBJECT_DATA_WEAPON> m_weaponDataWatcher;
               

                // UI MODULES
                private PawnHealthBarPawnUIModule m_uiModuleHealthBar;

                private bool m_inCombat = false;

                private bool HasOrderDataChanged(OBJECT_DATA_AI previous, OBJECT_DATA_AI current)
                {
                    return !previous.Equals(current);
                }

                private bool HasHealthDataChanged(OBJECT_DATA_HEALTH previous, OBJECT_DATA_HEALTH current)
                {
                    return !previous.Equals(current);
                }

                private bool HasMovementDataChanged(OBJECT_DATA_MOVEMENT previous, OBJECT_DATA_MOVEMENT current)
                {
                    return previous.Moving != current.Moving;
                }

                protected override void LinkToExtraGameData(ObjectMemoryManager memoryManager)
                {
                    if (RegisterDataWatcher(out m_orderDataWatcher, HasOrderDataChanged))
                    {
                        m_orderDataWatcher.onValueChanged += OnOrderDataChanged;
                    }
                    if (RegisterDataWatcher(out m_movementDataWatcher, HasMovementDataChanged))
                    {
                        m_movementDataWatcher.onValueChanged += OnMovementDataChanged;
                    }

                    RegisterDataWatcher(out m_weaponDataWatcher);
                    
                }

                private void OnMovementDataChanged(OBJECT_DATA_MOVEMENT movement)
                {
                    if (movement.Moving)
                    {
                        TriggerActorEvent(ACTOR_EVENT.START_MOVEMENT);
                    }
                    else
                    {
                        TriggerActorEvent(ACTOR_EVENT.STOP_MOVEMENT);
                    }
                }

                private void OnOrderDataChanged(OBJECT_DATA_AI order)
                {
                    if (order.orderType != OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                    {

                    }
                    else
                    {

                    }

                    if (order.orderType == OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION)
                    {

                    }
                    else
                    {

                    }
                }

                protected unsafe override void OnPawnUpdate()
                {
                    switch(m_orderDataWatcher.CurrentValue.orderType)
                    {
                        case (OBJECT_DATA_AI.ORDER_TYPE.NONE):
                            break;
                        case (OBJECT_DATA_AI.ORDER_TYPE.MOVE_TO_POSITION):
                            transform.LookAt(*m_orderDataWatcher.CurrentValue.MoveToPositionOrderData, Vector3.up);
                            break;
                        case (OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET):
                            {
                                uint targetID = m_orderDataWatcher.CurrentValue.AttackUnitOrderData->targetID;
                                var targetPawn = PawnsManager.GetPawnFromObjectID(targetID);
                                if (targetPawn != null)
                                {
                                    Position targetPos = targetPawn.transform.position;
                                    transform.LookAt(targetPos, Vector3.up);

                                    float inCombatRange = m_weaponDataWatcher.CurrentValue.weaponRange;
                                    inCombatRange *= inCombatRange;
                                    bool inCombat = Position.SquaredDistance(transform.position, targetPos) <  inCombatRange;
                                    if (inCombat != m_inCombat)
                                    {
                                        m_inCombat = inCombat;
                                        TriggerActorEvent(inCombat ? ACTOR_EVENT.ENTER_COMBAT : ACTOR_EVENT.LEAVE_COMBAT);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}


