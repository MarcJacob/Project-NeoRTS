using NeoRTS.Client.UI;
using NeoRTS.Communication;
using NeoRTS.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NeoRTS
{
    namespace Client
    {

        /// <summary>
        /// Manages all UI in the Game Client through allowing the use of UIModules.
        /// All UIModules spawned with this manager will be set as child of the [MAIN UI] object (Otherwise called "UI Root").
        /// TODO : We would like UIModules instantiated by States to automatically be unloaded when said State ends.
        /// To do this, we need to add an event on the Client that gets triggered whenever a new State is switched to.
        /// This also means we need a special type of manager that is able to react to this event.
        /// </summary>
        public class UIManager : ManagerObject
        {
            private GameObject m_uiRoot;
            private Dictionary<string, UIModule> m_nameBasedUIModuleAssetsContainer = new Dictionary<string, UIModule>();
            private Dictionary<Type, List<UIModule>> m_typeBasedUIModuleAssetsContainer = new Dictionary<Type, List<UIModule>>();
            protected override void OnManagerInitialize()
            {
                m_uiRoot = GameObject.Find("[MAIN UI]");
                GameObject.DontDestroyOnLoad(m_uiRoot);


                List<UIModule> modulesList = new List<UIModule>();
                LoadAllUIModules(modulesList);

                foreach(var module in modulesList)
                {
                    m_nameBasedUIModuleAssetsContainer.Add(module.name, module);
                    Type type = module.GetType();
                    if (m_typeBasedUIModuleAssetsContainer.ContainsKey(type))
                    {
                        m_typeBasedUIModuleAssetsContainer[type].Add(module);
                    }
                    else
                    {
                        m_typeBasedUIModuleAssetsContainer.Add(type, new List<UIModule>() { module });
                    }
                    
                }
            }

            private void LoadAllUIModules(List<UIModule> modulesList)
            {
                // Look into this folder and load all UI Modules here.
                var allAssetsObjectsHere = Resources.LoadAll("UI/Modules");
                foreach (var assetObject in allAssetsObjectsHere)
                {
                    if (assetObject is GameObject)
                    {
                        UIModule mod = ((GameObject)assetObject).GetComponent<UIModule>();
                        if (mod)
                        {
                            modulesList.Add(mod);
                            Debug.Log("UI LOADING : LOADED MODULE '" + assetObject.name + "'");
                        }
                    }
                    
                }
            }

            protected override void OnManagerUpdate(float deltaTime)
            {

            }

            public UIModule GetUIModule(string name)
            {
                var module = m_nameBasedUIModuleAssetsContainer[name];
                var instantiated = InstantiateModule(module);

                return instantiated;
            }

            public T GetUIModule<T>() where T : UIModule
            {
                var module = m_typeBasedUIModuleAssetsContainer[typeof(T)][0];
                UIModule instantiated = InstantiateModule(module);

                return (T)instantiated;
            }

            public T GetUIModule<T>(string name) where T : UIModule
            {
                var module = m_nameBasedUIModuleAssetsContainer[name];
                var casted = (T)module;
                if (casted != null)
                {
                    var instantiated = InstantiateModule(casted);

                    return casted;
                }
                return null;
            }

            public void ShowErrorPopup(string errorMessage, Action onPopupMessageClosed = null)
            {
                var mod = GetUIModule<ErrorPopupUIModule>();
                mod.Init(errorMessage, onPopupMessageClosed);
            }

            private UIModule InstantiateModule(UIModule module)
            {
                var instantiated = GameObject.Instantiate(module);
                instantiated.name = "[UI MODULE] " + module.name;

                if (instantiated.transform.parent == null) // If the module has NOT set its own parent, set it to the main UI ROOT by default.
                instantiated.transform.SetParent(m_uiRoot.transform, false);

                if (instantiated.GetComponent<Canvas>())
                {
                    RectTransform rectTransform = instantiated.GetComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;

                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = Vector2.zero;
                }
                return instantiated;
            }

            public override void OnManagerCleanupMessageReception(MessageDispatcher dispatcher)
            { }

            public override void OnManagerInitializeMessageReception(MessageDispatcher dispatcher)
            { }
        }
    }
}


