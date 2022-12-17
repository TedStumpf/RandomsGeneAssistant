using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace RandomsGeneAssistant
{
    public static class PatchGeneripperUI
    {
        public static void HandlePatch(Harmony har)
        {
            if (LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "danielwedemeyer.generipper")) {
                har.Patch(TargetMethod(), new HarmonyMethod(typeof(PatchGeneripperUI).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
            }
        }

        public static MethodBase TargetMethod()
        {
            MethodInfo mi = AccessTools.FirstMethod(AccessTools.TypeByName("Dialog_SelectGene"), m => m.Name == "DrawGeneBasics");
            return mi;
        }


        [HarmonyPrefix]
        public static void Prefix(GeneDef gene, Rect geneRect, GeneType geneType, bool doBackground, bool overridden)
        {
            //  Early returns
            Map map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome) { return; }

            //  Get genebanks
            List<Thing> banks = new List<Thing>(map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.GenepackHolder)));
            if ((banks == null) || (banks.Count == 0)) { return; }

            //  Init tracking vars
            int findQuality = 0;
            if (SettingsRef.ignoredGenes.Contains(gene))
            {
                findQuality = -1;
            }
            else
            {
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
                            if (g == gene)
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
            }

            //  Draw our own background
            GUI.BeginGroup(geneRect);
            Rect rect1 = geneRect.AtZero();

            Color c = SettingsRef.red;
            if (findQuality == 1)
            {
                c = SettingsRef.yellow;
            }
            else if (findQuality == 2)
            {
                c = SettingsRef.green;
            }
            else if (findQuality == -1)
            {
                c = SettingsRef.gray;
            }
            Widgets.DrawBoxSolid(rect1, c);
            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            Widgets.DrawBox(rect1);
            GUI.color = Color.white;
            
            GUI.EndGroup();

            //  Override the previous background
            doBackground = false;
        }
    }
}
