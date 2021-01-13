using UnityEngine;
using NeoRTS.Client.Pawns;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            /// <summary>
            /// Subtype of <see cref="UIModule"/> that defines a usually small module that is linked to a specific
            /// instance of a <see cref="ObjectPawnComponent"/>. The Pawn is then able to call specific setter functions
            /// to update the displayed data about itself on screen.
            /// The only setter function the base type defines is a function that gives the UI Module a chance
            /// to update its position on screen, by default simply setting itself to follow the object pawn's position
            /// on screen.
            /// 
            /// Also sets itself as child of the ObjectPawnUIRoot in its Awake function.
            /// </summary>
            public abstract class ObjectPawnUIModule : UIModule
            {
                // TODO Consider adding a property that drives whether this module instance should be invisible
                // if off screen. That way we can add elements that just hug the edge of the screen instead of disappearing.
                private void Awake()
                {
                    transform.SetParent(ObjectPawnUIRootUIModule.CurrentRoot);
                }

                public abstract void Initialize(ObjectPawnComponent linkedPawnComponent);

                public virtual void UpdatePositionOnScreen(Vector3 pawnWorldPosition, Vector3 offset = default(Vector3))
                {
                    var cam = ObjectPawnUIRootUIModule.Instance.ObjectPawnUICamera;

                    Vector2 screenPosition = cam.WorldToScreenPoint(pawnWorldPosition) + offset;
                    Vector2 screenDimensions = new Vector2(cam.pixelWidth, cam.pixelHeight);
                    transform.position = screenPosition;
                    bool visible = true;
                    if (screenPosition.x < 0 || screenPosition.x > screenDimensions.x)
                    {
                        visible = false;
                    }
                    else if (screenPosition.y < 0 || screenPosition.y > screenDimensions.y)
                    {
                        visible = false;
                    }
                    SetVisible(visible);
                }

                public void SetVisible(bool visible)
                {
                    if (gameObject.activeSelf != visible)
                    {
                        gameObject.SetActive(visible);
                    }
                }
            }
        }
    }
}


