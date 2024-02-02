using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace CozyFurniture
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        private static Plugin instance;

        public static AssetBundle bundle;
        public static AssetBundle networkBundle;

        public static GameObject shipObject;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        internal ManualLogSource mls;

        void Awake()
        {
            NetcodePatcher();
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            bundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "ship"));
            shipObject = bundle.LoadAsset<GameObject>("Assets/ShipInside (1).prefab");

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(shipObject);
            Utilities.FixMixerGroups(shipObject);


            SpawnNetworkManager();

            [ServerRpc]
            void SpawnNetworkManager()
            {
                var dllFolderPath = Path.GetDirectoryName(Info.Location);
                var assetBundleFilePath = Path.GetFileName(dllFolderPath);
                networkBundle = AssetBundle.LoadFromFile(assetBundleFilePath);
            }

            if (bundle == null)
            {
                mls.LogError("Failed to load custom assets.");
                return;
            }

            if (instance == null)
            {
                instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            mls.LogInfo("CozyFurniture initalized");
            mls.LogInfo("Mothership Asset: " + bundle);

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(TerminalPatch));
            harmony.PatchAll(typeof(SpacePatcher));
        }


        private static void NetcodePatcher()
        {
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
        }

        void Update()
        {

        }
    }
}
