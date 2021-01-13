using System;
using TMPro;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            public class MainMenuUIModule : UIModule
            {
                public struct LoginButtonClickEventData
                {
                    public string address;
                    public string port;
                    public string username;
                }


                private string m_connectionAddress = "";
                private string m_connectionPort = "";
                private string m_username = "";

                [SerializeField]
                private TMP_InputField m_addressField;
                [SerializeField]
                private TMP_InputField m_portField;
                [SerializeField]
                private TMP_InputField m_usernameField;

                private void Start()
                {
                    m_addressField.onValueChanged.AddListener(OnAddressTextFieldChanged);
                    m_portField.onValueChanged.AddListener(OnPortTextFieldChanged);
                    m_usernameField.onValueChanged.AddListener(OnUsernameFieldChanged);

                    m_addressField.text = PlayerPrefs.GetString("MASTER_SERVER_ADDRESS");
                    m_portField.text = PlayerPrefs.GetString("MASTER_SERVER_PORT");
                    m_usernameField.text = PlayerPrefs.GetString("USERNAME");
                }

                public void OnAddressTextFieldChanged(string val)
                {
                    m_connectionAddress = val;
                }

                public void OnPortTextFieldChanged(string val)
                {
                    m_connectionPort = val;
                }

                public void OnUsernameFieldChanged(string val)
                {
                    m_username = val;
                }

                public event Action<LoginButtonClickEventData> OnMultiplayerButtonClick = delegate { };
                public event Action OnSingleplayerButtonClick = delegate { };

                public void OnMultiplayerButtonPressed()
                {
                    var eventData = new LoginButtonClickEventData()
                    {
                        address = m_connectionAddress,
                        port = m_connectionPort,
                        username = m_username
                    };
                    OnMultiplayerButtonClick(eventData);
                }

                public void OnSingleplayerButtonPressed()
                {
                    OnSingleplayerButtonClick();
                }
            }
        }
    }
}


