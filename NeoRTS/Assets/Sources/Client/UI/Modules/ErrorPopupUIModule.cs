using System;
using UnityEngine;
using TMPro;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            public class ErrorPopupUIModule : UIModule
            {
                [SerializeField]
                private TextMeshProUGUI m_text;
                private Action m_onPopupClosed;

                public void Init(string text, Action onPopupClosed = null)
                {
                    m_text.text = text;
                    m_onPopupClosed = onPopupClosed;
                }

                public void CloseButton_Click()
                {
                    Destroy(gameObject);
                    if (m_onPopupClosed != null) m_onPopupClosed();
                }
            }
        }
    }
}


