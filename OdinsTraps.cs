using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using ServerSync;
using PieceManager;
using UnityEngine;

namespace OdinsTraps
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class OdinsTraps : BaseUnityPlugin
    {
        private const string ModName = "OdinsTraps";
        private const string ModVersion = "1.0.0";
        private const string ModGUID = "com.odinplus.odinstraps";
        private static Harmony harmony = null!;

        internal static SE_Stats? SE_Trapped;
        internal static Item? UnplacedMetalTrap;
        internal static ConfigEntry<float> Trapped;


        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(ServerConfigLocked);
            Item UnplacedMetalTrap = new("odinstrap", "Unplaced_Metal_Trap");  //assetbundle name, Asset Name
            UnplacedMetalTrap.Crafting.Add(CraftingTable.Forge, 2);
            UnplacedMetalTrap.RequiredItems.Add("Iron", 6);
            UnplacedMetalTrap.RequiredItems.Add("BlackMetal", 1);
            UnplacedMetalTrap.CraftAmount = 3;
            Item LureBlade = new("odinstrap", "OdinsLureBlade");  //assetbundle name, Asset Name
            LureBlade.Crafting.Add(CraftingTable.Forge, 2);
            LureBlade.RequiredItems.Add("Wood", 6);
            LureBlade.RequiredItems.Add("Iron", 2);
            LureBlade.CraftAmount = 1;
            Item LureCannon = new("odinstrap", "OdinsLureCannon");  //assetbundle name, Asset Name
            LureCannon.Crafting.Add(CraftingTable.Forge, 2);
            LureCannon.RequiredItems.Add("Wood", 6);
            LureCannon.RequiredItems.Add("Iron", 2);
            LureCannon.CraftAmount = 1;
            BuildPiece Metal_Trap = new("odinstrap", "Odins_Metal_Trap");
            Metal_Trap.Name.English("Odins Metal Trap");
            Metal_Trap.Description.English("It's a trap!");
            Metal_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
            BuildPiece Fire_Trap = new("odinstrap", "Odins_Fire_Trap");
            Fire_Trap.Name.English("Odins Fire Trap");
            Fire_Trap.Description.English("It's a fire trap!");
            Fire_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
            Fire_Trap.RequiredItems.Add("SurtlingCore", 1, true);
            BuildPiece Frost_Trap = new("odinstrap", "Odins_Frost_Trap");
            Frost_Trap.Name.English("Odins Frost Trap");
            Frost_Trap.Description.English("It's a frost trap!");
            Frost_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
            Frost_Trap.RequiredItems.Add("FreezeGland", 1, true);
            BuildPiece Lightning_Trap = new("odinstrap", "Odins_Lightning_Trap");
            Lightning_Trap.Name.English("Odins Lightning Trap");
            Lightning_Trap.Description.English("It's a lightning trap!");
            Lightning_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
            Lightning_Trap.RequiredItems.Add("Needle", 1, true);
            BuildPiece Poison_Trap = new("odinstrap", "Odins_Poison_Trap");
            Poison_Trap.Name.English("Odins Poison Trap");
            Poison_Trap.Description.English("It's a poison trap!");
            Poison_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
            Poison_Trap.RequiredItems.Add("Ooze", 1, true);
            BuildPiece Spike_Trap = new("odinstrap", "Odins_Spike_Trap");
            Spike_Trap.Name.English("Odins Spike Trap");
            Spike_Trap.Description.English("The floor is spikes.");
            Spike_Trap.RequiredItems.Add("BlackMetal", 4, true);
            Spike_Trap.RequiredItems.Add("Stone", 8, true);
            BuildPiece Blade_Trap = new("odinstrap", "Odins_Blade_Trap");
            Blade_Trap.Name.English("Odins Blade Trap");
            Blade_Trap.Description.English("My biggest fan.");
            Blade_Trap.RequiredItems.Add("BlackMetal", 2, true);
            Blade_Trap.RequiredItems.Add("Wood", 6, true);
            BuildPiece MetalCage = new("odinstrap", "OdinsMetalCage");
            MetalCage.Name.English("OdinsMetalCage");
            MetalCage.Description.English("Dispite all my rage.");
            MetalCage.RequiredItems.Add("BlackMetal", 4, true);
            MetalCage.RequiredItems.Add("Iron", 4, true);
            BuildPiece CageCart = new("odinstrap", "OdinsCageCart");
            CageCart.Name.English("OdinsCageCart");
            CageCart.Description.English("Dispite all my rage.");
            CageCart.RequiredItems.Add("BlackMetal", 4, true);
            CageCart.RequiredItems.Add("Iron", 4, true);
            GameObject OdinsLureTrap_Projectile = ItemManager.PrefabManager.RegisterPrefab("odinstrap", "OdinsLureTrap_Projectile"); //register projectile
            GameObject OdinsLure_Projectile = ItemManager.PrefabManager.RegisterPrefab("odinstrap", "OdinsLure_Projectile"); //register projectile


        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public class DBPatch
        {
            public static void Prefix(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Count <= 0) return;
                __instance.m_StatusEffects.Add(SE_Trapped);
                
            }
        }
    }
}
