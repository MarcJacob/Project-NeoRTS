#if UNITY_STANDALONE
#endif

using System;
using System.Runtime.InteropServices;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {

            [ObjectDataTypeID(0)]
            [Serializable]
            public struct OBJECT_DATA_WEAPON
            {
                public const float OBJECT_WEAPON_COOLDOWN = 1f;
                public const float OBJECT_WEAPON_WINDUP = 0.2f;
                public const int OBJECT_WEAPON_DAMAGE = 10;

                public float currentCooldown;
                public float currentWindup;
                public bool usingWeapon; // True when unit is winding up to attack.
                public float weaponRange;
            }
        }
    }
}

