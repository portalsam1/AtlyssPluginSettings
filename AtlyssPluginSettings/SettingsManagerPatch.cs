using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

using Input = UnityEngine.Input;
using Object = UnityEngine.Object;

namespace AtlyssPluginSettings
{
    
    internal static class SettingsManagerPatch
    {
        
        private static MenuElement? _pluginSettingsMenuElement;
        private static GameObject? _inputSettingsTabReference, _pluginSettingsTabReference, _pluginsContentReference, _resetButtonReference, _settingsMenuReference;

        private static readonly List<Button> KeybindButtons = new List<Button>();
        private static bool _waitingForKey;
        
        // Hardcoded enum value, maybe change this to dynamically choose a number in case other mods want to fuck with the settings menu, I am lazy for right now though.
        private const int Plugin = 4;
        
        [HarmonyPatch(typeof(SettingsManager), "Start")] [SuppressMessage("ReSharper", "UnusedMember.Local")] [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static class SettingsManagerStartPatch
        {
            
            private static GameObject? _dollySettingsMenuReference, _cancelButtonReference, _settingsHeaderReference, _boolToggleReference, _keyCodeReference;
        
            private static void Postfix(SettingsManager __instance)
            {
                
                KeybindButtons.Clear();

                _settingsMenuReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu");
                
                _dollySettingsMenuReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu");
                _cancelButtonReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_settingsButtons/Button_cancelSettings");
                
                _inputSettingsTabReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_inputSettingsTab");
                
                _settingsHeaderReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_videoSettingsTab/_backdrop_videoSettings/Scroll View/Viewport/Content/_header_GameEffectSettings");
                _boolToggleReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_videoSettingsTab/_backdrop_videoSettings/Scroll View/Viewport/Content/_cell_jiggleBonesToggle");
                _keyCodeReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_inputSettingsTab/_backdrop/Scroll View/Viewport/Content/_cell_keybinding_up");
                _resetButtonReference = Utility.TryFind("_SettingsManager/Canvas_SettingsMenu/_dolly_settingsMenu/_dolly_inputSettingsTab/_backdrop/Scroll View/Viewport/Content/_cell_cameraSensitivity/Button_01");

                if (!_settingsMenuReference || !_dollySettingsMenuReference || !_cancelButtonReference ||
                    !_inputSettingsTabReference || !_settingsHeaderReference || !_boolToggleReference ||
                    !_keyCodeReference || !_resetButtonReference)
                {
                    PluginSettings.Logger.LogError("One or more UI elements were not found. Mod will not take any affect.");
                    return;
                }
                
                GameObject? pluginSettingsButtonObject = Object.Instantiate(_cancelButtonReference, _dollySettingsMenuReference!.transform, true);
                pluginSettingsButtonObject!.name = "Button_pluginSettings";
                pluginSettingsButtonObject.transform.localPosition = new Vector3(-219f, -212f, 0f);
                pluginSettingsButtonObject.transform.localScale = new Vector3(1f, 1f, 1f);

                pluginSettingsButtonObject.transform.GetChild(0).GetComponent<Text>().text = "Plugins";
            
                Button pluginSettingsButton = pluginSettingsButtonObject.GetComponent<Button>();
                pluginSettingsButton.onClick = new Button.ButtonClickedEvent();
                pluginSettingsButton.onClick.AddListener(delegate { __instance.Set_SettingMenuSelectionIndex(Plugin); });
                
                _pluginSettingsTabReference = Object.Instantiate(_inputSettingsTabReference, _inputSettingsTabReference!.transform.parent, true);
                _pluginSettingsTabReference!.name = "_dolly_pluginSettingsTab";
                _pluginSettingsTabReference.transform.localPosition = new Vector3(1f, 1f, 1f);
                
                _pluginsContentReference = _pluginSettingsTabReference.transform.Find("_backdrop/Scroll View/Viewport/Content").gameObject;
                foreach(Transform child in _pluginsContentReference.transform) { Object.Destroy(child.gameObject); }
                _pluginSettingsMenuElement = _pluginSettingsTabReference.GetComponent<MenuElement>();
                
                SetupSettingsMenu();
                
                PluginSettings.Logger.LogInfo("SettingsManager has been patched.");

            }

            private static void SetupSettingsMenu()
            {

                foreach (KeyValuePair<string, PluginInfo> pluginInfoPair in Chainloader.PluginInfos)
                {
                    ConfigFile config = pluginInfoPair.Value.Instance.Config;
                    if (config.Keys.Count < 1) continue;
                    
                    CreateHeader(pluginInfoPair.Value.Metadata.Name);
                    
                    Dictionary<ConfigDefinition, ConfigEntryBase> configEntries = (config.GetType().GetProperty("Entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(config) as Dictionary<ConfigDefinition, ConfigEntryBase?>)!;
                    foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> entry in configEntries)
                    {
                        if (entry.Value.SettingType == typeof(bool))
                        {
                            CreateBool(entry.Value, pluginInfoPair.Value);
                        }
                        else if (entry.Value.SettingType == typeof(KeyCode))
                        {
                            CreateKeyCode(entry.Value, pluginInfoPair.Value);
                        }
                        else
                        {
                            CreateField(entry.Value, pluginInfoPair.Value);
                        }
                    }
                }
                
            }

            private static void CreateHeader(string headerName)
            {

                GameObject headerObject = Object.Instantiate(_settingsHeaderReference, _pluginsContentReference!.transform, true)!;
                headerObject.name = $"_header_{headerName}";
                
                headerObject.transform.localScale = new Vector3(1f, 1f, 1f);
                headerObject.transform.GetChild(0).GetComponent<Text>().text = headerName;
                
            }

            private static void CreateBool(ConfigEntryBase entry, PluginInfo plugin)
            {
                
                GameObject boolToggleObject = Object.Instantiate(_boolToggleReference, _pluginsContentReference!.transform, true)!;
                boolToggleObject.name = $"_cell_{entry.Definition.Key}";
                
                boolToggleObject.transform.localScale = new Vector3(1f, 1f, 1f);
                boolToggleObject.transform.GetChild(0).GetComponent<Text>().text = entry.Definition.Key;
                
                GameObject toggleObject = boolToggleObject.transform.GetChild(1).gameObject;
                toggleObject.name = $"Toggle_{entry.Definition.Key}";
                
                GameObject resetButtonObject = Object.Instantiate(_resetButtonReference, boolToggleObject.transform, true)!;
                resetButtonObject.name = "ResetButton";
                resetButtonObject.transform.localScale = new Vector3(1f, 1f, 1f);
                resetButtonObject.transform.localPosition = new Vector3(64f, 0f, 0f);
                RectTransform resetButtonRectTransform = resetButtonObject.GetComponent<RectTransform>();
                resetButtonRectTransform.sizeDelta = new Vector2(72f, 24f);
                Button resetButton = resetButtonObject.GetComponent<Button>();
                resetButton.onClick = new Button.ButtonClickedEvent();
                
                Toggle toggle = toggleObject.GetComponent<Toggle>();
                toggle.onValueChanged = new Toggle.ToggleEvent();

                toggle.isOn = entry.BoxedValue is bool value && value;

                resetButton.onClick.AddListener(delegate
                {
                    toggle.isOn = entry.DefaultValue is bool defaultValue && defaultValue;
                    entry.BoxedValue = toggle.isOn;
                    plugin.Instance.Config.Save();
                });
                
                toggle.onValueChanged.AddListener(delegate
                {
                    entry.BoxedValue = toggle.isOn;
                    plugin.Instance.Config.Save();
                });

            }

            private static void CreateKeyCode(ConfigEntryBase entry, PluginInfo plugin)
            {
                
                GameObject keyCodeObject = Object.Instantiate(_keyCodeReference, _pluginsContentReference!.transform, true)!;
                keyCodeObject.name = $"_cell_keybinding_{entry.Definition.Key}";
                
                keyCodeObject.transform.localScale = new Vector3(1f, 1f, 1f);
                keyCodeObject.transform.GetChild(0).GetComponent<Text>().text = entry.Definition.Key;
                
                GameObject buttonObject = keyCodeObject.transform.GetChild(1).gameObject;
                Button bindButton = buttonObject.GetComponent<Button>();
                bindButton.onClick = new Button.ButtonClickedEvent();
                
                GameObject resetButtonObject = Object.Instantiate(_resetButtonReference, keyCodeObject.transform, true)!;
                resetButtonObject.name = "ResetButton";
                resetButtonObject.transform.localScale = new Vector3(1f, 1f, 1f);
                resetButtonObject.transform.localPosition = new Vector3(64f, 0f, 0f);
                RectTransform resetButtonRectTransform = resetButtonObject.GetComponent<RectTransform>();
                resetButtonRectTransform.sizeDelta = new Vector2(72f, 24f);
                Button resetButton = resetButtonObject.GetComponent<Button>();
                resetButton.onClick = new Button.ButtonClickedEvent();
                
                Text keybindText = buttonObject.transform.GetChild(0).GetComponent<Text>();
                keybindText.text = (entry.BoxedValue is KeyCode code ? code : KeyCode.None).ToString();
                
                KeybindButtons.Add(bindButton);
                bindButton.onClick.AddListener(delegate
                { 
                    PluginSettings.Instance.StartCoroutine(BindButtonCoroutine(buttonObject, keybindText, entry, plugin));
                });
                
                resetButton.onClick.AddListener(delegate
                {
                    keybindText.text = entry.DefaultValue.ToString();
                    entry.BoxedValue = entry.DefaultValue;
                    plugin.Instance.Config.Save();
                });

            }

            private static void CreateField(ConfigEntryBase entry, PluginInfo plugin)
            {
                
                GameObject fieldObject = Object.Instantiate(_keyCodeReference, _pluginsContentReference!.transform, true)!;
                fieldObject.name = $"_cell_input_{entry.Definition.Key}";
                
                fieldObject.transform.localScale = new Vector3(1f, 1f, 1f);
                GameObject originalTextObject = fieldObject.transform.GetChild(0).gameObject;
                Text originalText = originalTextObject.GetComponent<Text>();
                originalText.text = entry.Definition.Key;
                
                Object.Destroy(fieldObject.transform.GetChild(1).gameObject);
                
                GameObject resetButtonObject = Object.Instantiate(_resetButtonReference, fieldObject.transform, true)!;
                resetButtonObject.name = "ResetButton";
                resetButtonObject.transform.localScale = new Vector3(1f, 1f, 1f);
                resetButtonObject.transform.localPosition = new Vector3(64f, 0f, 0f);
                RectTransform resetButtonRectTransform = resetButtonObject.GetComponent<RectTransform>();
                resetButtonRectTransform.sizeDelta = new Vector2(72f, 24f);
                Button resetButton = resetButtonObject.GetComponent<Button>();
                resetButton.onClick = new Button.ButtonClickedEvent();
                
                GameObject inputFieldObject = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField))
                {
                    layer = LayerMask.NameToLayer("UI")
                };
                inputFieldObject.transform.SetParent(fieldObject.transform, true);
                inputFieldObject.transform.localScale = new Vector3(1f, 1f, 1f);
                inputFieldObject.transform.localPosition = new Vector3(187f, 0f, 0f);
                
                RectTransform inputFieldRectTransform = inputFieldObject.GetComponent<RectTransform>();
                inputFieldRectTransform.sizeDelta = new Vector2(156f, 20f);
                
                Image inputFieldImage = inputFieldObject.GetComponent<Image>();
                inputFieldImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                inputFieldImage.type = Image.Type.Sliced;
                inputFieldImage.sprite = fieldObject.GetComponent<Image>().sprite;
                
                InputField inputField = inputFieldObject.GetComponent<InputField>();
                
                GameObject inputFieldTextObject = Object.Instantiate(originalTextObject, inputFieldObject.transform, true);
                inputFieldTextObject.layer = LayerMask.NameToLayer("UI");
                inputFieldTextObject.transform.localScale = new Vector3(1f, 1f, 1f);
                inputFieldTextObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                inputFieldTextObject.name = "Text";
                
                Text inputFieldText = inputFieldTextObject.GetComponent<Text>();
                inputFieldText.fontSize = 16;
                inputFieldText.text = "";
                RectTransform fieldTextRectTransform = inputFieldTextObject.GetComponent<RectTransform>();
                fieldTextRectTransform.sizeDelta = new Vector2(142f, 20f);
                
                GameObject inputFieldPlaceholderTextObject = Object.Instantiate(originalTextObject, inputFieldObject.transform, true);
                inputFieldPlaceholderTextObject.layer = LayerMask.NameToLayer("UI");
                inputFieldPlaceholderTextObject.transform.localScale = new Vector3(1f, 1f, 1f);
                inputFieldPlaceholderTextObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                inputFieldPlaceholderTextObject.name = "Placeholder";
                
                Text inputFieldPlaceholderText = inputFieldPlaceholderTextObject.GetComponent<Text>();
                inputFieldPlaceholderText.fontSize = 16;
                inputFieldPlaceholderText.text = "Enter a value...";
                inputFieldPlaceholderText.color = new Color(1f, 1f, 1f, 0.35f);
                RectTransform fieldPlaceholderTextRectTransform = inputFieldPlaceholderTextObject.GetComponent<RectTransform>();
                fieldPlaceholderTextRectTransform.sizeDelta = new Vector2(142f, 20f);
                
                inputField.textComponent = inputFieldText;
                inputField.placeholder = inputFieldPlaceholderText;
                inputField.SetTextWithoutNotify(entry.BoxedValue.ToString());
                inputField.MoveTextStart(true);
                
                resetButton.onClick.AddListener(delegate
                {
                    inputField.text = entry.BoxedValue.ToString();
                    entry.BoxedValue = entry.DefaultValue;
                    plugin.Instance.Config.Save();
                });
                
                inputField.onSubmit.AddListener(delegate
                {

                    bool validInput = false;

                    try
                    {
                        TypeConverter converter = TomlTypeConverter.GetConverter(entry.SettingType);
                        if (!TomlTypeConverter.CanConvert(entry.SettingType)) return;
                        
                        entry.BoxedValue = converter.ConvertToObject(inputField.text, entry.SettingType);
                        validInput = true;
                        plugin.Instance.Config.Save();
                    }
                    catch (Exception e) { PluginSettings.Logger.LogWarning($"Input String: \"{inputField.text}\" created exception: \n{e}"); }

                    if (!validInput) inputField.text = "";

                });
                
            }

            private static IEnumerator BindButtonCoroutine(GameObject buttonObject, Text bindButtonText, ConfigEntryBase configEntry, PluginInfo plugin)
            {
                
                if(_waitingForKey) yield break;
                KeyCode foundKey = KeyCode.None;
                string oldText = bindButtonText.text;
                float keyTimer = 5f;
                
                while (true)
                {

                    if (!buttonObject.activeSelf || !_pluginSettingsTabReference!.activeSelf || !_settingsMenuReference!.activeSelf) break;
                    
                    _waitingForKey = true;
                    foreach (Button buttons in KeybindButtons) { buttons.interactable = false; }

                    KeyCode keyPressed = KeyCode.None;
                    foreach (KeyCode key in Enum.GetValues(typeof (KeyCode)))
                    {
                        if (Input.GetKeyDown(key))
                            keyPressed = key;
                    }
                    
                    // Block binding to Mouse0, it just causes issues.
                    if (keyPressed == KeyCode.Escape || keyPressed == KeyCode.Mouse0) break;
                    if(keyPressed != KeyCode.None)
                    {
                        foundKey = keyPressed;
                        break;
                    }
                    
                    keyTimer -= Time.deltaTime;

                    bindButtonText.text = $"Press a key ({Mathf.Round(keyTimer)})";
                    if (keyTimer <= 0f) break;
                    
                    yield return null;
                    
                }
                
                _waitingForKey = false;
                
                if (foundKey != KeyCode.None)
                {
                    bindButtonText.text = foundKey.ToString();
                    configEntry.BoxedValue = foundKey;
                    plugin.Instance.Config.Save();
                } else bindButtonText.text = oldText;

                yield return new WaitForSeconds(0.25f);
                
                foreach (Button buttons in KeybindButtons) { buttons.interactable = true; }
                
            }
            
        }
        [HarmonyPatch(typeof(SettingsManager), "Set_SettingMenuSelectionIndex")] [SuppressMessage("ReSharper", "UnusedMember.Local")] [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static class SettingsManagerSelectionPatch
        {
            
            private static void Finalizer(int _index)
            {
                // This is usually supposed to check against _currentSettingsMenuSelection in SettingsManager, but I for some reason can't bind to that property. So fuck it we ball.
                if (_pluginSettingsMenuElement != null)
                    _pluginSettingsMenuElement.isEnabled = _index == Plugin;
            }
            
        }
        
    }
    
}
