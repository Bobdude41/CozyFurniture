using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using BepInEx.Logging;
using System.Linq;
using static Unity.Audio.Handle;
using Unity.Netcode;

//TooManyEmotes thank you for the custom currency code
namespace CozyFurniture
{
    [HarmonyPatch]
    public static class TerminalPatch
    {

        public static ManualLogSource mls;

        public static Terminal terminalInstance;

        public static int currentFurnitureCredits;
        public static Dictionary<string, int> currentFurnitureCreditsByPlayer;

        public static List<TerminalNode> ShipFurnitureSelection = new List<TerminalNode>();


        public static bool initializedTerminalNodes = false;

        static string confirmFurnitureOpeningText = "You have requested to order a new piece of furniture.";


        [HarmonyPatch(typeof(Terminal), "Awake")]
        [HarmonyPostfix]
        public static void InitializeTerminal(Terminal __instance)
        {
            terminalInstance = __instance;
            currentFurnitureCreditsByPlayer = new Dictionary<string, int>();
            if (!initializedTerminalNodes)
                EditExistingTerminalNodesForFurniture();
        }

        [HarmonyPatch(typeof(Terminal), "RotateShipDecorSelection")]
        [HarmonyPrefix]
        public static bool HijackDecor()
        {
            NetworkHandler.LevelEvent += ReceivedEventFromServer;

            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 65);
            terminalInstance.ShipDecorSelection.Clear();
            List<TerminalNode> list = new List<TerminalNode>();
            List<TerminalNode> furnitureList = new List<TerminalNode>();
            for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++)
            {

                if (StartOfRound.Instance.unlockablesList.unlockables[i].IsPlaceable || StartOfRound.Instance.unlockablesList.unlockables[i].suitMaterial == null)
                {
                    if (StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode != null && !StartOfRound.Instance.unlockablesList.unlockables[i].alwaysInStock)
                    {
                        furnitureList.Add(StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode);
                        StartOfRound.Instance.unlockablesList.unlockables[i].alwaysInStock = true;
                    }
                }
                else
                {
                    if (StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode != null && !StartOfRound.Instance.unlockablesList.unlockables[i].alwaysInStock)
                    {
                        list.Add(StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode);
                    }
                }
            }
            int num = random.Next(4, 6);
            for (int j = 0; j < num; j++)
            {
                if (list.Count <= 0)
                {
                    break;
                }
                TerminalNode furnitureItem = furnitureList[random.Next(0, furnitureList.Count)];
                ShipFurnitureSelection.Add(furnitureItem);
                furnitureList.Remove(furnitureItem);

                TerminalNode item = list[random.Next(0, list.Count)];
                terminalInstance.ShipDecorSelection.Add(item);
                list.Remove(item);
            }
            return false;
        }

        static void ReceivedEventFromServer(string eventName)
        {
            // Event Code Here
        }

        [HarmonyPatch(typeof(Terminal), "LoadNewNodeIfAffordable")]
        [HarmonyPostfix]
        public static void GenerateFurniture(ref TerminalNode node)
        {
            if (node.shipUnlockableID != -1) 
            {
                UnlockableItem unlockableItem2 = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];
                if ((!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded) || StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
                {
                    terminalInstance.LoadNewNode(terminalInstance.terminalNodes.specialNodes[15]);
                    return;
                }
                if (!ShipFurnitureSelection.Contains(node) && !unlockableItem2.alwaysInStock && (!node.buyUnlockable || unlockableItem2.shopSelectionNode == null))
                {
                    Debug.Log("Not in stock, node: " + node.name);
                    terminalInstance.LoadNewNode(terminalInstance.terminalNodes.specialNodes[16]);
                    return;
                }
            }
        }



        public static void EditExistingTerminalNodesForFurniture()
        {
            initializedTerminalNodes = true;
            foreach (TerminalNode node in terminalInstance.terminalNodes.specialNodes)
            {
                if (node.name == "Start")
                {
                    string keyword = "Type \"Help\" for a list of commands.";
                    int insertIndex = node.displayText.IndexOf(keyword);
                    if (insertIndex != -1)
                    {
                        insertIndex += keyword.Length;
                        string addText = "\n\n[CozyFurniture]\nType \"Furniture\" for a list of commands.";
                        node.displayText = node.displayText.Insert(insertIndex, addText);
                    }
                    else
                        Debug.LogError("Failed to add furniture tip to terminal. Maybe an update broke it?");
                }

                else if (node.name == "HelpCommands")
                {
                    string keyword = "[numberOfItemsOnRoute]";
                    int insertIndex = node.displayText.IndexOf(keyword);
                    if (insertIndex != -1)
                    {
                        string addText = ">FURNITURE\n" +
                            "For a list of Furniture commands.\n\n";
                        node.displayText = node.displayText.Insert(insertIndex, addText);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        public static void TextPostProcessFurniture(ref string modifiedDisplayText, TerminalNode node)
        {
            if (modifiedDisplayText.Length <= 0)
                return;

            if (modifiedDisplayText.Contains("[[[[furnitureUnlockablesSelectionList]]]]") || (modifiedDisplayText.Contains("[[[[") && modifiedDisplayText.Contains("]]]]")))
            {
                int index0 = modifiedDisplayText.IndexOf("[[[[");
                int index1 = modifiedDisplayText.IndexOf("]]]]") + 3;
                string textToReplace = modifiedDisplayText.Substring(index0, index1 - index0);
                string replacementTextFurniture = "";
                replacementTextFurniture += "Remaining furniture credit balance: $" + currentFurnitureCredits + ".\n";
                replacementTextFurniture += "\n";

                StringBuilder stringBuilder5 = new StringBuilder();
                for (int m = 0; m < ShipFurnitureSelection.Count; m++)
                {
                    stringBuilder5.Append($"\n{ShipFurnitureSelection[m].creatureName}  //  ${ShipFurnitureSelection[m].itemCost}");
                }
                replacementTextFurniture += stringBuilder5.ToString();


                if (ShipFurnitureSelection == null || ShipFurnitureSelection.Count <= 0)
                {
                    modifiedDisplayText = modifiedDisplayText.Replace("[[[[furnitureUnlockablesSelectionList]]]]", "[No items available]");
                }

                modifiedDisplayText = modifiedDisplayText.Replace(textToReplace, replacementTextFurniture);
            }
        }

        [HarmonyPatch(typeof(Terminal), "ParsePlayerSentence")]
        [HarmonyPrefix]
        public static bool ParsePlayerSentence(ref TerminalNode __result, Terminal __instance)
        {
            if (__instance.screenText.text.Length <= 0)
                return true;

            string input = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            string[] args = input.Split(' ');

            if (args.Length == 0)
                return true;

            if (!ConfigSync.isSynced)
            {
                if (input.StartsWith("furniture"))
                {
                    __result = BuildTerminalNodeHomeFurniture();
                    return false;
                }
                else
                    return true;
            }


            if (input.StartsWith("furnitures"))
                input = input.Replace("furnitures", "furniture");


            if (input.StartsWith("furniture"))
            {
                if (input == "furniture")
                {
                    __result = BuildTerminalNodeHomeFurniture();
                    return false;
                }
                input = input.Substring(6);
            }
            else
            {
                return true;
            }

            return true;
        }

        static TerminalNode BuildTerminalNodeHomeFurniture()
        {

            TerminalNode homeTerminalNodeFurniture = new TerminalNode
            {
                displayText = "[CozyFurniture]\n\n" +
                    "Store\n" +
                    "------------------------------\n" +
                    "[[[[furnitureUnlockablesSelectionList]]]]\n\n",
                clearPreviousText = true,
                acceptAnything = false
            };

            return homeTerminalNodeFurniture;
        }

        static TerminalNode BuildCustomTerminalNode(string displayText, bool clearPreviousText = false, bool acceptAnything = false, bool isConfirmationNode = false)
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = displayText,
                clearPreviousText = clearPreviousText,
                acceptAnything = false,
                isConfirmationNode = isConfirmationNode
            };
            return terminalNode;
        }

        //[HarmonyPatch(typeof(DepositItemsDesk), "SellItemsClientRpc")]
        //[HarmonyPostfix]
        //public static void GainFurnitureCredits(int itemProfit, int newGroupCredits, int itemsSold, float buyingRate, DepositItemsDesk __instance)
        //{
        //    if (((int)Traverse.Create(__instance).Field("__rpc_exec_stage").GetValue()) == 2 && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        //    {
        //        int furnitureProfit = (int)(itemProfit * ConfigSync.instance.syncAddEmoteCreditsMultiplier);
        //        mls.LogInfo("Gained " + itemProfit + " group credits.");
        //        mls.LogInfo("Gained " + furnitureProfit + " furniture credits. GainFurnitureCreditsMultiplier: " + ConfigSync.instance.syncAddEmoteCreditsMultiplier);
        //        currentFurnitureCredits += furnitureProfit;
        //    }
        //}
    }
}
