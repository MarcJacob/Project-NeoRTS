using UnityEngine;
using System;
using NeoRTS.GameData;
using NeoRTS.Communication;

namespace NeoRTS
{
    namespace Client
    {


        /// <summary>
        /// Centralizes the collection of input from peripheral electronics such as Keyboard, mouse...
        /// (FOR NOW) Works by being updated every frame and updating values which drive the call of input Events
        /// other game systems may subscribe to.
        /// (TODO : Switch to the modern Unity Input system).
        /// </summary>
        public class InputManager : ManagerObject
        {
            /// <summary>
            /// Passes screen position of the click.
            /// </summary>
            public event Action<Vector2> onLeftMouseDown = delegate { };
            /// <summary>
            /// Passes screen position of the click.
            /// </summary>
            public event Action<Vector2> onRightMouseClick = delegate { };
            /// <summary>
            /// Passes screen position of the mouse.
            /// </summary>
            public event Action<Vector2> onLeftMouseHold = delegate { };
            public event Action onLeftMouseUp = delegate { };

            /// <summary>
            /// Passes Vertical axis and Horizontal axis (in that order).
            /// </summary>
            public event Action<float, float> onMovementAxis = delegate { };

            protected override void OnManagerUpdate(float deltaTime)
            {
                float vertical, horizontal;

                vertical = Input.GetAxis("Vertical");
                horizontal = Input.GetAxis("Horizontal");

                
                bool rightClicked = Input.GetMouseButtonDown(1);

                bool leftClicked = Input.GetMouseButtonDown(0);
                bool leftHeld = !leftClicked && Input.GetMouseButton(0);
                bool leftUp = Input.GetMouseButtonUp(0);

                if (vertical != 0f || horizontal != 0f)
                {
                    onMovementAxis(vertical, horizontal); // (TODO : consider calling this event every time regardless of if vertical & horizontal are 0.
                }

                if (leftClicked)
                {
                    onLeftMouseDown(Input.mousePosition);
                }
                else if (leftHeld)
                {
                    onLeftMouseHold(Input.mousePosition);
                }
                else if (leftUp)
                {
                    onLeftMouseUp();
                }
                else if (rightClicked) onRightMouseClick(Input.mousePosition);
            }

            protected override void OnManagerInitialize()
            {
                
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {

            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {

            }
        }
    }
}


