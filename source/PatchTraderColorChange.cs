using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [HarmonyPatch]
    public static class PatchTraderColorChange
    {
        [HarmonyPatch(typeof(RimWorld.TradeUI), nameof(RimWorld.TradeUI.DrawTradeableRow))]
        [HarmonyPrefix]
        public static void DrawTradeableRow_Prefix(Rect rect, Tradeable trad, int index)
        {
            if (trad.ThingDef != ThingDefOf.Genepack) { return; }
            Map map = Find.CurrentMap;
            if (map == null)
            {
                map = Find.AnyPlayerHomeMap;
            }
            if (map == null || !map.IsPlayerHome) { return; }

            //  Get the genepack
            Genepack genepack = (Genepack)trad.AnyThing;
            if (genepack == null) { return; }

            //  Get genebanks
            List<Thing> banks = new List<Thing>(map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.GenepackHolder)));
            if ((banks == null) || (banks.Count == 0)) { return; }

            //  Init tracking vars
            Dictionary<GeneDef, int> trackingStaus = new Dictionary<GeneDef, int>();
            foreach (GeneDef gd in genepack.GeneSet.GenesListForReading)
            {
                trackingStaus.Add(gd, 0);
                //  0 - Does not have
                //  1 - Has but in pack with other genes
                //  2 - Can replicate fully, don't buy
            }
            foreach (GeneDef gd in SettingsRef.ignoredGenes)
            {
                if (trackingStaus.ContainsKey(gd))
                {
                    trackingStaus[gd] = 2;
                }
            }
            int minVal = trackingStaus.MinBy(kvp => kvp.Value).Value;

            if (minVal < 2)
            {
                //  Loop through buildings
                foreach (Thing b in banks)
                {
                    //  Get the genepacks
                    CompGenepackContainer comp = b.TryGetComp<CompGenepackContainer>();
                    foreach (Genepack gp in comp.ContainedGenepacks)
                    {
                        //  Scan each gene
                        if (gp.GeneSet.GenesListForReading.TrueForAll(x => trackingStaus.ContainsKey(x)))
                        {
                            //  Scanned pack is a subset of trade pack
                            foreach (GeneDef g in gp.GeneSet.GenesListForReading)
                            {
                                trackingStaus[g] = 2;
                            }
                        }
                        else
                        {
                            //  Gene by gene comparison
                            foreach (GeneDef g in gp.GeneSet.GenesListForReading)
                            {
                                if ((trackingStaus.ContainsKey(g)) && (trackingStaus[g] == 0))
                                {
                                    trackingStaus[g] = 1;
                                }
                            }
                        }

                        minVal = trackingStaus.MinBy(kvp => kvp.Value).Value;
                        if (minVal == 2)
                        {
                            break;
                        }
                    }
                    if (minVal == 2)
                    {
                        break;
                    }
                }
            }

            //  Draw our own background
            Rect rect1 = new Rect(rect.width - 75f, 0.0f, 75f, rect.height);
            Color c = SettingsRef.red;
            if (minVal == 1)
            {
                c = SettingsRef.yellow;
            }
            else if (minVal == 2)
            {
                c = SettingsRef.green;
            }
            Widgets.DrawBoxSolid(rect, c);
            GUI.color = Color.white;
        }
    }
}
