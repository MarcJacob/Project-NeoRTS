using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Actors
        {
            [RequireComponent(typeof(Renderer))]
            public class ActorSelectionCircleScript : MonoBehaviour
            {
                [SerializeField]
                private Color friendlySelectionColor;
                [SerializeField]
                private Color enemySelectionColor;
                [SerializeField]
                private string colorPropertyName = "_Color";

                private Renderer m_renderer;

                private void Awake()
                {
                    m_renderer = GetComponent<Renderer>();
                    gameObject.SetActive(false);
                }

                public void SelectFriendly()
                {
                    m_renderer.material.SetColor(colorPropertyName, friendlySelectionColor);
                }

                public void SelectEnemy()
                {
                    m_renderer.material.SetColor(colorPropertyName, enemySelectionColor);
                }
            }
        }
    }
}
