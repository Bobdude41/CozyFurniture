using BepInEx;
using HarmonyLib;
using Lucene.Net.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFurniture
{
    public class NetworkHandler : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            LevelEvent = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void EventClientRpc(string eventName)
        {
            LevelEvent?.Invoke(eventName); // If the event has subscribers (does not equal null), invoke the event
        }


        public static event Action<string> LevelEvent;

        public static NetworkHandler Instance { get; private set; }
    }
}
