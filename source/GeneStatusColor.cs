using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [StaticConstructorOnStartup]
    public static class GeneStatusColor
    {
        static GeneStatusColor()
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
            List<Building> banks = new List<Building>(map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.GeneBank));
            if ((banks == null) || (banks.Count == 0)) { return; }

            //  Init tracking vars
            int findQuality = 0;

            //  Loop through buildings
            foreach (Building b in banks)
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
                Color c = new Color(0.8f, 0f, 0f, 0.4f);
                if (findQuality == 1)
                {
                    c = new Color(0.8f, 0.8f, 0f, 0.4f);
                }
                else if (findQuality == 2)
                {
                    c = new Color(0f, 0.8f, 0f, 0.4f);
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
}
