using System;
using System.Reflection;
using System.Linq;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.InputSystem;

using LethalStreams.Patches;
using LethalStreams.Streamlabs;

using Newtonsoft.Json.Linq;

namespace LethalStreams
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);
        private static Plugin _instance;
        
        private SocketManager _socketManager;

        public bool IsClient => _socketManager.isClient;
        
        public static ConfigEntry<string> StreamlabsToken { get; set; }
        public static ConfigEntry<int> MaxCharacters { get; set; }
        public static ConfigEntry<bool> SpawnTransmitter { get; set; }
        public static ConfigEntry<int> MinDonationAmount { get; set; }
        public static ConfigEntry<string> DonationPrefix { get; set; }
        
        public static ConfigEntry<Vector2Int> MaxSanityBitRange { get; set; }
        public static ConfigEntry<Vector2Int> Rotate180BitRange { get; set; }
        public static ConfigEntry<Vector2Int> StaminaDrainBitRange { get; set; }
        public static ConfigEntry<Vector2Int> FlashlightDrainBitRange { get; set; }
        public static ConfigEntry<bool> PlayAirhornSub { get; set; }
        
        private void Awake()
        {
            if (_instance == null)
                _instance = this;

            // mark as persistent
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(this);
            
            StreamlabsToken = Config.Bind(
                "General",
                "StreamlabsToken",
                "",
                "Streamlabs Socket API token (leave empty for client mode)"
            );
            
            MaxCharacters = Config.Bind(
                "General",
                "MaxMessageLength",
                30,
                "The maximum of characters that can be displayed on donation messages"
            );
            
            SpawnTransmitter = Config.Bind(
                "General",
                "SpawnTransmitter",
                true,
                "Spawn a signal transmitter if it doesn't exist"
            );
            
            MinDonationAmount = Config.Bind(
                "General",
                "MinDonationAmount",
                50,
                "Minimum donation amount to trigger a message"
            );
            
            DonationPrefix = Config.Bind(
                "General",
                "DonationPrefix",
                "say:",
                "Prefix to trigger a donation message"
            );
            
            MaxSanityBitRange = Config.Bind(
                "Bits",
                "MaxSanityBitRange",
                new Vector2Int(10, 50),
                "Range of bits to set max sanity"
            );
            
            Rotate180BitRange = Config.Bind(
                "Bits",
                "Rotate180BitRange",
                new Vector2Int(50, 100),
                "Range of bits to rotate 180"
            );
            
            StaminaDrainBitRange = Config.Bind(
                "Bits",
                "StaminaDrainBitRange",
                new Vector2Int(100, 150),
                "Range of bits to drain stamina"
            );
            
            FlashlightDrainBitRange = Config.Bind(
                "Bits",
                "FlashlightDrainBitRange",
                new Vector2Int(150, 9999),
                "Range of bits to drain flashlight"
            );
            
            PlayAirhornSub = Config.Bind(
                "Bits",
                "PlayAirhornSub",
                true,
                "Play airhorn sound on sub"
            );
            
            CustomLogger.Initialize(PluginInfo.PLUGIN_GUID);
            
            StartOfRoundPatched.Spawn();

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            CustomLogger.Log($"Patched network types");
            
            CustomLogger.Log($"Setting up streamlabs client");
            {
                _socketManager.Spawn();
                _socketManager.Setup();

                if (!IsClient)
                {
                    _socketManager.socket.OnAnyInUnityThread((name, response) =>
                    {
                        JObject json = response.GetValue();
                        string type = json.GetValue("type").ToString();

                        // get streamevent from json 
                        JArray message = (JArray)json.GetValue("message");

                        // check if type is any of the supported types (make them all lowercase using linq)
                        var supportedTypes = Enum.GetNames(typeof(StreamEventType)).Select(x => x.ToLower()).ToList();
                        if (supportedTypes.Contains(type))
                        {
                            StreamEventType eventType = (StreamEventType)Enum.Parse(typeof(StreamEventType), type, true);
                            CustomLogger.Log($"Received {eventType.ToString()} event");

                            switch (eventType)
                            {
                                case StreamEventType.Donation:
                                    Donation donation = message[0].ToObject<Donation>();
                                    StreamEvent.OnStreamDonation(donation);
                                    break;
                                case StreamEventType.Subscription:
                                    Subscription subscription = message[0].ToObject<Subscription>();
                                    StreamEvent.OnStreamSubscription(subscription);
                                    break;
                                case StreamEventType.Bits:
                                    Bits bits = message[0].ToObject<Bits>();
                                    StreamEvent.OnStreamBits(bits);
                                    break;
                                case StreamEventType.Follow:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    });
                }
            }
            
            _harmony.PatchAll();
            CustomLogger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} loaded");
        }
        
#if DEBUG
        public void Update()
        {
            if (Keyboard.current[Key.P].wasPressedThisFrame)
            {
                CustomLogger.Log("action");
                TransmitMessage("test test test test");
            }
            
            // drain flashlight if pressed o
            if (Keyboard.current[Key.O].wasPressedThisFrame)
            {
                CustomLogger.Log("draining flashlight");
                HUDManagerNetworkPatched.Instance.DrainFlashlightBatteryServerRpc();
            }
            
            // drain stamina if pressed l
            if (Keyboard.current[Key.L].wasPressedThisFrame)
            {
                CustomLogger.Log("draining stamina");
                HUDManagerNetworkPatched.Instance.DrainStaminaServerRpc();
            }
            
            // set max sanity if pressed k
            if (Keyboard.current[Key.K].wasPressedThisFrame)
            {
                CustomLogger.Log("setting max sanity");
                HUDManagerNetworkPatched.Instance.SetMaxSanityServerRpc();
            }
            
            // rotate 180 if pressed j
            if (Keyboard.current[Key.J].wasPressedThisFrame)
            {
                CustomLogger.Log("rotating 180");
                HUDManagerNetworkPatched.Instance.Rotate180ServerRpc();
            }
            
            // play airhorn if pressed h
            if (Keyboard.current[Key.H].wasPressedThisFrame)
            {
                CustomLogger.Log("playing airhorn");
                HUDManagerNetworkPatched.Instance.MakeAirHornNoiseServerRpc();
            }
        }
#endif
        
        void TransmitMessage(string message)
        {
            CustomLogger.Log($"Transmitting message: {message}");
            HUDManagerNetworkPatched.Instance.UseSignalTranslatorServerRpc(message);
        }
    }

    internal static class CustomLogger
    {
        internal static ManualLogSource logSource;

        public static void Initialize(string modGUID)
        {
            logSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        public static void Log(object message)
        {
            logSource.LogInfo(message);
        }

        public static void LogError(object message)
        {
            logSource.LogError(message);
        }

        public static void LogWarning(object message)
        {
            logSource.LogWarning(message);
        }
        
        public static void LogInfo(object message)
        {
            logSource.LogInfo(message);
        }
    }
}
