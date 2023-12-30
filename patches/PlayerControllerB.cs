using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GameNetcodeStuff;
using HarmonyLib;

using UnityEngine;
using Unity.Netcode;

namespace LethalStreams.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
    public class PlayerControllerBOriginal
    {
        public static void Postfix(PlayerControllerB __instance)
        {
            var obj = PlayerControllerBPatched.Spawn(__instance);
            CustomLogger.LogInfo($"Captured PlayerControllerB instance.");
        }
    }

    public class PlayerControllerBPatched : MonoBehaviour
    {
        public PlayerControllerB _original = null;

        public static PlayerControllerBPatched Spawn(PlayerControllerB __instance)
        {
            var controller = __instance.gameObject.AddComponent<PlayerControllerBPatched>();
            CustomLogger.LogInfo($"Spawned PlayerControllerBPatched for {__instance.playerUsername}-{__instance.playerClientId.ToString()} Network ID: {controller.GetInstanceID().ToString()}.");
            
            controller._original = __instance;
            return controller;
        }

        public void DrainFlashlightBattery()
        {
            // get all flashlights in _original.ItemSlots
            var flashlights = _original.ItemSlots.Where(item => item != null && item.GetComponent<FlashlightItem>()).ToList();
            if (flashlights.Count == 0)
                return;
            
            foreach (var flashlight in flashlights)
            {
                var flashlightItem = flashlight.GetComponent<FlashlightItem>();
                var grabbableObject = flashlightItem.GetComponent<GrabbableObject>();
                
                // drain battery
                flashlightItem.insertedBattery.charge = 0f;
            }
        }
        
        public void DrainStamina()
        {
            _original.sprintMeter = 0f;
        }
        
        public void SetMaxSanity()
        {
            _original.insanityLevel = _original.maxInsanityLevel;
            _original.playersManager.fearLevel = 1f;
        }
        
        public void Rotate180()
        {
            StartCoroutine(LerpRotate180());
        }

        IEnumerator LerpRotate180(float rotateTime = 0.1f)
        {
            var startRotation = _original.transform.rotation;
            var endRotation = startRotation * Quaternion.Euler(0f, 180f, 0f);
            
            int random = UnityEngine.Random.Range(0, 2);
            if (random == 0)
                endRotation = startRotation * Quaternion.Euler(0f, -180f, 0f);

            float startTime = Time.time;
            float endTime = startTime + rotateTime;
            while (Time.time < endTime)
            {
                float time = (Time.time - startTime) / (endTime - startTime);
                _original.transform.rotation = Quaternion.Lerp(startRotation, endRotation, time);
                yield return null;
            }
            
            _original.transform.rotation = endRotation;
        }
        
        public static PlayerControllerBPatched GetLocalPlayer()
        {
            if (GameNetworkManager.Instance == null)
                return null;
            
            var localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer == null)
                return null;
            
            return localPlayer.GetComponent<PlayerControllerBPatched>();
        }

        public static PlayerControllerBPatched GetSpectatedPlayer()
        {
            if (GameNetworkManager.Instance == null)
                return null;
            
            var localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer == null)
                return null;
            
            var spectatedPlayer = localPlayer.GetComponent<PlayerControllerBPatched>()._original.spectatedPlayerScript;
            if (spectatedPlayer == null)
                return null;
            
            return spectatedPlayer.GetComponent<PlayerControllerBPatched>();
        }

        public static PlayerControllerBPatched GetCurrentPlayer(string username)
        {
            PlayerControllerBPatched player;
            PlayerControllerBPatched localPlayer = GetLocalPlayer();
            if (username != null)
                player = GetPlayer(username);
            else
                player = GetLocalPlayer();

            if (player._original.isPlayerDead)
                player = GetSpectatedPlayer();
            
            if (player != localPlayer)
                return null;
            
            return player;
        }
        
        public static PlayerControllerBPatched GetPlayer(string username)
        {
            var players = GetAllPlayers();
            foreach (var player in players)
            {
                if (player._original.playerUsername == username)
                    return player;
            }
            
            return null;
        }
        
        public static List<PlayerControllerBPatched> GetAllPlayers()
        {
            // find all playercontrollerb instances
            var playerNames = new List<PlayerControllerBPatched>();
            var playerControllers = FindObjectsOfType<PlayerControllerBPatched>();
            
            foreach (var playerController in playerControllers)
            {
                // if not spawned, skip
                if (!playerController.GetComponent<NetworkObject>().IsSpawned 
                    || playerController._original == null 
                    || !playerController._original.isPlayerControlled)
                    continue;
                
                playerNames.Add(playerController);
            }
            
            return playerNames;
        }

    }
}