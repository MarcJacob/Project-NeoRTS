using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            public class ChatBoxUIModule : UIModule
            {
                [SerializeField]
                private TMP_Text chatTextTarget;
                [SerializeField]
                private RectTransform contentTransform;
                [SerializeField]
                private ScrollRect scrollbar;
                [SerializeField]
                private TMP_InputField chatInputField;

                public event Action<string> OnMessageSubmission = delegate { };

                public void Submit(string msg)
                {
                    OnMessageSubmission(msg);
                    chatInputField.text = "";
                    chatInputField.ActivateInputField();
                }

                private void Start()
                {
                    chatInputField.onSubmit.AddListener(Submit);
                }

                public void AddChatLine(string senderName, string text)
                {
                    if (chatTextTarget.text.Length > 0)
                    {
                        chatTextTarget.text += "\n";
                    }
                    
                    chatTextTarget.text += senderName + " : " + text;

                    if (chatTextTarget.isTextOverflowing)
                    {
                        Vector2 size = chatTextTarget.GetPreferredValues(chatTextTarget.text);

                        contentTransform.sizeDelta = size;
                        scrollbar.verticalNormalizedPosition = 0f;
                    }
                }
            }
        }
    }
}