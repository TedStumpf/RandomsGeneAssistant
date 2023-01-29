using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{



    [HarmonyPatch]
    public static class PatchGeneAssemblerEject
    {
        //  Taken from the Genebank
        private static readonly CachedTexture EjectTex = new CachedTexture("UI/Gizmos/EjectAll");

        [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetGizmos))]
        [HarmonyPostfix]
        public static void GetGizmos_Postfix(Building_GeneAssembler __instance, ref IEnumerable<Gizmo> __result)
        {
            Command_Action commandActionEject = new Command_Action();
            commandActionEject.defaultLabel = "Eject Genepacks";
            commandActionEject.defaultDesc = "Ejects all duplicate genepacks for ease of selling. Contains an option to eject all cosmetic genepacks.";
            commandActionEject.icon = (Texture)EjectTex.Texture;
            commandActionEject.action = new Action(() => EjectionSelection(__instance));

            List<Gizmo> gizmoList = new List<Gizmo>(__result);
            gizmoList.Add(commandActionEject);
            __result = gizmoList;
        }

        public static void EjectionSelection(Building_GeneAssembler source)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("Eject Duplicates", () => EjectDuplicateGenepacks(source)));
            options.Add(new FloatMenuOption("Eject Cosmetic", () => EjectCosmeticGenepacks(source)));
            options.Add(new FloatMenuOption("Eject Combined Genepacks", () => EjectCombinedGenepacks(source)));
            Find.WindowStack.Add((Window)new FloatMenu(options));
        }

        public static void EjectDuplicateGenepacks(Building_GeneAssembler source)
        {
            //  Lists and a set for matching
            //  This script does not cover the case where 1 + 2 = 3
            //  Or to say when a genepack with one gene and a genepack with two genes matches on with three
            List<Genepack> allPacks = source.GetGenepacks(true, true);
            List<Genepack> markedForEjection = new List<Genepack>();
            HashSet<GeneDef> soloDefs = new HashSet<GeneDef>();
            soloDefs.AddRange(SettingsRef.ignoredGenes);

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

        public static void EjectCosmeticGenepacks(Building_GeneAssembler source)
        {
            //  Ejects genepacks that contain all 0 complexity genes
            List<Genepack> allPacks = source.GetGenepacks(true, true);
            List<Genepack> markedForEjection = new List<Genepack>();

            //  Scan for redundant sets
            foreach (Genepack gp in allPacks)
            {
                bool cosOnly = true;
                foreach (GeneDef def in gp.GeneSet.GenesListForReading)
                {
                    if ((def.biostatCpx != 0) || (def.biostatMet != 0))
                    {
                        cosOnly = false;
                        break;
                    }
                }

                if (cosOnly)
                {
                    markedForEjection.Add(gp);
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
                Messages.Message("No cosmetic genepacks.", MessageTypeDefOf.NegativeEvent, false);
            }
            else
            {
                Messages.Message("Ejected " + markedForEjection.Count + " genepacks.", MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public static void EjectCombinedGenepacks(Building_GeneAssembler source)
        {
            //  Ejects genepacks that contain all 0 complexity genes
            List<Genepack> allPacks = source.GetGenepacks(true, true);
            List<Genepack> markedForEjection = new List<Genepack>();

            //  Scan for redundant sets
            foreach (Genepack gp in allPacks)
            {
                if (gp.GeneSet.GenesListForReading.Count > 1)
                {
                    markedForEjection.Add(gp);
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
                Messages.Message("No combined genepacks.", MessageTypeDefOf.NegativeEvent, false);
            }
            else
            {
                Messages.Message("Ejected " + markedForEjection.Count + " genepacks.", MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}
