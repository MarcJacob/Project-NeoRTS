using NeoRTS.Client.Pawns;
using NeoRTS.GameData.ObjectData;
using UnityEngine;
using UnityEngine.UI;

namespace NeoRTS
{
    namespace Client
    {
        namespace UI
        {
            public class PawnHealthBarPawnUIModule : ObjectPawnUIModule
            {
                [SerializeField]
                private Slider m_healthSlider;
                [SerializeField]
                private Color m_friendlyColor;
                [SerializeField]
                private Color m_enemyColor;

                private int m_maxHealth;

                private ObjectPawnComponent.DataWatcher<OBJECT_DATA_HEALTH> m_healthDataWatcher;

                private bool HasHealthDataChanged(OBJECT_DATA_HEALTH previous, OBJECT_DATA_HEALTH current)
                {
                    return previous.HP != current.HP;
                }

                public override void Initialize(ObjectPawnComponent linkedPawnComponent)
                {
                    if (linkedPawnComponent.RegisterDataWatcher(out m_healthDataWatcher, HasHealthDataChanged))
                    {
                        SetMaxHealth(m_healthDataWatcher.CurrentValue.HP);
                        SetColorIsFriendly(linkedPawnComponent.ControlledByLocalPlayer);
                        m_healthDataWatcher.onValueChanged += UpdateHealth;
                    }
                }

                public void SetMaxHealth(int maxHealth)
                {
                    m_maxHealth = maxHealth;
                }
                
                public void UpdateHealth(OBJECT_DATA_HEALTH data)
                {
                    m_healthSlider.value = ((float)data.HP) / m_maxHealth;
                }

                public void SetColorIsFriendly(bool friendly)
                {
                    if (friendly) m_healthSlider.fillRect.GetComponent<Image>().color = m_friendlyColor;
                    else m_healthSlider.fillRect.GetComponent<Image>().color = m_enemyColor;
                }
            }
        }
    }
}


