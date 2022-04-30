using System;
using System.Linq;
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
		private const string ModVersion = "1.0.6";
		private const string ModGUID = "com.odinplus.odinstraps";
		private static Harmony harmony = null!;

		private static Item UnplacedMetalTrap = null!;
		private static GameObject OdinsLure_Projectile = null!;

		ConfigSync configSync = new(ModGUID)
		{ DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
		internal static ConfigEntry<bool> ServerConfigLocked = null!;
		internal static ConfigEntry<int> trappedDuration = null!;
		internal static ConfigEntry<int> trappedEffectStrength = null!;
		internal static ConfigEntry<int> trapProjectileDuration = null!;
		internal static ConfigEntry<int> trapProjectileEffectStrength = null!;

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
			trappedDuration = config("1 - General", "Trapped duration", 5, new ConfigDescription("Sets the duration for the trapped effect.", new AcceptableValueRange<int>(1, 15)));
			trappedDuration.SettingChanged += (_, _) => AddStatusEffect.SetTrappedValues();
			trappedEffectStrength = config("1 - General", "Trapped effect strength", 100, new ConfigDescription("Sets the strength for the trapped effect.", new AcceptableValueRange<int>(1, 100)));
			trappedEffectStrength.SettingChanged += (_, _) => AddStatusEffect.SetTrappedValues();
			trapProjectileDuration = config("1 - General", "Trap projectile duration", 5, new ConfigDescription("Sets the duration for the trap projectile effect.", new AcceptableValueRange<int>(1, 15)));
			trapProjectileDuration.SettingChanged += (_, _) => AddStatusEffect.SetHitValues();
			trapProjectileEffectStrength = config("1 - General", "Trap projectile effect strength", 100, new ConfigDescription("Sets the strength for the trap projectile effect.", new AcceptableValueRange<int>(1, 100)));
			trapProjectileEffectStrength.SettingChanged += (_, _) => AddStatusEffect.SetHitValues();

			UnplacedMetalTrap = new Item("odinstrap", "Unplaced_Metal_Trap"); //assetbundle name, Asset Name
			UnplacedMetalTrap.Crafting.Add(CraftingTable.Forge, 2);
			UnplacedMetalTrap.RequiredItems.Add("Iron", 6);
			UnplacedMetalTrap.RequiredItems.Add("BlackMetal", 1);
			UnplacedMetalTrap.CraftAmount = 3;

			Item LureBlade = new("odinstrap", "OdinsLureBlade"); //assetbundle name, Asset Name
			LureBlade.Crafting.Add(CraftingTable.Forge, 2);
			LureBlade.RequiredItems.Add("Wood", 6);
			LureBlade.RequiredItems.Add("Iron", 2);
			LureBlade.CraftAmount = 1;

			Item LureCannon = new("odinstrap", "OdinsLureCannon"); //assetbundle name, Asset Name
			LureCannon.Crafting.Add(CraftingTable.Forge, 2);
			LureCannon.RequiredItems.Add("Wood", 6);
			LureCannon.RequiredItems.Add("Iron", 2);
			LureCannon.CraftAmount = 1;

			BuildPiece Metal_Trap = new("odinstrap", "Odins_Metal_Trap");
			Metal_Trap.Name.English("Odins Metal Trap");
			Metal_Trap.Description.English("It's a trap!");
			Metal_Trap.RequiredItems.Add("Unplaced_Metal_Trap", 1, true);
			Metal_Trap.Prefab.transform.Find("HatchProxy/Traps/HIT AREA").gameObject.AddComponent<TrapTriggered>();

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

			BuildPiece Flame_Trap = new("odinstrap", "Odins_Flame_Trap");
			Flame_Trap.Name.English("Odins Flame Trap");
			Flame_Trap.Description.English("The floor is fire.");
			Flame_Trap.RequiredItems.Add("BlackMetal", 4, true);
			Flame_Trap.RequiredItems.Add("Wood", 6, true);

			BuildPiece MetalCage = new("odinstrap", "OdinsMetalCage");
			MetalCage.Name.English("OdinsMetalCage");
			MetalCage.Description.English("Dispite all my rage.");
			MetalCage.RequiredItems.Add("BlackMetal", 4, true);
			MetalCage.RequiredItems.Add("Iron", 4, true);

			BuildPiece Nest_Trap = new("odinstrap", "Odins_Nest_Trap");
			Nest_Trap.Name.English("Odins_Nest_Trap");
			Nest_Trap.Description.English("A cage for your ChickenBoo");
			Nest_Trap.RequiredItems.Add("DeerHide", 4, true);
			Nest_Trap.RequiredItems.Add("Wood", 4, true);

			BuildPiece CageCart = new("odinstrap", "OdinsCageCart");
			CageCart.Name.English("OdinsCageCart");
			CageCart.Description.English("Dispite all my rage.");
			CageCart.RequiredItems.Add("BlackMetal", 4, true);
			CageCart.RequiredItems.Add("Iron", 4, true);


			GameObject OdinsLureTrap_Projectile = ItemManager.PrefabManager.RegisterPrefab("odinstrap", "OdinsLureTrap_Projectile");

			OdinsLure_Projectile = ItemManager.PrefabManager.RegisterPrefab("odinstrap", "OdinsLure_Projectile"); //register projectile
		}

		[HarmonyPatch(typeof(Character), nameof(Character.Awake))]
		public class AddRPC
		{
			private static void Postfix(Character __instance)
			{
				__instance.m_nview.Register("OdinsTrap ProjectileHit", _ => __instance.GetSEMan().AddStatusEffect("Trap projectile hit"));
			}
		}

		[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
		public class AddStatusEffect
		{
			private static DecreaseMovementSpeed? trapped;
			private static DecreaseMovementSpeed? hit;

			private static void Postfix(ObjectDB __instance)
			{
				trapped = ScriptableObject.CreateInstance<DecreaseMovementSpeed>();
				trapped.name = "Trapped";
				trapped.m_name = "Trapped";
				trapped.m_icon = UnplacedMetalTrap.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons.First();
				SetTrappedValues();
				__instance.m_StatusEffects.Add(trapped);

				hit = ScriptableObject.CreateInstance<DecreaseMovementSpeed>();
				hit.isProjectile = true;
				hit.name = "Trap projectile hit";
				hit.m_name = "Trap projectile hit";
				hit.m_icon = UnplacedMetalTrap.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons.First();
				SetHitValues();
				__instance.m_StatusEffects.Add(hit);
			}

			public static void SetTrappedValues()
			{
				if (trapped is not null)
				{
					trapped.m_tooltip = $"You stepped into a trap. Your movement speed is reduced by {trappedEffectStrength.Value}%.";
					trapped.m_ttl = trappedDuration.Value;
				}
			}

			public static void SetHitValues()
			{
				if (hit is not null)
				{
					hit.m_tooltip = $"You got hit by a trap projectile. Your movement speed is reduced by {trapProjectileEffectStrength.Value}%.";
					hit.m_ttl = trapProjectileDuration.Value;
				}
			}
		}

		public class DecreaseMovementSpeed : StatusEffect
		{
			public bool isProjectile = false;
			public override void ModifySpeed(float baseSpeed, ref float speed)
			{
				speed *= 1 - (isProjectile ? trapProjectileEffectStrength.Value : trappedEffectStrength.Value) / 100f;
			}
		}

		[HarmonyPatch(typeof(Projectile), nameof(Projectile.IsValidTarget))]
		public class CheckProjectileHit
		{
			private static void Postfix(Projectile __instance, IDestructible destr)
			{
				if (__instance.name.StartsWith(OdinsLure_Projectile.gameObject.name, StringComparison.Ordinal) && ((MonoBehaviour)destr).GetComponent<Character>() is { } character && character != __instance.m_owner)
				{
					character.m_nview.InvokeRPC("OdinsTrap ProjectileHit");
				}
			}
		}

		[HarmonyPatch(typeof(Character), nameof(Character.Jump))]
		public class PreventJumping
		{
			private static bool Prefix(Character __instance)
			{
				return __instance is not Player player || (!player.GetSEMan().HaveStatusEffect("Trapped") && !player.GetSEMan().HaveStatusEffect("Trap projectile hit"));
			}
		}
	}
}
