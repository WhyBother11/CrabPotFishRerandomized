using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Extensions;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace CrabPotFishRerandomized;

public static class CrabPotPatch
{
    #nullable disable
    private static IMonitor _monitor;
    #nullable restore

    public static void Register(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        _monitor.Log($"Applying transpiler patch {nameof(CrabPot_DayUpdate_Transpiler)}");
        harmony.Patch(
            original: AccessTools.Method(typeof(CrabPot), nameof(CrabPot.DayUpdate)),
            transpiler: new HarmonyMethod(typeof(CrabPotPatch), nameof(CrabPot_DayUpdate_Transpiler))
        );
    }

    private static IEnumerable<CodeInstruction> CrabPot_DayUpdate_Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        try
        {
            var matcher = new CodeMatcher(instructions);
            var localItemList = generator.DeclareLocal(typeof(List<Object>));

            matcher.Start() 
            // insert localItemList = CrabPotPatch.GetInitialLocalFishList();
                .Insert(
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(CrabPotPatch), nameof(GetInitialLocalFishList))),
                    new CodeInstruction(OpCodes.Stloc_S, localItemList)
                );
            // match if (!(r.NextDouble() < chanceForCatch)) { continue; }
            // just to make sure matcher is looking somewhere close to targeted code
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Random), nameof(Random.NextDouble))),
                new CodeMatch(instr => instr.operand is LocalBuilder lb && lb.LocalIndex == 19),
                new CodeMatch(OpCodes.Bge_Un_S)
            ).ThrowIfNotMatch("Failed to locate check for whether fish was caught or not");
            // match this.heldObject = ...
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld)
            ).ThrowIfNotMatch("Failed to locate loading 'heldObject' field for future assignment");
            // no need to set heldObject yet
            matcher.RemoveInstructions(2);
            // match heldObject setter
            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Callvirt))
                .ThrowIfNotMatch("Failed to locate 'heldObject' callvirt to set value");
            // remove heldObject setter, again no need
            matcher.RemoveInstruction();
            // insert CrabPotPatch.AddFishItemToList(
            // ItemRegistry.Create<Object>("(O)" + v.Key, quantity, quality),
            // localItemList);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, localItemList),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CrabPotPatch), nameof(AddFishItemToList)))
            );
            // remove break instruction
            matcher.RemoveInstruction()
            // match if (heldObject.Value == null) 
                .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Brtrue_S)
            ).ThrowIfNotMatch("Failed to locate check for whether 'heldObject' field if null after fish catching logic or not");
            var labels = matcher.Instruction.ExtractLabels();
            // insert this.heldObject = CrabPotPatch.ChooseHeldObject(this, localItemList, r);
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldloc_S, localItemList),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CrabPotPatch), nameof(ChooseHeldObject)))
            );
            return matcher.Instructions();
        }
        catch (Exception e)
        {
            _monitor.Log($"Failed to modify crab pot randomization, error:\n{e}", LogLevel.Error);
            return instructions;
        }
    }

    private static List<Object> GetInitialLocalFishList()
    {
        return new List<Object>();
    }

    private static void AddFishItemToList(Object fish, List<Object> fishItems)
    {
        fishItems.Add(fish);
    }

    private static void ChooseHeldObject(Object crabPot, List<Object> fishItems, Random random)
    {
        if (!fishItems.Any()) return;
        var chosen = random.ChooseFrom(fishItems);
        var fishNames = fishItems.Select(fish => fish.Name).ToList();
        _monitor.Log($"Randomized fish: [{string.Join(", ", fishNames)}], chosen: {chosen.Name}");
        crabPot.heldObject.Value = chosen;
    }
}
