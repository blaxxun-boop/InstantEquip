using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace InstantEquip;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class InstantEquip : BaseUnityPlugin
{
	private const string ModName = "InstantEquip";
	private const string ModVersion = "1.0.0";
	private const string ModGUID = "org.bepinex.plugins.instantequip";

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	private static ConfigEntry<Toggle> instantEquipWeapons = null!;
	private static ConfigEntry<Toggle> instantEquipArmor = null!;

	private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = "1.0.0" };

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

		SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
		syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

	private enum Toggle
	{
		On = 1,
		Off = 0
	}

	public void Awake()
	{
		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		instantEquipWeapons = config("1 - General", "Instant Equip Weapons", Toggle.On, "If on, weapons will be equipped instantly.");
		instantEquipArmor = config("1 - General", "Instant Equip Armor", Toggle.On, "If on, armor will be equipped instantly.");

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);
	}

	[HarmonyPatch(typeof(Player), nameof(Player.QueueEquipItem))]
	private class RemoveEquipDuration
	{
		private static bool Prefix(Player __instance, ItemDrop.ItemData item)
		{
			if (item.IsWeapon() && instantEquipWeapons.Value == Toggle.On)
			{
				__instance.EquipItem(item);
				return false;
			}
			if (item.IsEquipable() && !item.IsWeapon() && instantEquipArmor.Value == Toggle.On)
			{
				__instance.EquipItem(item);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.QueueUnequipItem))]
	private class RemoveUnequipDuration
	{
		private static bool Prefix(Player __instance, ItemDrop.ItemData item)
		{
			if (item.IsWeapon() && instantEquipWeapons.Value == Toggle.On)
			{
				__instance.UnequipItem(item);
				return false;
			}
			if (item.IsEquipable() && !item.IsWeapon() && instantEquipArmor.Value == Toggle.On)
			{
				__instance.UnequipItem(item);
				return false;
			}
			return true;
		}
	}
}
