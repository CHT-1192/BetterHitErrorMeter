using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace BetterHitErrorMeter
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Harmony = new Harmony(modEntry.Info.Id);
            modEntry.OnToggle = OnToggle;

            Mod.Logger.Log("[BHEM] Loading, patching...");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Mod.Logger.Log("[BHEM] Patches applied");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Harmony?.PatchAll(Assembly.GetExecutingAssembly());
                Mod?.Logger.Log("[BHEM] Enabled");
            }
            else
            {
                Patches.RestoreAll();
                Harmony?.UnpatchAll(modEntry.Info.Id);
                Mod?.Logger.Log("[BHEM] Disabled, sprites restored");
            }
            return true;
        }
    }
}
