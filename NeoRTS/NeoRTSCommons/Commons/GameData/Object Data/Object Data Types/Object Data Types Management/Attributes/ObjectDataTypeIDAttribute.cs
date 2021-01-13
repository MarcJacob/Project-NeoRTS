using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            public class ObjectDataTypeIDAttribute : Attribute
            {
                private byte m_id;
                public byte ID { get { return m_id; } set { if (m_id == 0) m_id = value; else throw new Exception("Error : Attempted to reassign ID of Object Data Type ID Attribute."); } }
                public ObjectDataTypeIDAttribute(byte id)
                {
                    m_id = id;
                }
            }

            public class ObjectTypeReferenceAttribute : Attribute
            {
            }
        }
    }
}

