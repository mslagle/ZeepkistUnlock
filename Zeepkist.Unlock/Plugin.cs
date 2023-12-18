using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zeepkist.Unlock
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static ConfigEntry<bool> UnlockCosmetics { get; private set; }
        public static ConfigEntry<bool> UnlockLevels { get; private set; }

        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Debug.Log($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            UnlockCosmetics = Config.Bind<bool>("Mod", "Click to unlock all cosmetics", false);
            UnlockLevels = Config.Bind<bool>("Mod", "Click to unlock all levels.  Requires game restart.", false);

            UnlockCosmetics.SettingChanged += UnlockCosmetics_SettingChanged;
            UnlockLevels.SettingChanged += UnlockLevels_SettingChanged;

            // Re-run every load if mod enabled
            if (UnlockCosmetics.Value == true)
            {
                UnlockCosmetics_SettingChanged(UnlockCosmetics, new EventArgs());
            }
            if (UnlockLevels.Value == true)
            {
                UnlockLevels_SettingChanged(UnlockLevels, new EventArgs());
            }
        }

        private void UnlockCosmetics_SettingChanged(object sender, EventArgs e)
        {
            var configObject = sender as ConfigEntryBase;
            var configValue = configObject.BoxedValue as bool?;
            if (configObject == null || configValue.Value == false)
            {
                return;
            }

            Debug.Log("Unlocking all cosmetics now...");

            var carts = PlayerManager.Instance.objectsList.wardrobe.everyZeepkist.Select(x => x.Value);
            var hats = PlayerManager.Instance.objectsList.wardrobe.everyHat.Select(x => x.Value);
            var skins = PlayerManager.Instance.objectsList.wardrobe.everyColor.Select(x => x.Value);

            var allItems = new List<CosmeticItemBase>();
            allItems.AddRange(carts);
            allItems.AddRange(hats);
            allItems.AddRange(skins);

            bool flag = false;
            foreach (var item in allItems)
            {
                if (PlayerManager.Instance.objectsList.wardrobe.UnlockNewCosmeticStep1(item))
                {
                    flag = true;
                    AvonturenUnlockData avonturenUnlockData = UnityEngine.Object.Instantiate<AvonturenUnlockData>(PlayerManager.Instance.avonturenUnlockDataPrefab);
                    avonturenUnlockData.transform.parent = PlayerManager.Instance.transform;
                    avonturenUnlockData.unlockItem = item;
                    avonturenUnlockData.unlockLevel = "";
                    PlayerManager.Instance.avonturenUnlocks.Add(avonturenUnlockData);

                    Debug.Log($"Unlocking item {item.name}");
                }
            }
            if (flag)
            {
                PlayerManager.Instance.objectsList.wardrobe.UnlockNewCosmeticStep2();
            }

            Debug.Log("All cosmetics now unlocked!");
        }

        private void UnlockLevels_SettingChanged(object sender, EventArgs e)
        {
            var configObject = sender as ConfigEntryBase;
            var configValue = configObject.BoxedValue as bool?;
            if (configObject == null || configValue.Value == false)
            {
                return;
            }

            Debug.Log("Unlocking all adventure levels...");

            var levels = ProgressionManager.Instance.AllAdventureLevels.Levels;
            foreach (var level in levels)
            {
                try
                {

                    bool hasUnLocked = ProgressionManager.Instance.HasLevelUnlocked(level.UID);
                    var time = ProgressionManager.Instance.GetAdventureTime(level.UID);

                    if (!hasUnLocked || time == null)
                    {
                        ProgressionManager.Instance.SetAdventureTime(level.UID, level.TimeAuthor - 1, new List<float>());
                        ProgressionManager.Instance.SetLevelUnlocked(level.UID);
                        ProgressionManager.Instance.SetLevelCompleted(level.UID);
                        Debug.Log($"Unlocked adventure level {level.name} [{level.UID}]");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            Debug.Log("All adventure levels unlocked!");
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}