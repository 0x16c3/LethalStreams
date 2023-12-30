using System;
using System.Collections.Generic;

using UnityEngine;

using LethalStreams.Patches;

namespace LethalStreams.Streamlabs
{
    public class StreamEvent
    {
        public static PlayerControllerBPatched FindUsernameInMessage(List<PlayerControllerBPatched> playerList, string message)
        {
            // find username in message, ignore case
            var username = playerList.Find(player => message.ToLower().Contains(player._original.playerUsername.ToLower()));
            return username;
        }

        public static void OnStreamDonation(Donation donate)
        {
            CustomLogger.Log($"Donation received: {donate.message} - {donate.amount.ToString()} - {donate.from}");

            if (donate.amount < Plugin.MinDonationAmount.Value)
                return;

            // try to find prefix in donation message
            int index = donate.message.IndexOf(Plugin.DonationPrefix.Value, StringComparison.Ordinal);
            if (index > 0)
            {
                string signalMessage = donate.message.Substring(index + Plugin.DonationPrefix.Value.Length);
                signalMessage = signalMessage.Trim();
                signalMessage = signalMessage.Length < Plugin.MaxCharacters.Value ? signalMessage : signalMessage.Substring(0, Plugin.MaxCharacters.Value);

                CustomLogger.Log($"Transmitting message: {signalMessage}");
                HUDManagerNetworkPatched.Instance.UseSignalTranslatorServerRpc(signalMessage);
            }
        }

        public static void OnStreamBits(Bits bits)
        {
            CustomLogger.Log($"Bits received: {bits.message} - {bits.amount.ToString()} - {bits.name}");

            bool InRange(int amount, Vector2Int range)
            {
                return amount >= range.x && amount <= range.y;
            }

            List<PlayerControllerBPatched> players = PlayerControllerBPatched.GetAllPlayers();
            string username = FindUsernameInMessage(players, bits.message)._original.playerUsername;

            // if range is in bits, do action
            if (InRange(bits.amount, Plugin.FlashlightDrainBitRange.Value))
                HUDManagerNetworkPatched.Instance.DrainFlashlightBatteryServerRpc(username);
            else if (InRange(bits.amount, Plugin.StaminaDrainBitRange.Value))
                HUDManagerNetworkPatched.Instance.DrainStaminaServerRpc(username);
            else if (InRange(bits.amount, Plugin.Rotate180BitRange.Value))
                HUDManagerNetworkPatched.Instance.Rotate180ServerRpc(username);
            else if (InRange(bits.amount, Plugin.MaxSanityBitRange.Value))
                HUDManagerNetworkPatched.Instance.SetMaxSanityServerRpc(username);
        }
        
        public static void OnStreamSubscription(Subscription sub)
        {
            CustomLogger.Log($"Subscription received: {sub.message} - {sub.name} - {sub.months}");

            List<PlayerControllerBPatched> players = PlayerControllerBPatched.GetAllPlayers();
            string username = FindUsernameInMessage(players, sub.message)._original.playerUsername;

            // if range is in bits, do action
            if (Plugin.PlayAirhornSub.Value)
                HUDManagerNetworkPatched.Instance.SetMaxSanityServerRpc(username);
        }
    }

    public enum StreamEventType
    {
        Donation,
        Subscription,
        Follow,
        Host,
        Bits,
        Raid
    }

    public static class StreamEventTypeExtensions
    {
        public static string ToString(this StreamEventType eventType)
        {
            return eventType.ToString().ToLower();
        }
    }

    public class Donation : StreamEvent
    {
        public int id { get; set; }
        public string name { get; set; }
        public int amount { get; set; }
        public string formattedAmount { get; set; }
        public string message { get; set; }
        public string currency { get; set; }
        public string from { get; set; }
    }

    public class Follow : StreamEvent
    {
        public string name { get; set; }
    }

    public class Subscription : StreamEvent
    {
        public string name { get; set; }
        public string months { get; set; }
        public string message { get; set; }
    }

    public class Host : StreamEvent
    {
        public string name { get; set; }
        public string viewers { get; set; }
        public string type { get; set; }
    }

    public class Bits : StreamEvent
    {
        public string name { get; set; }
        public int amount { get; set; }
        public string message { get; set; }

        public string SanitizedMessage()
        {
            // remove cheeramount from message 
            var message = this.message;
            var cheerString = $"cheer{amount.ToString()}";
            
            if (message.Contains(cheerString))
            {
                message = message.Replace(cheerString, "");
            }
            
            return message;
        }
    }

    public class Raid : StreamEvent
    {
        public string name { get; set; }
        public string viewers { get; set; }
    }
}

