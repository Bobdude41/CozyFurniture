using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Lucene.Net.Support;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LethalLib;
using LethalLib.Modules;
using JetBrains.Annotations;
using System.Net;

namespace CozyFurniture
{
    [HarmonyPatch]
    public static class SpacePatcher
    {
        //public Collider shipInnerRoomBounds;

        public static ManualLogSource mls;

        public static bool inSpace = false;

        public static Plugin pluginInstance;

        public static GameObject shipInstantiation;

        public static AutoParentToShip[] furnitureArray;

        public static HashSet<string> blacklistedFurnitureObjects = new HashSet<string>();

        public static HashSet<AutoParentToShip> toggledFurniture = new HashSet<AutoParentToShip>();

        public static GameObject shipRails;
        public static GameObject shipRailsPost;



        [HarmonyPatch(typeof(StartOfRound), "TeleportPlayerInShipIfOutOfRoomBounds")]
        [HarmonyPrefix]
        public static bool TeleportPlayerInShipIfOutOfRoomBounds()
        {
            if (inSpace)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void SpawnShip()
        {

        }


        [HarmonyPatch(typeof(ShipBuildModeManager), "PlayerMeetsConditionsToBuild")]
        [HarmonyPostfix]
        public static void PlayerMeetsConditionsToBuild(ref bool __result)
        {
            PlayerControllerB playerObject = GameObject.FindObjectOfType<PlayerControllerB>();

            __result = inSpace;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "Awake")]
        static void AwakeServerRpc()
        {
            NetworkHandler.LevelEvent += ReceivedEventFromServer;
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            shipInstantiation = GameObject.Instantiate(Plugin.shipObject, new Vector3(-13.7527f, 0.96f, -15.52f), Quaternion.identity);
            //shipInstantiation.SetActive(false);

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(shipInstantiation);
            toggledFurniture = new HashSet<AutoParentToShip>();
            mls.LogInfo("Server side Starting game");


            shipRails = GameObject.Find("ShipRails");
            shipRailsPost = GameObject.Find("ShipRailPosts");

            if (shipInstantiation != null)
            {
                shipInstantiation.transform.Rotate(new Vector3(0, 1, 0), 180);
                shipInstantiation.transform.Rotate(new Vector3(1, 0, 0), 270);
            }

            blacklistedFurnitureObjects.Add("FileCabinet");
            blacklistedFurnitureObjects.Add("Terminal");
            blacklistedFurnitureObjects.Add("Bunkbeds");
            blacklistedFurnitureObjects.Add("StorageCloset");
            blacklistedFurnitureObjects.Add("Teleporter(Clone)");
            blacklistedFurnitureObjects.Add("ShipHorn(Clone)");
            blacklistedFurnitureObjects.Add("InverseTeleporter(Clone)");
            blacklistedFurnitureObjects.Add("SignalTranslator(Clone)");

            GameObject.Find("ShipRails").SetActive(true);
            GameObject.Find("ShipRailPosts").SetActive(true);

        }


        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.InitializeRandomNumberGenerators))]
        static void SubscribeToHandler()
        {
            NetworkHandler.LevelEvent += ReceivedEventFromServer;

            inSpace = false;
            shipInstantiation.SetActive(false);
            furnitureArray = UnityEngine.Object.FindObjectsOfType<AutoParentToShip>();
            RoundManager.Instance.playersManager.shipDoorsAnimator.SetBool("Closed", value: true);
            shipRails.SetActive(true);
            shipRailsPost.SetActive(true);

            BoxCollider innerCollider = GameObject.Find("ShipInnerRoomBoundsTrigger").GetComponent<BoxCollider>();

            innerCollider.center = new Vector3(0, 0, 0);
            innerCollider.size = new Vector3(1, 0, 0.9999999f);
            mls.LogInfo("Server side Starting game");



            if (furnitureArray != null)
            {
                mls.LogInfo(furnitureArray);
                mls.LogInfo("Found furniture array");

                foreach (AutoParentToShip a in furnitureArray)
                {
                    if (!blacklistedFurnitureObjects.Contains(a.gameObject.name))
                    {
                        a.gameObject.SetActive(false);
                        toggledFurniture.Add(a);
                    }
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        static void UnsubscribeFromHandler()
        {
            NetworkHandler.LevelEvent -= ReceivedEventFromServer;

            inSpace = true;
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            UnityEngine.Object.FindObjectOfType<HangarShipDoor>().SetDoorButtonsEnabled(doorButtonsEnabled: true);


            BoxCollider innerCollider = GameObject.Find("ShipInnerRoomBoundsTrigger").GetComponent<BoxCollider>();

            innerCollider.center = new Vector3(-0.487964f, 0, 0);
            innerCollider.size = new Vector3(1.975929f, 1, 1);

            furnitureArray = UnityEngine.Object.FindObjectsOfType<AutoParentToShip>();
            shipInstantiation.SetActive(true);

            shipRails.SetActive(false);
            shipRailsPost.SetActive(false);


            if (toggledFurniture != null && furnitureArray != null)
            {
                mls.LogInfo(furnitureArray);
                mls.LogInfo("Found furniture array");

                foreach (AutoParentToShip a in toggledFurniture.ToList())
                {
                    if (!blacklistedFurnitureObjects.Contains(a.gameObject.name))
                    {
                        a.gameObject.SetActive(true);
                        toggledFurniture.Remove(a);
                    }
                }
            }
        }

        static void ReceivedEventFromServer(string eventName)
        {
            // Event Code Here
        } 

        static void SendEventToClients(string eventName)
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return;

            NetworkHandler.Instance.EventClientRpc(eventName);
        }
    }
}
