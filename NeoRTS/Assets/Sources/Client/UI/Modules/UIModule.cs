using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            /// <summary>
            /// UIModule defines a GameObject component that is considered as a "singular" unit of UI functionnality.
            /// It allows mostly Client States to load / unload specific UI functionnality they require.
            /// 
            /// UIModules are automatically loaded by the <see cref="UIManager"/> and are accessible by type or by name or both.
            /// </summary>
            public abstract class UIModule : MonoBehaviour
            {
            }
        }
    }
}


