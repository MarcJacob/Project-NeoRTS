namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            namespace Types
            {
                /// <summary>
                /// Defines a set of data types that an object contains upon being spawned with
                /// this archetype. Also defines their default values.
                /// </summary>
                public class ObjectArchetype
                {
                    private ObjectDataTypeDef[] m_archetypeDataDefs;

                    public ObjectDataTypeDef[] ArchetypeDataDefs { get { return m_archetypeDataDefs; } }

                    public ObjectArchetype(params ObjectDataTypeDef[] defs)
                    {
                        m_archetypeDataDefs = defs;
                    }

                    public bool GetDefaultArchetypeDataForType<T>(out T data) where T : unmanaged
                    {
                        foreach(var def in m_archetypeDataDefs)
                        {
                            if (def.DataType == typeof(T))
                            {
                                data = def.DefaultValues<T>();
                                return true;
                            }
                        }

                        data = default(T);
                        return false;
                    }
                }
            }
        }
    }
}

