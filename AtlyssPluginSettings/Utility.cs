using UnityEngine;

namespace AtlyssPluginSettings
{
    public static class Utility
    {
        public static GameObject? TryFind(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if(gameObject == null) PluginSettings.Logger.LogError($"GameObject \"{name}\" could not be found! Please report this as an issue. [ https://github.com/portalsam1/AtlyssPluginSettings ]");
            return gameObject;
        } 
    }
}
