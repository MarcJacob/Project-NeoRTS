using System;
using UnityEngine;
using UnityEngine.UI;
namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {

            public class ConnectedToMasterServerMenuUIModule : UIModule
            {
                [SerializeField]
                [Tooltip("Buttons that become unclickable when looking for a match.")]
                private Button[] m_normalStateButtons;

                [SerializeField]
                [Tooltip("Objects that show up only while in matchmaking.")]
                private GameObject[] m_matchmakingStateObjects;

                public event Action OnMatchmakingButtonClick = delegate { };
                public event Action OnSingleplayerButtonClick = delegate { };

                public void OnMatchmakingButtonPressed()
                {
                    OnMatchmakingButtonClick();
                }

                public void OnSingleplayerButtonPressed()
                {
                    OnSingleplayerButtonClick();
                }

                /// <summary>
                /// Makes all buttons clickable.
                /// </summary>
                public void SwitchToNormalState()
                {
                    foreach(var button in m_normalStateButtons)
                    {
                        button.interactable = true;
                    }

                    foreach(var obj in m_matchmakingStateObjects)
                    {
                        obj.SetActive(false);
                    }
                }

                /// <summary>
                /// Makes all (initial) buttons unclickable and makes a set of elements show.
                /// </summary>
                public void SwitchToMatchmakingState()
                {
                    foreach (var button in m_normalStateButtons)
                    {
                        button.interactable = false;
                    }

                    foreach (var obj in m_matchmakingStateObjects)
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }
    }
}


