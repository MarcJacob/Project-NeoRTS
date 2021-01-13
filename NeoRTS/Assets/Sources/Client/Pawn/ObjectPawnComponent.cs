using NeoRTS.Client.LocalMatch;
using NeoRTS.Client.UI;
using NeoRTS.GameData;
using NeoRTS.GameData.Actors;
using NeoRTS.GameData.ObjectData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace Pawns
        {
            /// <summary>
            /// An Object Pawn Component is a component held by any GameObject spawned as a Object Pawn.
            /// This component, through its link to <see cref="ObjectPawnsManager"/> is responsible for handling
            /// events that are fired on its linked object.
            /// 
            /// On top of that, it is also responsible for "tracking" or "watching" (using a <see cref="DataWatcher{T}"/>
            /// any data of its linked object it needs in order to accurately represent its linked object's state.
            /// </summary>
            public unsafe abstract class ObjectPawnComponent : MonoBehaviour
            {
                public abstract class DataWatcher_Base
                {
                    public abstract void Watch();
                }
                public unsafe class DataWatcher<T> : DataWatcher_Base where T : unmanaged
                {
                    private T* ptr;
                    public T CurrentValue
                    {
                        get { return *ptr; }
                    }
                    private T* Pointer
                    {
                        get { return ptr; }
                    }

                    private T m_previousValue;

                    private Func<T, T, bool> m_hasChangedFunction;

                    public DataWatcher(T* watchedPtr, Func<T, T, bool> hasChangedFunction = null)
                    {
                        ptr = watchedPtr;
                        m_hasChangedFunction = hasChangedFunction;
                    }

                    public event Action<T> onValueChanged = delegate { };

                    public override void Watch()
                    {
                        if (m_hasChangedFunction != null && m_hasChangedFunction(m_previousValue, CurrentValue))
                        {
                            onValueChanged(CurrentValue);
                        }
                        m_previousValue = CurrentValue;
                    }
                }

                // TODO : Move this to Object type definition.
                [SerializeField]
                public ObjectPawnUIModule[] pawnUIModules;
                [SerializeField]
                private Actor actor;
                [SerializeField]
                private bool selectable = true;

                private List<DataWatcher_Base> m_dataWatchers;
                
                


                protected ObjectPawnsManager PawnsManager
                {
                    get { return ObjectPawnsManager.Instance; }
                }
                public uint ObjectID { get; private set; }
                public int OwnerID { get { return m_objectOwnerDataWatcher.CurrentValue.ownerID; } }
                public bool ControlledByLocalPlayer { get { return OwnerID == PawnsManager.LocalPlayerID; } }

                public bool Selectable { get { return selectable; } }

                protected DataWatcher<OBJECT_DATA_TRANSFORM> m_objectTransformDataWatcher;
                protected DataWatcher<OBJECT_DATA_OWNER> m_objectOwnerDataWatcher;
                protected DataWatcher<OBJECT_DATA_WEAPON> m_objectWeaponDataWatcher;

                public void UpdatePawn()
                {
                    foreach (var watcher in m_dataWatchers) watcher.Watch();


                    transform.position = m_objectTransformDataWatcher.CurrentValue.position;
                    OnPawnUpdate();


                    foreach (var uiModule in pawnUIModules)
                    {
                        uiModule.UpdatePositionOnScreen(transform.position, Vector2.up * 50);
                    }
                }

                public bool RegisterDataWatcher<T>(out DataWatcher<T> watcher, Func<T, T, bool> hasChangedFunc = null) where T : unmanaged
                {
                    var memoryManager = GameClient.Instance.GetManager<LocalMatchManager>().LocalMatch.Managers.Get<ObjectMemoryManager>();

                    T* ptr;
                    if (GetPointerToData(memoryManager, out ptr))
                    {
                        watcher = new DataWatcher<T>(ptr, hasChangedFunc);

                        m_dataWatchers.Add(watcher);
                        return true;
                    }
                    else
                    {
                        watcher = null;
                        return false;
                    }
                }

                private bool GetPointerToData<T>(ObjectMemoryManager memoryManager, out T* ptr) where T : unmanaged
                {
                    ptr = null;
                    uint dataID = memoryManager.Objects[(int)ObjectID].GetDataSlotIDForType<T>();
                    if (dataID == uint.MaxValue)
                    {
                        return false;
                    }

                    fixed (T* dataPtr = &memoryManager.GetDataContainer<T>().Data[dataID])
                    {
                        ptr = dataPtr;
                    }
                    return true;
                }

                protected T GetPawnUIModule<T>() where T : ObjectPawnUIModule
                {
                    foreach(var mod in pawnUIModules)
                    {
                        if (mod is T)
                        {
                            return (T)mod;
                        }
                    }
                    return null;
                }

                public virtual void LinkToGameData(ObjectMemoryManager memoryManager, uint id)
                {
                    m_dataWatchers = new List<DataWatcher_Base>();

                    ObjectID = id;

                    RegisterDataWatcher(out m_objectTransformDataWatcher);
                    RegisterDataWatcher(out m_objectOwnerDataWatcher);
                    
                    if (RegisterDataWatcher(out m_objectWeaponDataWatcher, HasWeaponDataChanged))
                    {
                        m_objectWeaponDataWatcher.onValueChanged += OnWeaponUsageChanged;
                    }

                    LinkToExtraGameData(memoryManager);
                }
                public void InitializePawnUIModules()
                {
                    foreach(var uiModule in pawnUIModules)
                    {
                        uiModule.Initialize(this);
                    }
                }

                public virtual void AssignActor(Actor actor)
                {
                    if (actor != null)
                    {
                        if (this.actor != null)
                        {
                            Destroy(this.actor.gameObject);
                        }
                        this.actor = Instantiate(actor.gameObject).GetComponent<Actor>();
                        this.actor.transform.parent = transform;
                    }
                }

                protected void TriggerActorEvent(ACTOR_EVENT eventName)
                {
                    if (actor != null) actor.ProcessEvent(eventName);
                }

                public void SetSelected(bool selected)
                {
                    if (!selectable) return;
                    if (selected)
                    {
                        if (ControlledByLocalPlayer)
                            TriggerActorEvent(ACTOR_EVENT.SELECTED_FRIENDLY);
                        else
                            TriggerActorEvent(ACTOR_EVENT.SELECTED_ENEMY);
                    }
                    else
                    {
                        TriggerActorEvent(ACTOR_EVENT.DESELECTED);
                    }
                }

                public void Kill()
                {
                    // Cleanup all linked Pawn UI Modules.
                    foreach (var mod in pawnUIModules) Destroy(mod.gameObject);
                    OnDeath();
                }

                protected virtual void OnDeath()
                {
                    // By default destroy this Pawn's gameobject.
                    Destroy(gameObject);
                }

                protected abstract void LinkToExtraGameData(ObjectMemoryManager memoryManager);
                protected abstract void OnPawnUpdate();


                private bool HasWeaponDataChanged(OBJECT_DATA_WEAPON previous, OBJECT_DATA_WEAPON current)
                {
                    return (previous.usingWeapon != current.usingWeapon);
                }

                private void OnWeaponUsageChanged(OBJECT_DATA_WEAPON weaponData)
                {
                    if (weaponData.usingWeapon)
                    {
                        TriggerActorEvent(ACTOR_EVENT.ATTACK);
                    }
                }
            }
        }

    }
}


