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
            PatchTradeUIRevisited.HandlePatch(harmony);
            PatchGeneripperUI.HandlePatch(harmony);
        }
    }
}
