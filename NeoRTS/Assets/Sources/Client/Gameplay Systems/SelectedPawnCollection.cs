using NeoRTS.Client.Pawns;
using NeoRTS.GameData;
using NeoRTS.GameData.ObjectData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// SelectedPawnCollection is a wrapper for a HashSet of UnitPawns.
        /// It can be interacted with by either deselecting every pawn selected, by asking it to select a specific pawn
        /// or to run the multi selection algorithm on a set of pawns.
        /// 
        /// TODO : Add ability to deselect a specific pawn OR have the collection react to one of its pawns being destroyed.
        /// </summary>
        public class SelectedPawnCollection
        {
            private HashSet<ObjectPawnComponent> m_selectedPawns;
            private int m_localPlayerID;
            public int SelectedUnitCount
            {
                get { return m_selectedPawns.Count; }
            }

            public SelectedPawnCollection()
            {
                m_selectedPawns = new HashSet<ObjectPawnComponent>();
                m_localPlayerID = -1000;
            }

            public void SetLocalPlayerID(int localPlayerID)
            {
                // TODO : If we currently have units selected then deselect them all except for those we can still control.

                m_localPlayerID = localPlayerID;
            }

            public int SelectAllPawns(IEnumerable<ObjectPawnComponent> selectedPawns, bool conservePreviousSelection)
            {
                return SelectPawnsConditional(selectedPawns, conservePreviousSelection, (p) => { return true; });
            }

            public int SelectPawnsOwnedBy(IEnumerable<ObjectPawnComponent> selectedPawns, bool conservePreviousSelection, int ownerID)
            {
                return SelectPawnsConditional(selectedPawns, conservePreviousSelection, (p) => { return p.OwnerID == ownerID; });
            }

            public int SelectPawnsConditional(IEnumerable<ObjectPawnComponent> selectedPawns, bool conservePreviousSelection, Func<ObjectPawnComponent, bool> selectionTestFunc)
            {
                List<ObjectPawnComponent> finalPawnSelection = new List<ObjectPawnComponent>();
                foreach(var p in selectedPawns)
                {
                    if (selectionTestFunc(p)) finalPawnSelection.Add(p);
                }

                if (finalPawnSelection.Count > 0)
                {
                    if (conservePreviousSelection == false) DeselectAllPawns();
                    foreach(var p in finalPawnSelection)
                    {
                        SelectSinglePawn(p, true);
                    }
                }
                return finalPawnSelection.Count;
            }

            public void SelectSinglePawn(ObjectPawnComponent pawn, bool keepPreviousSelection = false)
            {
                if (pawn.Selectable == false) return;
                if (keepPreviousSelection)
                {
                    if (m_selectedPawns.Contains(pawn) == false)
                    {
                        m_selectedPawns.Add(pawn); // TODO Run checks to make sure we're not selecting something we shouldn't be able to like an enemy unit
                        FireOnPawnSelectedEvent(pawn);
                    }
                    else
                    {
                        m_selectedPawns.Remove(pawn);
                        pawn.SetSelected(false);
                    }
                }
                else
                {
                    DeselectAllPawns();
                    m_selectedPawns.Add(pawn);
                    FireOnPawnSelectedEvent(pawn);
                }
            }

            private void FireOnPawnSelectedEvent(ObjectPawnComponent pawn)
            {
                pawn.SetSelected(true);
            }

            public void DeselectAllPawns()
            {
                foreach(var pawn in m_selectedPawns)
                {
                    pawn.SetSelected(false);
                }
                m_selectedPawns.Clear();
            }

            public uint[] RetrieveSelectedUnitIDs()
            {
                uint[] selectedUnitIDs = new uint[m_selectedPawns.Count];

                int index = 0;
                foreach(var pawn in m_selectedPawns)
                {
                    selectedUnitIDs[index] = pawn.ObjectID;
                    index++;
                }

                return selectedUnitIDs;
            }
        }
    }
}


