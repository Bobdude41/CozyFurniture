using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using CozyFurniture;

namespace CozyFurniture
{

    [Serializable]
    [HarmonyPatch]
    public class ConfigSync
    {

        public static bool isSynced = false;
        public static ConfigSync defaultConfig;
        public static ConfigSync instance;

        public bool syncUnlockEverything;
        public bool syncShareEverything;
        public bool syncSyncUnsharedEmotes;
        public bool syncEnableMovingWhileEmoting;
        public bool syncDisableRaritySystem;

        public int syncStartingEmoteCredits;
        public float syncAddEmoteCreditsMultiplier;
        public bool syncPurchaseEmotesWithDefaultCurrency;

        public float syncPriceMultiplierEmotesStore;
        public int syncBasePriceEmoteTier0;
        public int syncBasePriceEmoteTier1;
        public int syncBasePriceEmoteTier2;
        public int syncBasePriceEmoteTier3;

        public int syncNumEmotesStoreRotation;
        public float syncRotationChanceEmoteTier0;
        public float syncRotationChanceEmoteTier1;
        public float syncRotationChanceEmoteTier2;
        public float syncRotationChanceEmoteTier3;

        public bool syncEnableMaskedEnemiesEmoting;
        public float syncMaskedEnemiesEmoteChanceOnEncounter;
        public bool syncMaskedEnemiesAlwaysEmoteOnFirstEncounter;
        public bool syncOverrideStopAndStareDuration;
        public float syncMaskedEnemyEmoteRandomDelayMin;
        public float syncMaskedEnemyEmoteRandomDelayMax;
        public float syncMaskedEnemyEmoteRandomDurationMin;
        public float syncMaskedEnemyEmoteRandomDurationMax;

        public static Vector2 syncMaskedEnemyEmoteRandomDelay;
        public static Vector2 syncMaskedEnemyEmoteRandomDuration;

        //public static int syncNumMysteryEmotesStoreRotation;

        public static HashSet<ulong> syncedClients;


        public ConfigSync()
        {

            if (syncUnlockEverything)
                syncShareEverything = true;
            //syncNumMysteryEmotesStoreRotation = ConfigSettings.numMysteryEmotesStoreRotation.Value;
        }


        public Vector2 ParseVector2FromString(string str)
        {
            Vector2 vector = Vector2.zero;
            try
            {
                string[] values = str.Split(',');
                if (float.TryParse(values[0].Trim(' '), out float x) && float.TryParse(values[1].Trim(' '), out float y))
                    vector = new Vector2(Mathf.Min(Mathf.Abs(x), Mathf.Abs(y)), Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)));
                return vector;
            }
            catch { }
            return Vector2.zero;
        }


        public static void BuildDefaultConfigSync()
        {
            defaultConfig = new ConfigSync();
            instance = new ConfigSync();
        }



        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void ResetValues()
        {
            isSynced = false;
        }




        private static void OnRequestConfigSyncClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsClient)
                return;

            int dataLength;
            reader.ReadValueSafe(out dataLength);
            if (reader.TryBeginRead(dataLength))
            {
                byte[] bytes = new byte[dataLength];
                reader.ReadBytesSafe(ref bytes, dataLength);
                instance = DeserializeFromByteArray(bytes);
                if (instance.syncUnlockEverything)
                    instance.syncShareEverything = true;
                syncMaskedEnemyEmoteRandomDelay = new Vector2(instance.syncMaskedEnemyEmoteRandomDelayMin, instance.syncMaskedEnemyEmoteRandomDelayMax);
                syncMaskedEnemyEmoteRandomDuration = new Vector2(instance.syncMaskedEnemyEmoteRandomDurationMin, instance.syncMaskedEnemyEmoteRandomDurationMax);
                isSynced = true;
                return;
            }
        }



        public static byte[] SerializeConfigToByteArray(ConfigSync config)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, config);
            return memoryStream.ToArray();
        }

        public static ConfigSync DeserializeFromByteArray(byte[] data)
        {
            MemoryStream s = new MemoryStream(data);
            BinaryFormatter b = new BinaryFormatter();
            return (ConfigSync)b.Deserialize(s);
        }
    }
}