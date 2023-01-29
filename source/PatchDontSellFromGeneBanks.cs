using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RandomsGeneAssistant
{
    class Tools
    {
        private static readonly MethodInfo m_getParentContainer = AccessTools.PropertyGetter(typeof(Genepack), "ParentContainer");
        private static readonly FastInvokeHandler getParentContainer = MethodInvoker.GetHandler(m_getParentContainer);

        public static IEnumerable<Thing> FilterGeneBanks(IEnumerable<Thing> source)
        {
            foreach (var thing in source)
            {

                if (SettingsRef.sellFromGeneBanks || (!(thing is Genepack genepack)))
                    yield return thing;

                else if (getParentContainer(genepack) == null)

                    yield return thing;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_TraderTracker))]
    [HarmonyPatch(nameof(Pawn_TraderTracker.ColonyThingsWillingToBuy))]
    public class Pawn_TraderTrackerPatches
    {

        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result)
        {
            return Tools.FilterGeneBanks(__result);
        }
    }

    [HarmonyPatch(typeof(TradeUtility))]
    [HarmonyPatch(nameof(TradeUtility.AllLaunchableThingsForTrade))]
    public class TradeUtilityPatches
    {

        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result)
        {
            return Tools.FilterGeneBanks(__result);
        }
    }
}
