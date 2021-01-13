using System.Collections.ObjectModel;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            namespace Types
            {
                /// <summary>
                /// An Object Type defines a unique link between an <see cref="ObjectArchetype"/>,
                /// an Actor, and a Pawn Type aswell as a name. This is the full set of data that
                /// both the client and server need to properly spawn and manage an object that is
                /// "game ready" and pre-designed.
                /// 
                /// Since Actors and Pawn Types are not a thing and in themselves unneeded on the Server,
                /// they are defined as an ID that is only pertinent on the Client. The Archetype is only
                /// used at construction to build the final set of <see cref="ObjectDataTypeDef"/> this object
                /// type will have upon being spawned.
                /// </summary>
                public class ObjectType
                {
                    private ObjectDataTypeDef[] m_dataTypeDefs;
                    private int m_actorID;
                    private int m_pawnTypeID;
                    private string m_name;

                    public ReadOnlyCollection<ObjectDataTypeDef> DataTypeDefs
                    {
                        get { return new ReadOnlyCollection<ObjectDataTypeDef>(m_dataTypeDefs); }
                    }

                    public int ActorID
                    {
                        get { return m_actorID; }
                    }

                    public int PawnTypeID
                    {
                        get { return m_pawnTypeID; }
                    }

                    public string Name
                    {
                        get { return m_name; }
                    }

                    public ObjectType(ObjectArchetype archetype, ObjectDataTypeDef[] overrides, int actorID, int pawnTypeID, string name)
                    {
                        m_actorID = actorID;
                        m_name = name;
                        m_pawnTypeID = pawnTypeID;
                        m_dataTypeDefs = archetype.ArchetypeDataDefs.Overlap(overrides);
                    }
                }
            }
        }
    }
}

