using NeoRTS.GameData.ObjectData;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Workers
        {
            /// <summary>
            /// Updates the unit's weapon over time (IE its cooldown) and checks for possible attacks happening
            /// (usually, if we are attacking something & "canAttackTarget" is true AND our weapon cooldown is
            /// below 0 then we trigger an attack.
            /// </summary>
            public class WORKER_ObjectWeaponUpdater : Game_Worker_Base
            {
                // READ
                private OBJECT_DATA_AI[] m_objectAIData;
                private OBJECT_DATA_MOVEMENT[] m_objectsMovementData;

                // READ AND WRITE
                private OBJECT_DATA_WEAPON[] m_objectWeapons;
                private OBJECT_DATA_HEALTH[] m_objectHealths;
                private Queue<uint> m_deathRequestsQueue;

                public WORKER_ObjectWeaponUpdater(IObjectDataContainersHolder dataHolder, Queue<uint> deathRequestsQueue)
                {
                    m_objectAIData = dataHolder.GetDataContainer<OBJECT_DATA_AI>().Data;
                    m_objectWeapons = dataHolder.GetDataContainer<OBJECT_DATA_WEAPON>().Data;
                    m_objectHealths = dataHolder.GetDataContainer<OBJECT_DATA_HEALTH>().Data;
                    m_deathRequestsQueue = deathRequestsQueue;
                    m_objectsMovementData = dataHolder.GetDataContainer<OBJECT_DATA_MOVEMENT>().Data;
                }

                public override void RunWorkOnID(float deltaTime, uint ID)
                {
                    var weaponDataID = FetchDataSlotForObject<OBJECT_DATA_WEAPON>(ID);
                    if (weaponDataID != uint.MaxValue)
                    {
                        if (m_objectWeapons[weaponDataID].currentCooldown >= 0f)
                        {
                            m_objectWeapons[weaponDataID].currentCooldown -= deltaTime;
                        }

                        var AIDataID = FetchDataSlotForObject<OBJECT_DATA_AI>(ID);

                        var movementDataID = FetchDataSlotForObject<OBJECT_DATA_MOVEMENT>(ID);

                        bool movementImpairsAttacking = movementDataID != uint.MaxValue && m_objectsMovementData[movementDataID].Moving;

                        if (m_objectAIData[AIDataID].orderType == OBJECT_DATA_AI.ORDER_TYPE.ATTACK_TARGET)
                        {
                            unsafe
                            {
                                var attackOrderData = m_objectAIData[AIDataID].AttackUnitOrderData;
                                if (movementImpairsAttacking == false && attackOrderData->canAttackTarget && m_objectWeapons[weaponDataID].currentCooldown < 0f)
                                {
                                    if (m_objectWeapons[weaponDataID].currentWindup == 0f)
                                    {
                                        m_objectWeapons[weaponDataID].usingWeapon = true;
                                    }

                                    
                                    m_objectWeapons[weaponDataID].currentWindup += deltaTime;
                                    if (m_objectWeapons[weaponDataID].currentWindup > OBJECT_DATA_WEAPON.OBJECT_WEAPON_WINDUP)
                                    {
                                        fixed (OBJECT_DATA_WEAPON* weaponDataPtr = &m_objectWeapons[weaponDataID])
                                            TriggerWeaponOnTarget(attackOrderData, weaponDataPtr);
                                    }
                                }
                                else if (m_objectWeapons[weaponDataID].currentCooldown < 0f)
                                {
                                    m_objectWeapons[weaponDataID].currentWindup = 0f;
                                }

                            }
                        }
                    }

                    // TODO : Consider moving this to a different worker
                    var healthDataID = FetchDataSlotForObject<OBJECT_DATA_HEALTH>(ID);
                    if (healthDataID != uint.MaxValue && m_objectHealths[ID].HP <= 0f)
                    {
                        m_deathRequestsQueue.Enqueue(ID);
                    }
                }

                private unsafe void TriggerWeaponOnTarget(OBJECT_DATA_AI.ATTACK_TARGET_ORDER_DATA* attackOrderData, OBJECT_DATA_WEAPON* weaponData)
                {
                    // TODO : For now all weapons are a direct attack weapon.
                    // In the future there needs to be a more dynamic system for different weapon behaviors.

                    var targetHealthDataSlot = FetchDataSlotForObject<OBJECT_DATA_HEALTH>(attackOrderData->targetID);
                    
                    if (targetHealthDataSlot != uint.MaxValue)
                    {
                        m_objectHealths[targetHealthDataSlot].HP -= OBJECT_DATA_WEAPON.OBJECT_WEAPON_DAMAGE;
                    }
                    
                    weaponData->currentWindup = 0f;
                    weaponData->usingWeapon = false;
                    weaponData->currentCooldown = OBJECT_DATA_WEAPON.OBJECT_WEAPON_COOLDOWN;
                }
            }
        }
    }
}

