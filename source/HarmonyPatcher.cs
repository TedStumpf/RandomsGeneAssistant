using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatcher
    {
        static HarmonyPatcher()
        {
            Harmony harmony = new Harmony("rimworld.randomcoughdrop.geneassistant");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class GeneBackgroundColor
    {
        [HarmonyPatch(typeof(GeneUIUtility), nameof(GeneUIUtility.DrawGene))]
        [HarmonyPrefix]
        public static void DrawGeneBasics_Prefix(Gene gene, Rect geneRect, GeneType geneType, ref bool doBackground, bool clickable = true)
        {
            //  Early returns
            if (!doBackground) { return; }
            Map map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome) { return; }

            //  Get genebanks
            
            List<Thing> banks = new List<Thing>(map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.GenepackHolder)));
            if ((banks == null) || (banks.Count == 0)) { return; }

            //  Init tracking vars
            int findQuality = 0;

            //  Loop through buildings
            foreach (Thing b in banks)
            {
                //  Get the genepacks
                CompGenepackContainer comp = b.TryGetComp<CompGenepackContainer>();
                foreach (Genepack gp in comp.ContainedGenepacks)
                {
                    //  Scan each gene
                    foreach (GeneDef g in gp.GeneSet.GenesListForReading)
                    {
                        if (g == gene.def)
                        {
                            findQuality = gp.GeneSet.GenesListForReading.Count == 1 ? 2 : 1;
                        }

                        if (findQuality > 0)
                        {
                            break;
                        }
                    }
                    if (findQuality == 2)
                    {
                        break;
                    }
                }
                if (findQuality == 2)
                {
                    break;
                }
            }

            //  Draw our own background
            GUI.BeginGroup(geneRect);
            Rect rect1 = geneRect.AtZero();
            if (doBackground)
            {
                Color c = new Color(SettingsRef.sat, 0f, 0f, 0.4f);
                if (findQuality == 1)
                {
                    c = new Color(SettingsRef.sat, SettingsRef.sat, 0f, 0.4f);
                }
                else if (findQuality == 2)
                {
                    c = new Color(0f, SettingsRef.sat, 0f, 0.4f);
                }
                Widgets.DrawBoxSolid(rect1, c);
                GUI.color = new Color(1f, 1f, 1f, 0.05f);
                Widgets.DrawBox(rect1);
                GUI.color = Color.white;
            }
            GUI.EndGroup();

            //  Override the previous background
            doBackground = false;
        }
    }


    [HarmonyPatch]
    public static class GeneAssemblerEject
    {
        //  Taken from the Genebank
        private static readonly CachedTexture EjectTex = new CachedTexture("UI/Gizmos/EjectAll");

        [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetGizmos))]
        [HarmonyPostfix]
        public static void GetGizmos_Postfix(Building_GeneAssembler __instance, ref IEnumerable<Gizmo> __result)
        {
            Command_Action commandActionEject = new Command_Action();
            commandActionEject.defaultLabel = "Eject Duplicates";
            commandActionEject.defaultDesc = "Ejects all duplicate genepacks for ease of selling.";
            commandActionEject.icon = (Texture)EjectTex.Texture;
            commandActionEject.action = new Action(() => EjectDuplicateGenepacks(__instance));

            List<Gizmo> gizmoList = new List<Gizmo>(__result);
            gizmoList.Add(commandActionEject);
            __result = gizmoList;
        }

        public static void EjectDuplicateGenepacks(Building_GeneAssembler source)
        {
            //  Lists and a set for matching
            //  This script does not cover the case where 1 + 2 = 3
            //  Or to say when a genepack with one gene and a genepack with two genes matches on with three
            List<Genepack> allPacks = source.GetGenepacks(true, true);
            List<Genepack> markedForEjection = new List<Genepack>();
            HashSet<GeneDef> soloDefs = new HashSet<GeneDef>();

            //  Build list of solo genes
            foreach (Genepack gp in allPacks)
            {
                if (gp.GeneSet.GenesListForReading.Count == 1)
                {
                    GeneDef def = gp.GeneSet.GenesListForReading[0];
                    if (soloDefs.Contains(def))
                    {
                        markedForEjection.Add(gp);
                    }
                    else
                    {
                        soloDefs.Add(def);
                    }
                }
            }

            //  Scan for redundant sets
            foreach (Genepack gp in allPacks)
            {
                if (gp.GeneSet.GenesListForReading.Count > 1)
                {
                    bool hasUnique = false;
                    foreach (GeneDef def in gp.GeneSet.GenesListForReading)
                    {
                        if (!soloDefs.Contains(def))
                        {
                            hasUnique = true;
                            break;
                        }
                    }

                    if (!hasUnique)
                    {
                        markedForEjection.Add(gp);
                    }
                }
            }

            //  Eject genepacks
            foreach (Genepack gp in markedForEjection)
            {
                CompGenepackContainer holder = source.GetGeneBankHoldingPack(gp);
                Map destMap = holder.parent.Map;
                holder.innerContainer.TryDrop(gp, ThingPlaceMode.Near, out Thing thingout);
            }

            //  Alert user
            if (markedForEjection.Count == 0)
            {
                Messages.Message("No duplicate genepacks.", MessageTypeDefOf.NegativeEvent, false);
            }
            else
            {
                Messages.Message("Ejected " + markedForEjection.Count + " genepacks.", MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }

    [HarmonyPatch]
    public static class GeneAssemblerFilledSlots
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

    [HarmonyPatch]
    public static class SortGenepacksOverride
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

    [HarmonyPatch]
    public static class TraderColorChange
    {
        [HarmonyPatch(typeof(TradeUI), nameof(TradeUI.DrawTradeableRow))]
        [HarmonyPrefix]
        public static void DrawTradeableRow_Prefix(Rect rect, Tradeable trad, int index)
        {
            if (trad.ThingDef != ThingDefOf.Genepack) { return; }
            Map map = Find.CurrentMap;
            if (map  == null)
            {
                map = Find.AnyPlayerHomeMap;
            }
            if (map == null || !map.IsPlayerHome) { return; }

            //  Get the genepack
            Genepack genepack = (Genepack) trad.AnyThing;
            if (genepack == null) { return; }

            //  Get genebanks
            List<Thing> banks = new List<Thing>(map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.GenepackHolder)));
            if ((banks == null) || (banks.Count == 0)) { return; }

            //  Init tracking vars
            Dictionary<GeneDef, int> trackingStaus = new Dictionary<GeneDef, int>();
            foreach(GeneDef gd in genepack.GeneSet.GenesListForReading)
            {
                trackingStaus.Add(gd, 0);
                //  0 - Does not have
                //  1 - Has but in pack with other genes
                //  2 - Can replicate fully, don't buy
            }
            int minVal = 0;

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

            //  Draw our own background
            Rect rect1 = new Rect(rect.width - 75f, 0.0f, 75f, rect.height);
            Color c = new Color(SettingsRef.sat, 0f, 0f, 0.3f);
            if (minVal == 1)
            {
                c = new Color(SettingsRef.sat, SettingsRef.sat, 0f, 0.3f);
            }
            else if (minVal == 2)
            {
                c = new Color(0f, SettingsRef.sat, 0f, 0.3f);
            }
            Widgets.DrawBoxSolid(rect, c);
            GUI.color = Color.white;
        }
    }
}
