using NeoRTS.Communication;
using NeoRTS.GameData.CellSystem;
namespace NeoRTS
{
    namespace GameData
    {
        /// <summary>
        /// Manages the grid that is responsible for storing up to date, spatially coherent IDs of objects so that we
        /// are able to cheaply query what objects are in a certain area.
        /// </summary>
        public class ObjectPositionGridManager : MatchManagerObject
        {
            private const int MAX_UNIT_PER_CELL = 10;

            private CellGrid m_cellGrid;
            private int m_cellSize;
            private Cell.Coordinates m_maxCoordinates;

            public CellGrid CellGrid
            {
                get { return m_cellGrid; }
            }

            public ObjectPositionGridManager(bool authoritative, int gridCellSize, int gridCellCount, uint maxUnitCount) : base(authoritative)
            {
                m_cellGrid = new CellGrid(maxUnitCount, gridCellCount);
                m_cellSize = gridCellSize;
                m_maxCoordinates = new Cell.Coordinates(gridCellCount - 1, gridCellCount - 1);
            }

            public Cell.Coordinates GetCellCoordinatesFromPosition(Position pos)
            {
                int x = (int)(pos.x / m_cellSize);
                int z = (int)(pos.z / m_cellSize);

                if (x < 0)
                {
                    x = 0;
                }
                else if (x > m_maxCoordinates.x)
                {
                    x = m_maxCoordinates.x;
                }

                if (z < 0)
                {
                    z = 0;
                }
                else if (z > m_maxCoordinates.y)
                {
                    z = m_maxCoordinates.y;
                }

                return new Cell.Coordinates(x, z);
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
            }

            protected override void OnManagerInitialize()
            {
            }

            protected override void OnManagerUpdate(float deltaTime)
            {

            }
        }
    }
}

