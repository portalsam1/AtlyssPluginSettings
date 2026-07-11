
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AtlyssPluginSettings;

internal static class NewSettingsManagerPatch
{

    private static SettingsManager _settingsManager = null!;
        
    private static MenuElement? _settingsMenu;
    private static GameObject? HeaderTemplate, BoolTemplate, FieldTemplate, KeyTemplate;

    private static void Postfix()
    {
         
        _settingsManager = SettingsManager._current;
        _settingsMenu = typeof(SettingsManager).GetField("_settingsMenuElement", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_settingsManager) as MenuElement;
        if (!_settingsMenu)
        {
            PluginSettings.Logger.LogError("A problem was encountered while patching the settings menu, _settingsMenuElement is null or could not be found.");
            return;
        }
        
        HorizontalLayoutGroup buttonGroup = _settingsMenu!.transform.Find("_dolly_tabButtons").GetComponent<HorizontalLayoutGroup>();
        if (!buttonGroup.childControlWidth)
        {
            buttonGroup.childControlWidth = true;
            buttonGroup.padding.right = buttonGroup.padding.right;
            buttonGroup.spacing = 4;
        }
        
        GameObject? videoTabContent = typeof(SettingsManager).GetField("_videoTabContent", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_settingsManager) as GameObject;
        HeaderTemplate = videoTabContent?.transform.GetChild(0).gameObject;
            
        BoolTemplate = _settingsManager._jiggleBonesToggle.gameObject.transform.parent.gameObject;
        FieldTemplate = _settingsManager._defaultChatRoomNameInput.gameObject.transform.parent.gameObject;
        
        GameObject? inputTabContent = typeof(SettingsManager).GetField("_inputTabContent",  BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_settingsManager) as GameObject;
        KeyTemplate = inputTabContent?.transform.Find("_backdrop/Scroll View/Viewport/Content/_cell_keybinding_up").gameObject;

    }

    private static byte GetUnusedSettingMenuSelectionIndex(GameObject settingsMenu)
    {
            
        List<MenuElement> menuElements = [];
                
        /* Find all menu elements that are a tab in the settings menu, and that are not the parent settings menu. */
        foreach (Object obj in Object.FindObjectsOfType(typeof(MenuElement), true))
        {
                    
            MenuElement? menuElement = obj as MenuElement;
            if(menuElement?.gameObject is null || menuElement.gameObject == settingsMenu) continue;
                    
            if(menuElement && menuElement!.transform.IsChildOf(settingsMenu.transform))
                menuElements.Add(menuElement);
                    
        }
            
        /* Iterate through every possible tab index until one is found that does not have an enabled menu element. */
        for (byte i = 0; i < byte.MaxValue; i++)
        {

            bool indexUsed = false;
                    
            _settingsManager.Set_SettingMenuSelectionIndex(i);
            foreach (MenuElement unused in menuElements.Where(menuElement => menuElement.isEnabled))
                indexUsed = true;

            if (indexUsed) continue;
            
            _settingsManager.Set_SettingMenuSelectionIndex(0);
            return i;

        }

        return 255;

    }
        
}
