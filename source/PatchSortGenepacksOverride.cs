using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [HarmonyPatch]
    public static class PatchSortGenepacksOverride
    {
        [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.SortGenepacks))]
        [HarmonyPrefix]
        public static bool SortGenepacks_Prefix(List<Genepack> genepacks)
        {
            if (SettingsRef.overrideSorting)
            {
                List<GeneDef> order = GeneUtility.GenesInOrder;
                if (SettingsRef.singlesFirst)
                {
                    genepacks.SortBy((x => x.GeneSet.GenesListForReading.Count == 1 ? 0 : 1), (x => order.IndexOf(OrderAndGetFirst(order, x.GeneSet.GenesListForReading))));
                }
                else
                {
                    genepacks.SortBy(x => order.IndexOf(OrderAndGetFirst(order, x.GeneSet.GenesListForReading)));
                }

                return false;
            }
            //  Use default sorting
            return true;
        }

        private static GeneDef OrderAndGetFirst(List<GeneDef> order, List<GeneDef> set)
        {
            List<GeneDef> localSet = new List<GeneDef>(set);
            localSet.SortBy(x => -x.biostatCpx, x => -Mathf.Abs(x.biostatMet), x => order.IndexOf(x));
            return localSet[0];
        }
    }
}
