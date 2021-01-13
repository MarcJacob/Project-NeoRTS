using NeoRTS.GameData.CellSystem;
using System;
using System.Runtime.InteropServices;
#if UNITY_STANDALONE
#endif

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_CELL_COORDS
            {
                public OBJECT_DATA_CELL_COORDS(int x, int y)
                {
                    Coords.x = x;
                    Coords.y = y;
                    placed = true;
                }
                public OBJECT_DATA_CELL_COORDS(Cell.Coordinates coords)
                {
                    Coords = coords;
                    placed = true;
                }
                public Cell.Coordinates Coords;
                public bool placed;

                public float x { get { return Coords.x; } }
                public float y { get { return Coords.y; } }
            }
        }
    }
}

