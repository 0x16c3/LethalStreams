using HarmonyLib;

using UnityEngine;

namespace LethalStreams.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    public class GameNetworkManagerOriginal
    {
        private static void Prefix(GameNetworkManager __instance)
        {
            HUDManagerNetworkPatched.Register(__instance);
        }
    }
    
    public class GameNetworkManagerPatched : MonoBehaviour
    {
        public static GameNetworkManagerPatched Instance { get; private set; }
        public GameNetworkManager _original = null;
        
        public static void Spawn(GameNetworkManager __instance)
        {
            var gameObject = new GameObject("GameNetworkManagerPatched");
            gameObject.AddComponent<GameNetworkManagerPatched>();
            
            CustomLogger.LogInfo("Spawned GameNetworkManagerPatched.");
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Instance = this;
        }
    }
}

