using NeoRTS.GameData;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace Misc
        {
            public static class CameraTools
            {
                static public Position GetTerrainPositionFromScreenPosition(Vector2 screenPosition)
                {
                    Plane groundPlane = new Plane(Vector3.up, 0f);
                    // TODO : Don't use Camera.main ! Whenever we implement a CameraManager, use that to give us the main "Game view camera".
                    Ray ray = Camera.main.ScreenPointToRay(screenPosition);

                    float dist;
                    if (groundPlane.Raycast(ray, out dist))
                    {
                        return ray.GetPoint(dist);
                    }
                    else
                    {
                        throw new System.Exception("ERROR - Attempted to get Terrain position of a ray parallel or casting away from terrain plane.");
                    }
                }
            }
        }
    }
}
