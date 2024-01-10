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
using Random = System.Random;

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
        
        public static ConfigEntry<int> MaxSanityBitMin { get; set; }
        public static ConfigEntry<int> MaxSanityBitMax { get; set; }
        public static ConfigEntry<int> Rotate180BitMin { get; set; }
        public static ConfigEntry<int> Rotate180BitMax { get; set; }
        public static ConfigEntry<int> StaminaDrainBitMin { get; set; }
        public static ConfigEntry<int> StaminaDrainBitMax { get; set; }
        public static ConfigEntry<int> FlashlightDrainBitMin { get; set; }
        public static ConfigEntry<int> FlashlightDrainBitMax { get; set; }
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
            
            MaxSanityBitMin = Config.Bind(
                "Bits",
                "MaxSanityBitMin",
                10,
                "Minimum bits to set max sanity"
            );
            
            MaxSanityBitMax = Config.Bind(
                "Bits",
                "MaxSanityBitMax",
                50,
                "Maximum bits to set max sanity"
            );
            
            Rotate180BitMin = Config.Bind(
                "Bits",
                "Rotate180BitMin",
                50,
                "Minimum bits to rotate 180"
            );
            
            Rotate180BitMax = Config.Bind(
                "Bits",
                "Rotate180BitMax",
                100,
                "Maximum bits to rotate 180"
            );
            
            StaminaDrainBitMin = Config.Bind(
                "Bits",
                "StaminaDrainBitMin",
                100,
                "Minimum bits to drain stamina"
            );
            
            StaminaDrainBitMax = Config.Bind(
                "Bits",
                "StaminaDrainBitMax",
                150,
                "Maximum bits to drain stamina"
            );
            
            FlashlightDrainBitMin = Config.Bind(
                "Bits",
                "FlashlightDrainBitMin",
                150,
                "Minimum bits to drain flashlight"
            );
            
            FlashlightDrainBitMax = Config.Bind(
                "Bits",
                "FlashlightDrainBitMax",
                99999,
                "Maximum bits to drain flashlight"
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
                _socketManager = SocketManager.Spawn();
                _socketManager.Setup();

                if (!IsClient)
                {
                    _socketManager.socket.OnAnyInUnityThread((name, response) =>
                    {
                        JObject json;
                        JObject message;
                        string type;
                        
                        try
                        {
                            json = response.GetValue();

                            var arr = (JArray)json.GetValue("message");
                            message = (JObject)arr[0];
                            type = json.GetValue("type").ToString();
                        }
                        catch (Exception)
                        {
                            CustomLogger.LogInfo("failed to parse event, ignoring");
                            return;
                        }
                        
                        // check if type is any of the supported types (make them all lowercase using linq)
                        var supportedTypes = Enum.GetNames(typeof(StreamEventType)).Select(x => x.ToLower()).ToList();
                        if (supportedTypes.Contains(type))
                        {
                            StreamEventType eventType = (StreamEventType)Enum.Parse(typeof(StreamEventType), type, true);
                            CustomLogger.Log($"Received {eventType.ToString()} event");

                            switch (eventType)
                            {
                                case StreamEventType.Donation:
                                    Donation donation = message.ToObject<Donation>();
                                    StreamEvent.OnStreamDonation(donation);
                                    break;
                                case StreamEventType.Subscription:
                                    Subscription subscription = message.ToObject<Subscription>();
                                    StreamEvent.OnStreamSubscription(subscription);
                                    break;
                                case StreamEventType.Bits:
                                    Bits bits = message.ToObject<Bits>();
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
                Donation donation = new Donation()
                {
                    name = "test",
                    amount = 100,
                    message = "test message say: test"
                };
                StreamEvent.OnStreamDonation(donation);
            }

            Bits bits = new Bits()
            {
                name = "test",
                amount = 10,
                message = "test message"
            };

            // drain flashlight if pressed o
            if (Keyboard.current[Key.O].wasPressedThisFrame)
            {
                bits.amount = FlashlightDrainBitMin.Value + 1;
                if (new Random().Next(0, 2) == 0)
                    bits.message = "emiru";

                StreamEvent.OnStreamBits(bits);
            }
            
            // drain stamina if pressed l
            if (Keyboard.current[Key.L].wasPressedThisFrame)
            {
                bits.amount = StaminaDrainBitMin.Value + 1;
                if (new Random().Next(0, 2) == 0)
                    bits.message = "emiru";

                StreamEvent.OnStreamBits(bits);
            }
            
            // set max sanity if pressed k
            if (Keyboard.current[Key.K].wasPressedThisFrame)
            {
                bits.amount = MaxSanityBitMin.Value + 1;
                if (new Random().Next(0, 2) == 0)
                    bits.message = "emiru";

                StreamEvent.OnStreamBits(bits);
            }
            
            // rotate 180 if pressed j
            if (Keyboard.current[Key.J].wasPressedThisFrame)
            {
                bits.amount = Rotate180BitMin.Value + 1;
                if (new Random().Next(0, 2) == 0)
                    bits.message = "emiru";

                StreamEvent.OnStreamBits(bits);
            }
            
            // play airhorn if pressed h
            if (Keyboard.current[Key.H].wasPressedThisFrame)
            {
                Subscription sub = new Subscription()
                {
                    name = "test",
                    message = "test message"
                };
                
                if (new Random().Next(0, 2) == 0)
                    sub.message = "emiru";

                StreamEvent.OnStreamSubscription(sub);
            }
        }
#endif
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
