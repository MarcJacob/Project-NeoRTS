using NeoRTS.Client.LocalMatch;
using NeoRTS.Client.Pawns;
using NeoRTS.GameData;
using NeoRTS.Communication.Messages;
using UnityEngine;
using NeoRTS.Communication;
using NeoRTS.GameData.ObjectData;
using System;

namespace NeoRTS
{
    namespace Client
    {
        /// <summary>
        /// This Client-Side manager handles input related to Unit Selection & Control when the player is playing in
        /// RTS mode.
        /// </summary>
        public class RTSModeUnitControlManager : ManagerObject
        {
            private Vector2 m_currentSelectionStartPoint;
            private Vector2 m_currentSelectionEndPoint;

            private SelectedPawnCollection m_selectedPawns;
            private int m_localPlayerID = 0;

            // When this is true, the collection should only contain a single unit, and shift select should never
            // do anything. This can return to false when using non-shift select (box or single) and successfully selecting controllable units.
            private bool enemySelectedMode = false;

            private MatchStartedDataMessagePacker m_matchStartedMessagePacker;
            ObjectDataChangeEventMessagePacker m_orderGivenMessagePacker;
            protected override void OnManagerInitialize()
            {

                m_selectedPawns = new SelectedPawnCollection();
                var inputManager = GameClient.Instance.GetManager<InputManager>();
                inputManager.onRightMouseClick += InputManager_OnRightClick;
                inputManager.onLeftMouseDown += InputManager_OnLeftMouseDown;
                inputManager.onLeftMouseHold += InputManager_OnLeftMouseHold;
                inputManager.onLeftMouseUp += InputManager_OnLeftMouseUp;

                m_matchStartedMessagePacker = new MatchStartedDataMessagePacker();
                m_orderGivenMessagePacker = new ObjectDataChangeEventMessagePacker();
            }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.RegisterOnMessageReceivedHandler(MESSAGE_TYPE.MATCH_STARTED, OnMatchStartedDataMessageReceived);
            }


            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            {
                dispatcher.UnregisterOnMessageReceivedHandler(OnMatchStartedDataMessageReceived);
            }

            protected override void OnManagerUpdate(float deltaTime)
            {
            }

            private void SelectUnitsWithCurrentSelectionZone()
            {
                // TODO : Move this to InputManager and use a globally readable property called "ShiftMode".
                bool shiftSelect = Input.GetKey(KeyCode.LeftShift);
                if (shiftSelect && enemySelectedMode) return;


                if (Vector2.Distance(m_currentSelectionStartPoint, m_currentSelectionEndPoint) < 50)
                {
                    ObjectPawnComponent selected;
                    if (Misc.UnitSelector.TrySelectUnitAtScreenPosition(m_currentSelectionStartPoint, out selected))
                    {
                        m_selectedPawns.SelectSinglePawn(selected, shiftSelect);

                        enemySelectedMode = selected.OwnerID != m_localPlayerID;
                    }
                }
                else
                {
                    ObjectPawnComponent[] selectedUnits;
                    if (Misc.UnitSelector.TrySelectUnitsInBox(new Rect(m_currentSelectionStartPoint.x, m_currentSelectionStartPoint.y, m_currentSelectionEndPoint.x - m_currentSelectionStartPoint.x, m_currentSelectionEndPoint.y - m_currentSelectionStartPoint.y), out selectedUnits))
                    {
                        // TODO : Don't assume local player ID is 0 ! 
                        int n = m_selectedPawns.SelectPawnsOwnedBy(selectedUnits, shiftSelect, m_localPlayerID);
                        if (n > 0)
                            enemySelectedMode = false;
                    }
                }
            }


            private void InputManager_OnLeftMouseDown(Vector2 screenCoords)
            {
                m_currentSelectionStartPoint = screenCoords;
                m_currentSelectionEndPoint = screenCoords;
            }

            private void InputManager_OnLeftMouseHold(Vector2 screenCoords)
            {
                m_currentSelectionEndPoint = screenCoords;
            }

            private void InputManager_OnLeftMouseUp()
            {
                SelectUnitsWithCurrentSelectionZone();
            }

            private void InputManager_OnRightClick(Vector2 screenCoords)
            {
                if (enemySelectedMode) return;

                // TODO Think about where to put the "generate orders and send to server" code below.
                var selectedUnitIDs = m_selectedPawns.RetrieveSelectedUnitIDs();
                if (selectedUnitIDs.Length > 0)
                {
                    OBJECT_DATA_AI order;

                    Vector3 pos = Misc.CameraTools.GetTerrainPositionFromScreenPosition(screenCoords);
                    order = UnitOrderFactory.DispatchOrderWithTargetPosition(selectedUnitIDs, pos);

                    var msgData = DataChangeEventFactory.BuildDataChangeEventData(order, selectedUnitIDs, 0f);

                    StageMessageForSending(m_orderGivenMessagePacker.PackMessage(msgData));
                }
            }

            private void OnMatchStartedDataMessageReceived(MESSAGE message)
            {
                var matchStartedData = m_matchStartedMessagePacker.UnpackMessage(message);
                m_localPlayerID = matchStartedData.localPlayerID;
                m_selectedPawns.SetLocalPlayerID(m_localPlayerID);
            }

        }
    }
}


