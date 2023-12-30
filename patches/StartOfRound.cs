using HarmonyLib;

using Unity.Netcode;
using UnityEngine;

namespace LethalStreams.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    public class StartOfRoundOriginal
    {
        private static void Postfix(StartOfRound __instance)
        {
            StartOfRoundPatched.Instance._original = __instance;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                HUDManagerNetworkPatched.Spawn();
            }
            
            CustomLogger.LogInfo("Captured StartOfRound instance.");
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
    public class OnClientConnect
    {
        private static void Prefix(StartOfRound __instance, ulong clientId)
        {
            
        }
    }
    
    public class StartOfRoundPatched : MonoBehaviour
    {
        public static StartOfRoundPatched Instance { get; private set; }
        public StartOfRound _original = null;
        
        public static void Spawn()
        {
            var gameObject = new GameObject("StartOfRoundPatched");
            gameObject.AddComponent<StartOfRoundPatched>();
            
            CustomLogger.LogInfo("Spawned StartOfRoundPatched.");
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
        
        public SignalTranslator SpawnTransmitter()
        {
            int unlockableIndex = 17;
            UnlockableItem unlockableItem = _original.unlockablesList.unlockables[unlockableIndex];

            var gameObject = Instantiate(unlockableItem.prefabObject, _original.elevatorTransform.position, Quaternion.identity, null);

            if (!gameObject.GetComponent<NetworkObject>().IsSpawned)
                gameObject.GetComponent<NetworkObject>().Spawn(false);

            if (gameObject != null)
                _original.SpawnedShipUnlockables.Add(unlockableIndex, gameObject);
            
            CustomLogger.LogInfo("Spawned Signal Translator.");
            return FindObjectOfType<SignalTranslator>();
        }

        public void SetMaxFearValue()
        {
            
        }
    }
}

