using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            /// <summary>
            /// Global module spawned whenever we expect to get Pawns spawned with their own UI Modules.
            /// Any newly spawned <see cref="ObjectPawnUIModule"/> will have the Instance (Singleton)
            /// be its parent.
            /// </summary>
            public class ObjectPawnUIRootUIModule : UIModule
            {
                static public ObjectPawnUIRootUIModule Instance { get; private set; }
                static public Transform CurrentRoot { get { return Instance.transform; } }

                public Camera ObjectPawnUICamera
                {
                    get; private set;
                }

                public void SetCamera(Camera cam)
                {
                    if (ObjectPawnUICamera != null) throw new System.Exception("ERROR : Attempted to set ObjectPawnUIRoot module's camera more than once in its lifetime !");
                    ObjectPawnUICamera = cam;
                }

                private void Awake()
                {
                    // Check that we can take the spot of unique Instance.
                    if (Instance == null)
                    {
                        Instance = this;
                    }
                    else
                    {
                        Debug.LogError("ERROR : ObjectPawnnUIRoot module instance was already created ! Destroying new instance...");
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}


