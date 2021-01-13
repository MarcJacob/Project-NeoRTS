using NeoRTS.Client.Misc;
using NeoRTS.Client.Pawns;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace Misc
        {
            /// <summary>
            /// UnitSelector is a static class that contains a set of Utility functions that allow a Client app to easily
            /// determine what unit(s) should be selected from a specific type of data such as Screen position.
            /// </summary>
            public static class UnitSelector
            {
                // TODO : Change that to a radius of selection that depends on a unit's size.
                private const float SELECTION_DISTANCE = 1.5f;

                static public bool TrySelectUnitAtScreenPosition(Vector2 screenPosition, out ObjectPawnComponent selected)
                {
                    // TODO This is a disgusting function, remove it you absolute doughnut
                    var allUnitPawns = GameObject.FindObjectsOfType<ObjectPawnComponent>();

                    Vector3 selectPos = Misc.CameraTools.GetTerrainPositionFromScreenPosition(screenPosition);

                    ObjectPawnComponent closestUnitPawn = null;
                    float squaredClosestDist = 0f;
                    foreach (var pawn in allUnitPawns)
                    {
                        float squaredDist = Vector3.SqrMagnitude(pawn.transform.position - selectPos);
                        if (closestUnitPawn == null || squaredDist < squaredClosestDist)
                        {
                            closestUnitPawn = pawn;
                            squaredClosestDist = squaredDist;
                        }
                    }

                    if (closestUnitPawn != null && squaredClosestDist < SELECTION_DISTANCE * SELECTION_DISTANCE)
                    {
                        selected = closestUnitPawn;
                        return true;
                    }
                    else
                    {
                        selected = null;
                        return false;
                    }
                }

                static public bool TrySelectUnitsInBox(Rect screenRectangle, out ObjectPawnComponent[] selected)
                {
                    Vector2 screenCorner1, screenCorner2;

                    screenCorner1 = new Vector2(screenRectangle.x, screenRectangle.y);
                    screenCorner2 = new Vector2(screenRectangle.x + screenRectangle.width, screenRectangle.y + screenRectangle.height);

                    Vector3 screenCorner1WorldPos, screenCorner2WorldPos;
                    screenCorner1WorldPos = CameraTools.GetTerrainPositionFromScreenPosition(screenCorner1);
                    screenCorner2WorldPos = CameraTools.GetTerrainPositionFromScreenPosition(screenCorner2);

                    Rect worldPosSelectionRect = new Rect(screenCorner1WorldPos.x, screenCorner1WorldPos.z, screenCorner2WorldPos.x - screenCorner1WorldPos.x, screenCorner2WorldPos.z - screenCorner1WorldPos.z);

                    // TODO This is a disgusting function, remove it you absolute doughnut
                    var allUnitPawns = GameObject.FindObjectsOfType<ObjectPawnComponent>();

                    List<ObjectPawnComponent> selectedList = new List<ObjectPawnComponent>();

                    foreach(var pawn in allUnitPawns)
                    {
                        Vector2 pawn2DPosition = new Vector2(pawn.transform.position.x, pawn.transform.position.z);
                        if (worldPosSelectionRect.Contains(pawn2DPosition, true))
                        {
                            selectedList.Add(pawn);
                        }
                    }


                    if (selectedList.Count > 0)
                    {
                        selected = selectedList.ToArray();
                        return true;
                    }
                    else
                    {
                        selected = null;
                        return false;
                    }
                }
            }
        }
    }
}
