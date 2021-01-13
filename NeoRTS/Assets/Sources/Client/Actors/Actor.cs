using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoRTS.Client.Pawns;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Actors
        {
            public enum ACTOR_EVENT
            {
                DESELECTED,
                SELECTED_FRIENDLY,
                SELECTED_ENEMY,
                ATTACK,
                DEATH,
                ENTER_COMBAT,
                LEAVE_COMBAT,
                START_MOVEMENT,
                STOP_MOVEMENT
            }

            /// <summary>
            /// Actors manage a collection of graphical / audio content that follows (via parenting)
            /// and is able to react to events sent by the controlling <see cref="ObjectPawnComponent"/>.
            /// </summary>
            public class Actor : MonoBehaviour
            {
                [Serializable]
                public struct EventHandler
                {
                    public ACTOR_EVENT eventName;
                    public UnityEvent eventHandlers;
                }

                [SerializeField]
                private EventHandler[] eventHandlers;

                private Dictionary<ACTOR_EVENT, EventHandler> m_eventNameToEventHandlerDictionary;
                private Animator m_animatorComponent;

                private void Awake()
                {
                    m_eventNameToEventHandlerDictionary = new Dictionary<ACTOR_EVENT, EventHandler>();
                    foreach(var handler in eventHandlers)
                    {
                        m_eventNameToEventHandlerDictionary.Add(handler.eventName, handler);
                    }
                    m_animatorComponent = GetComponentInChildren<Animator>();
                }

                public void ProcessEvent(ACTOR_EVENT eventName)
                {
                    if (m_eventNameToEventHandlerDictionary.ContainsKey(eventName))
                    {
                        var eventHandlerData = m_eventNameToEventHandlerDictionary[eventName];
                        eventHandlerData.eventHandlers.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning("Warning - Actor '" + name + "' was instructed to process event '" + eventName + "' which it does not support !");
                    }
                }

                public void SetInCombat(bool inCombat)
                {
                    if (m_animatorComponent)
                    {
                        m_animatorComponent.SetBool("InCombat", inCombat);
                    }
                }

                public void SetInMovement(bool inMovement)
                {
                    if (m_animatorComponent)
                    {
                        m_animatorComponent.SetBool("Moving", inMovement);
                    }
                }

            }
        }
    }
}