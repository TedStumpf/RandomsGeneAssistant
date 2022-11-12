using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [HarmonyPatch]
    public static class PatchGeneAssemblerFilledSlots
    {
        //  Taken from the Gene Assembler
        [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetInspectString))]
        [HarmonyPostfix]
        public static void GetInspectString_Postfix(Building_GeneAssembler __instance, ref string __result)
        {
            int filled = 0;
            int total = 0;

            //  
            List<Thing> connectedFacilities = __instance.ConnectedFacilities;
            if (connectedFacilities != null)
            {
                foreach (Thing thing in connectedFacilities)
                {
                    CompGenepackContainer comp = thing.TryGetComp<CompGenepackContainer>();
                    if (comp != null)
                    {
                        filled += comp.ContainedGenepacks.Count;
                        total += comp.Props.maxCapacity;
                    }
                }
            }

            __result += "\nFilled Slots: " + filled + "/" + total;
        }
    }
}
