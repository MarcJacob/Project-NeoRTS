using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NeoRTS
{
    namespace GameData
    {

        /// <summary>
        /// In-house equivalent of Unity3D's Vector3 structure. Contains 3 floating point attributes that are most
        /// commonly used to represent a 3-dimensional vector.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Position
        {
            static public Position Zero { get; private set; } = new Position(0, 0, 0);

            public Position(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public float x;
            public float y;
            public float z;

            #region UNITY_DEPENDENT

            public Position GetNormalized()
            {
                Vector3 vec3 = this;
                return vec3.normalized;
            }

            static public implicit operator Vector3(Position pos)
            {
                return new Vector3(pos.x, pos.y, pos.z);
            }

            static public implicit operator Position(Vector3 pos)
            {
                return new Position(pos.x, pos.y, pos.z);
            }

            #endregion

            static public Position operator -(Position a, Position b)
            {
                return new Position(a.x - b.x, a.y - b.y, a.z - b.z);
            }

            static public Position operator +(Position a, Position b)
            {
                return new Position(a.x + b.x, a.y + b.y, a.z + b.z);
            }

            static public Position operator *(Position a, float f)
            {
                return new Position(a.x * f, a.y * f, a.z * f);
            }

            static public bool operator ==(Position a, Position b)
            {
                return a.x == b.x && a.y == b.y && a.z == b.z;
            }

            static public bool operator !=(Position a, Position b)
            {
                return !(a == b);
            }

            /// <summary>
            /// Returns the squared distance between two positions. Use that for distance related comparisons and calculations preferably.
            /// </summary>
            public static float SquaredDistance(Position a, Position b)
            {
                return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
            }

            /// <summary>
            /// Returns the distance between two positions. WARNING : Costly operation ! Contains a square root + a double -> float cast.
            /// Only use if STRICTLY necessary. Always consider using <see cref="SquaredDistance(Position, Position)"></see>.
            /// </summary>
            public static float Distance(Position a, Position b)
            {
                return (float)Math.Sqrt(SquaredDistance(a, b));
            }

            public static bool IsEqualToWithEpsilon(Position a, Position b, float epsilon)
            {
                return Math.Abs(a.x - b.x) < epsilon && Math.Abs(a.y - b.y) < epsilon && Math.Abs(a.z - b.z) < epsilon;
            }
        }
    }
}

