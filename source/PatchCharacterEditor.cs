using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace RandomsGeneAssistant
{
    public static class PatchCharacterEditor
    {
        public static void HandlePatch(Harmony har)
        {
            if (LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "void.charactereditor"))
            {
                har.Patch(TargetMethod(), new HarmonyMethod(typeof(PatchCharacterEditor).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public)));
            }
        }

        public static MethodBase TargetMethod()
        {
            MethodInfo mi = AccessTools.FirstMethod(AccessTools.TypeByName("RaceTool"), m => m.Name == "CanHaveChangedBodySize");
            return mi;
        }


        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, Pawn p)
        {
            __result = p != null && p.RaceProps.Humanlike && (p.DevelopmentalStage > DevelopmentalStage.Child);
            return false;
        }
    }
}
