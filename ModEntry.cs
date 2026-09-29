using HarmonyLib;
using StardewModdingAPI;

namespace CrabPotFishRerandomized;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var harmony = new Harmony(ModManifest.UniqueID);
        CrabPotPatch.Register(harmony, Monitor);
    }
}