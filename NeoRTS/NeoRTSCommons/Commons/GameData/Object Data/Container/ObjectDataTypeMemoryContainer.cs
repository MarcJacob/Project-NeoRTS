using NeoRTS.Communication.Messages;
using System.Collections.Generic;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            /// <summary>
            /// Base class for all types of the generic class <see cref="ObjectDataTypeMemoryContainer{T}"/>
            /// </summary>
            public abstract class ObjectDataTypeMemoryContainer_Base
            {
                /// <summary>
                /// Takes the event data, casts it to the underlying type this container handles and potentially changes its data
                /// accordingly. If this returns true, it means that the event was "accepted" and impacted the underlying data.
                /// </summary>
                public unsafe abstract bool ProcessEventMessage(OBJECT_DATA_CHANGE_EVENT_DATA eventObject, IEnumerable<uint> slotIDs);

                public abstract void ClearSlot(uint id);
                public abstract uint AllocateSlot();
                public unsafe abstract uint AllocateSlotWithData(object data);
            }

            /// <summary>
            /// Contains a certain type of unmanaged data that implements the IObjectData interface.
            /// Upon construction, builds an internal array of that data type. Access to it is allowed
            /// in a read only fashion (NOTE : the array itself, its values are modifiable !).
            /// 
            /// It is also able to process a DataChangeEvent.
            /// </summary>
            public class ObjectDataTypeMemoryContainer<T> : ObjectDataTypeMemoryContainer_Base where T : unmanaged
            {
                public ObjectDataTypeMemoryContainer(uint objectCount)
                {
                    m_data = new T[objectCount];
                    m_freeDataSlots = new List<uint>((int)objectCount);
                    for(uint i = 0; i < objectCount; i++)
                    {
                        m_freeDataSlots.Add(i);
                    }
                }

                public T[] Data { get { return m_data; } }
                private T[] m_data;

                private List<uint> m_freeDataSlots;

                public override unsafe bool ProcessEventMessage(OBJECT_DATA_CHANGE_EVENT_DATA eventObject, IEnumerable<uint> slotIDs)
                {
                    T data;
                    try
                    {
                        fixed(byte* eventData = eventObject.data)
                        data = *(T*)eventData;
                    }
                    catch
                    {
                        throw new System.Exception("ERROR : Passed data change object is not of the correct type !");
                    }

                    // TODO : Add a "filter" system to be able to run sanity checks on the data if we are authoritative.

                    foreach(var id in slotIDs)
                    {
                        m_data[id] = data;
                    }

                    return true;
                }

                public override void ClearSlot(uint dataSlotID)
                {
                    m_data[dataSlotID] = default(T);
                    m_freeDataSlots.Add(dataSlotID);
                }

                public override uint AllocateSlot()
                {
                    if (m_freeDataSlots.Count > 0)
                    {
                        uint slot = m_freeDataSlots[0];
                        m_freeDataSlots.RemoveAt(0);
                        return slot;
                    }
                    else
                    {
                        throw new System.Exception("Out of memory on container type " + GetType().Name + " !");
                    }
                }

                public uint AllocateSlotWithData(T data)
                {
                    uint slot = AllocateSlot();
                    Data[slot] = data;
                    return slot;
                }

                public unsafe override uint AllocateSlotWithData(object data)
                {
                    try
                    {
                        return AllocateSlotWithData((T)data);
                    }
                    catch
                    {
                        throw new System.Exception("ERROR : Passed data doesn't match Type !");
                    }
                }
            }
        }
    }
}

