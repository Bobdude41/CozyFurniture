using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CozyFurniture
{
    [HarmonyPatch]
    public class NetworkObjectManager
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)Plugin.networkBundle.LoadAsset("ExampleNetworkHandler");
            networkPrefab.AddComponent<NetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        static GameObject networkPrefab;
    }
}
