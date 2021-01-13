using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {

        /// <summary>
        /// Adds Classical-RTS-Style movement to the associated camera. (TODO : Move Input code away)
        /// (TODO : Add zoom / dezoom feature and maybe rotation).
        /// </summary>
        public class RTSCameraController : MonoBehaviour
        {
            [SerializeField]
            private float speed;
            [SerializeField]
            private bool scrollingMovementActivated = false;
            void Start()
            {
                Initialize();
            }

            public void Initialize()
            {
                var inputManager = GameClient.Instance.GetManager<InputManager>();
                inputManager.onMovementAxis += OnMovementAxis;

                Cursor.lockState = CursorLockMode.Confined;
            }

            public void OnDestroy()
            {
                GameClient.Instance.GetManager<InputManager>().onMovementAxis -= OnMovementAxis;
            }

            private void OnMovementAxis(float vertical, float horizontal)
            {
                transform.Translate(horizontal * speed * Time.deltaTime, 0f, vertical * speed * Time.deltaTime, Space.World);
            }

            void Update()
            {
                #region TO_IMPLEMENT
                /*
                Vector2 screenDimensions = new Vector2(Screen.width, Screen.height);
                if (Input.GetButton("Fire3"))
                {
                    Cursor.visible = false;


                    float xMovement = Input.GetAxis("Mouse X");
                    float yMovement = Input.GetAxis("Mouse Y");

                    transform.Translate(xMovement * speed * Time.deltaTime, 0f, yMovement * speed * Time.deltaTime, Space.World);
                }
                else if (scrollingMovementActivated)
                {
                    Cursor.visible = true;

                    Vector2 cursorPos = Input.mousePosition;
                    if (cursorPos.x > screenDimensions.x * 0.9f)
                    {
                        transform.Translate(speed * Time.deltaTime, 0f, 0f, Space.World);
                    }
                    else if (cursorPos.x < screenDimensions.x * 0.1f)
                    {
                        transform.Translate(-speed * Time.deltaTime, 0f, 0f, Space.World);
                    }
                    if (cursorPos.y > screenDimensions.y * 0.9f)
                    {
                        transform.Translate(0f, 0f, speed * Time.deltaTime, Space.World);
                    }
                    else if (cursorPos.y < screenDimensions.y * 0.1f)
                    {
                        transform.Translate(0f, 0f, -speed * Time.deltaTime, Space.World);
                    }
                }*/
                #endregion
            }
        }
    }
}