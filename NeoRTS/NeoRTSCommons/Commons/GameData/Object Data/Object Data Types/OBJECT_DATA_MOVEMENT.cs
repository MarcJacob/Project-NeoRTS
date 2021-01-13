using System;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_MOVEMENT : IEquatable<OBJECT_DATA_MOVEMENT>
            {
                public const float OBJECT_SPEED_PER_SECOND = 2f; // TODO Load that from game data
                public bool Moving { get { return movementVector != Position.Zero; } }
                public Position movementVector;
                public Position movementVectorNormalized;

                public void SetMovementTarget(Position moveTarget, Position currentPosition)
                {
                    movementVector = moveTarget - currentPosition;
                    movementVectorNormalized = movementVector.GetNormalized();
                }

                public void StopMoving()
                {
                    movementVector = Position.Zero;
                    movementVectorNormalized = Position.Zero;
                }

                public bool Equals(OBJECT_DATA_MOVEMENT other)
                {
                    return Position.IsEqualToWithEpsilon(movementVector, other.movementVector, 0.01f);
                }
            }
        }
    }
}

