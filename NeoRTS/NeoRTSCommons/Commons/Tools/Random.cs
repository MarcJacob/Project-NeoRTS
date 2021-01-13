namespace NeoRTS
{
    namespace Tools
    {

        /// <summary>
        /// Custom Random library as to not rely on Unity's. For now completely static. Relies on an internal System.Random object.
        /// This was created because UnityEngine static classes like UnityEngine.Random can only be called from within a
        /// Unity app which broke the Server.
        /// </summary>
        public static class Random
        {
            static private bool Initialized = false;
            static private System.Random InternalRandom;
            static public void Initialize(int seed)
            {
                if (Initialized) throw new System.Exception("ERROR : NeoRTS.Tools.Random class is already initialized !");
                Initialized = true;
                InternalRandom = new System.Random(seed);
            }
            static public void Initialize()
            {
                if (Initialized) throw new System.Exception("ERROR : NeoRTS.Tools.Random class is already initialized !");
                Initialized = true;
                InternalRandom = new System.Random();
            }

            static private void CheckInitialized()
            {
                if (!Initialized) throw new System.Exception("ERROR : Attempted to use NeoRTS.Tools.Random class without initializing it !");
            }

            /// <summary>
            /// Returns a random value between min (inclusive) and max (inclusive).
            /// </summary>
            static public float Range(float min, float max)
            {
                CheckInitialized();

                float step = (max - min) / int.MaxValue;
                int stepCount = InternalRandom.Next();
                return min + stepCount * step;
            }

            /// <summary>
            /// Returns a random value between min (inclusive) and max (exclusive)
            /// </summary>
            static public int Range(int min, int max)
            {
                float floatVal = Range((float)min, ((float)max) - float.Epsilon);

                return (int)floatVal;
            }
        }
    }
}
