using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace RandomsGeneAssistant
{
    public class GeneSettings : ModSettings
    {
        public bool overrideSorting = true;
        public bool singlesFirst = true;
        public float backgroundSaturation = 0.5f;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref overrideSorting, "overrideSorting");
            Scribe_Values.Look(ref singlesFirst, "singlesFirst");
            Scribe_Values.Look(ref backgroundSaturation, "backgroundSaturation", 0.8f);
            base.ExposeData();
        }
    }

    public class SettingsRef : Mod
    {
        public static GeneSettings settings;
        public static bool overrideSorting => settings.overrideSorting;
        public static bool singlesFirst => settings.singlesFirst;
        public static float sat => settings.backgroundSaturation;

        public SettingsRef(ModContentPack content) : base(content)
        {
            settings = GetSettings<GeneSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Override xenogerm sorting", ref settings.overrideSorting, "Sorts the genes in the gene assembler menu by the most impactful gene in a set, rather than the first gene.");
            listingStandard.CheckboxLabeled("Sort single genes first", ref settings.singlesFirst, "Moves single genes to be first in the gene assembler.");

            listingStandard.Label($"Background Saturation: {(int) Math.Round(settings.backgroundSaturation * 100)}%");
            settings.backgroundSaturation = listingStandard.Slider(settings.backgroundSaturation, 0f, 1f);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Random's Gene Assistant";
        }
    }
}
