using System;
using System.Collections.Generic;

using UnityEngine;

using SocketIOClient;

namespace LethalStreams
{
    public class SocketManager : MonoBehaviour
    {
        public static SocketManager Instance { get; private set; }
        public static bool IsLoaded { get; private set; }

        public SocketIOUnity socket;
        public bool isClient = false;

        public void Awake()
        {
            Instance = this;

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(this);

            IsLoaded = true;
        }

        public void Spawn()
        {
            var gameobject = new GameObject("SocketManager");
            gameobject.AddComponent<SocketManager>();
        }

        public void Setup()
        {
            // if token is empty, use client mode
            if (Plugin.StreamlabsToken.Value == "")
            {
                isClient = true;
                CustomLogger.Log("Using client mode");
                return;
            }

            var uri = new Uri("https://sockets.streamlabs.com");
            socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", Plugin.StreamlabsToken.Value }
                },
                EIO = 4,
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            socket.OnConnected += (sender, e) =>
            {
                CustomLogger.Log("Connected");
                Debug.Log("connected");
            };
            socket.Connect();
        }
    }
}