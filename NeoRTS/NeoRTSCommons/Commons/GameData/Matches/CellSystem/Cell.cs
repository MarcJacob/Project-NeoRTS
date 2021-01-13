using System.Runtime.InteropServices;
namespace NeoRTS
{
    namespace GameData
    {
        namespace CellSystem
        {
            /// <summary>
            /// A Cell contains a definition for a "block" of some collective memory that can be dynamically
            /// resized to make room for neighbouring cells within some kind of cell container system (See <see cref="CellGrid"/>).
            /// It contains a position within that collective memory in the form of an index.
            /// Likewise it contains the total size of its "owned" memory and within that owned memory, how many slots are "free"
            /// to be taken by other cells.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct Cell
            {
                public struct Coordinates
                {
                    public Coordinates(int x, int y)
                    {
                        this.x = x;
                        this.y = y;
                    }
                    public int x, y;

                    static public Coordinates operator +(Coordinates a, Coordinates b)
                    {
                        return new Coordinates(a.x + b.x, a.y + b.y);
                    }

                    static public Coordinates operator -(Coordinates a, Coordinates b)
                    {
                        return new Coordinates(a.x - b.x, a.y - b.y);
                    }

                    static public Coordinates operator *(Coordinates target, float mult)
                    {
                        return new Coordinates((int)(target.x * mult), (int)(target.y * mult));
                    }

                    static public bool operator ==(Coordinates a, Coordinates b)
                    {
                        return a.x == b.x && a.y == b.y;
                    }

                    static public bool operator !=(Coordinates a, Coordinates b)
                    {
                        return a.x != b.x || a.y != b.y;
                    }
                }

                // Size of owned memory in number of IDs (each are 4 byte)
                public int TotalOwnedSlots
                {
                    get { return (int)(usedSlots + freeSlots); }
                }

                public int ownedIDMemoryChunkIndex;
                public int usedSlots;

                // Size of the owned memory chunk that contains unused IDs.
                public int freeSlots;
            }
        }
    }
}

