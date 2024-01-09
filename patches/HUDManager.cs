using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using Unity.Netcode;

namespace LethalStreams.Patches
{
    [HarmonyPatch(typeof(HUDManager), "Start")]
    public class HUDManagerOriginal
    {
        private static void Postfix(HUDManager __instance)
        {
            HUDManagerNetworkPatched.Instance._original = __instance;
            CustomLogger.LogInfo("Captured HUDManager instance.");
        }
    }

    [HarmonyPatch(typeof(HUDManager), "Update")]
    public class HUDManagerOriginalBackup
    {
        private static void Postfix(HUDManager __instance)
        {
            if (!HUDManagerNetworkPatched.Instance || HUDManagerNetworkPatched.Instance._original == __instance)
                return;

            HUDManagerNetworkPatched.Instance._original = __instance;
            CustomLogger.LogInfo("Captured HUDManager instance using backup call.");
        }
    }
    
    public class HUDManagerNetworkPatched : NetworkBehaviour
    {
        public static HUDManagerNetworkPatched Instance { get; private set; }
        public HUDManager _original = null;
        public static GameObject networkPrefab;
        
        public static void Register(GameNetworkManager gameNetworkManager)
        {
            if (networkPrefab != null)
                return;
            
            var bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalstreams"));
            
            networkPrefab = bundle.LoadAsset<GameObject>("Assets/NetworkPrefab.prefab");
            networkPrefab.AddComponent<HUDManagerNetworkPatched>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab); 
            
            CustomLogger.LogInfo("Registered HUDManagerNetworkPatched.");
        }

        public static void Spawn()
        {
            var obj = Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn();
            
            CustomLogger.LogInfo("Spawned HUDManagerNetworkPatched.");
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            
            base.OnNetworkSpawn();
        }
        
        [ServerRpc]
        public void DrainFlashlightBatteryServerRpc(string username = null)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            DrainFlashlightBatteryClientRpc(username);
        }
        
        [ClientRpc]
        public void DrainFlashlightBatteryClientRpc(string username = null)
        {
            CustomLogger.Log("DrainFlashlightBatteryClientRpc");
            var player = PlayerControllerBPatched.GetCurrentPlayer(username);
            if (player == null)
                return;
            
            player.DrainFlashlightBattery();
        }
        
        [ServerRpc]
        public void DrainStaminaServerRpc(string username = null)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            DrainStaminaClientRpc(username);
        }
        
        [ClientRpc]
        public void DrainStaminaClientRpc(string username = null)
        {
            CustomLogger.Log("DrainStaminaClientRpc");
            
            var player = PlayerControllerBPatched.GetCurrentPlayer(username);
            if (player == null)
                return;
            
            player.DrainStamina();
        }
        
        [ServerRpc]
        public void SetMaxSanityServerRpc(string username = null)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            SetMaxSanityClientRpc(username);
        }
        
        [ClientRpc]
        public void SetMaxSanityClientRpc(string username = null)
        {
            CustomLogger.Log("SetMaxSanityClientRpc");
            
            var player = PlayerControllerBPatched.GetCurrentPlayer(username);
            if (player == null)
                return;
            
            player.SetMaxSanity();
        }
        
        [ServerRpc]
        public void Rotate180ServerRpc(string username = null)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            Rotate180ClientRpc(username);
        }
        
        [ClientRpc]
        public void Rotate180ClientRpc(string username = null)
        {
            CustomLogger.Log("Rotate180ClientRpc");
            
            var player = PlayerControllerBPatched.GetCurrentPlayer(username);
            if (player == null)
                return;
            
            player.Rotate180();
        }

        [ServerRpc]
        public void MakeAirHornNoiseServerRpc(string username = null)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            MakeAirHornNoiseClientRpc(username);
        }

        [ClientRpc]
        public void MakeAirHornNoiseClientRpc(string username = null)
        {
            CustomLogger.Log("MakeAirHornNoiseClientRpc");
            
            // find "AirHorn1" sound effect in game assets
            var airHorn = Resources.FindObjectsOfTypeAll<AudioClip>().FirstOrDefault(x => x.name == "AirHorn1");

            // play air horn sound effect
            var player = PlayerControllerBPatched.GetCurrentPlayer(username);
            if (player == null)
                return;
            
            player._original.itemAudio.PlayOneShot(airHorn);
        }
        
        [ServerRpc]
        public void UseSignalTranslatorServerRpc(string signalMessage)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            SignalTranslator signalTranslator = FindObjectOfType<SignalTranslator>();

            // if no signal translator found, spawn one
            if (signalTranslator == null)
            {
                if (!Plugin.SpawnTransmitter.Value)
                    return;
                
                signalTranslator = StartOfRoundPatched.Instance.SpawnTransmitter();
            }

            if (Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator < 8f)
                return;

            signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
            signalTranslator.timesSendingMessage++;
            
            UseSignalTranslatorClientRpc(signalMessage, signalTranslator.timesSendingMessage);
        }

        [ClientRpc]
        public void UseSignalTranslatorClientRpc(string message, int timesSendingMessage)
        {
            CustomLogger.Log("UseSignalTranslatorClientRpc");

            SignalTranslator signalTranslator = FindObjectOfType<SignalTranslator>();
            signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
            
            if (signalTranslator == null)
                CustomLogger.LogError("SignalTranslator is null!");
            
            if (signalTranslator.signalTranslatorCoroutine != null)
                StopCoroutine(signalTranslator.signalTranslatorCoroutine);

            string signalMessage = message.Length < Plugin.MaxCharacters.Value ? message : message.Substring(0, Plugin.MaxCharacters.Value);

            signalTranslator.timesSendingMessage = timesSendingMessage;
            signalTranslator.signalTranslatorCoroutine = StartCoroutine(DisplaySignalTranslatorMessage(signalMessage, timesSendingMessage, signalTranslator));
        }

        private IEnumerator DisplaySignalTranslatorMessage(string signalMessage, int seed, SignalTranslator signalTranslator)
        {
            CustomLogger.Log(signalTranslator);
            
            System.Random signalMessageRandom = new System.Random(seed + StartOfRound.Instance.randomMapSeed);

            _original.signalTranslatorAnimator.SetBool("transmitting", true);

            signalTranslator.localAudio.Play();
            _original.UIAudio.PlayOneShot(signalTranslator.startTransmissionSFX, 1f);
            _original.signalTranslatorText.text = "";
            
            yield return new WaitForSeconds(1.21f);
            
            int i = 0;
            while (i < signalMessage.Length && !(signalTranslator == null) && signalTranslator.gameObject.activeSelf)
            {
                _original.UIAudio.PlayOneShot(signalTranslator.typeTextClips[UnityEngine.Random.Range(0, signalTranslator.typeTextClips.Length)]);
                _original.signalTranslatorText.text = _original.signalTranslatorText.text + signalMessage[i].ToString();
                float num = Mathf.Min((float)signalMessageRandom.Next(-1, 4) * 0.5f, 0f);
                yield return new WaitForSeconds(0.7f + num);
                int num2 = i;
                i = num2 + 1;
            }

            if (signalTranslator != null)
            {
                _original.UIAudio.PlayOneShot(signalTranslator.finishTypingSFX);
                signalTranslator.localAudio.Stop();
            }

            yield return new WaitForSeconds(0.5f);
            _original.signalTranslatorAnimator.SetBool("transmitting", false);
            yield break;
        }
    }
}
