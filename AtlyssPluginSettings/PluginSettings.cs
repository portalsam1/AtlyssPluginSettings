using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace AtlyssPluginSettings
{
    
    [SuppressMessage("ReSharper", "UnusedMember.Local")] [SuppressMessage("ReSharper", "UnusedType.Global")]
    [BepInPlugin("net.portalsam.AtlyssPluginSettings", "PluginSettings", "1.0.0.0")]
    [BepInProcess("ATLYSS.exe")]
    public class PluginSettings : BaseUnityPlugin
    {

        internal static PluginSettings Instance = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            
            Logger.LogInfo("PluginSettings has been initialized!");
        }

        internal static ConfigFile GetConfig() => Instance.Config;
        
    }
    
}
