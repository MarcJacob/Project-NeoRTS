using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            namespace Types
            {
                /// <summary>
                /// Defines a type of data an object can contain upon being spawned along with
                /// its default value.
                /// Used by <see cref="ObjectArchetype"/> and <see cref="ObjectType"/>.
                /// </summary>
                public struct ObjectDataTypeDef
                {
                    public ObjectDataTypeDef(Type dataType, object defaultValue)
                    {
                        DataType = dataType;
                        m_defaultValue = defaultValue;
                    }

                    public Type DataType { get; private set; }
                    private object m_defaultValue;

                    public object DefaultValue
                    {
                        get { return m_defaultValue; }
                    }

                    public T DefaultValues<T>() where T : unmanaged
                    {
                        if (DataType != typeof(T)) throw new Exception("Error : Attempted to get Default Values for wrong type !");
                        return (T)m_defaultValue;
                    }
                }
            }
        }
    }
}

