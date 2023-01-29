using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;

namespace RandomsGeneAssistant
{
    public class GeneSettings : ModSettings
    {
        public bool overrideSorting = true;
        public bool singlesFirst = true;
        public float backgroundSaturation = 0.5f;
        public bool overrideColors = false;
        public bool sellFromGeneBanks = false;
        public Color testColor;
        public HashSet<string> ignoredGenes;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref overrideSorting, "overrideSorting", true);
            Scribe_Values.Look(ref singlesFirst, "singlesFirst", true);
            Scribe_Values.Look(ref backgroundSaturation, "backgroundSaturation", 0.8f);
            Scribe_Values.Look(ref overrideColors, "overrideColors", false);
            Scribe_Values.Look(ref sellFromGeneBanks, "sellFromGeneBanks", false);
            Scribe_Collections.Look(ref ignoredGenes, "ignoredGenes");
            base.ExposeData();
        }
    }

    public class SettingsRef : Mod
    {
        public static GeneSettings settings;
        public static bool overrideSorting => settings.overrideSorting;
        public static bool singlesFirst => settings.singlesFirst;
        public static float sat => settings.backgroundSaturation;
        public static bool sellFromGeneBanks => settings.sellFromGeneBanks;
        public static Color green => settings.overrideColors ? new Color(0f, 0f, sat, 0.6f) : new Color(0f, sat, 0f, 0.4f);
        public static Color yellow => settings.overrideColors ? new Color(sat * 0.4f, sat * 0.4f, sat * 0.4f, 0.6f) : new Color(sat, sat, 0f, 0.4f);
        public static Color red => settings.overrideColors ? new Color(0f, sat, 0f, 0.6f) : new Color(sat, 0f, 0f, 0.4f);
        public static Color gray => new Color(0.2f, 0.2f, 0.2f, 0.6f);

        public static HashSet<GeneDef> ignoredGenes { get => GetGeneIgnore(); } //  Non-null guaranteed
        private static HashSet<GeneDef> ignoredGenes_;  //  Actual data

        public SettingsRef(ModContentPack content) : base(content)
        {
            settings = GetSettings<GeneSettings>();
            if (settings.ignoredGenes == null)
            {
                settings.ignoredGenes = new HashSet<string>();
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Override xenogerm sorting", ref settings.overrideSorting, "Sorts the genes in the gene assembler menu by the most impactful gene in a set, rather than the first gene.");
            listingStandard.CheckboxLabeled("Sort single genes first", ref settings.singlesFirst, "Sorts single genes to be first in the gene assembler.");

            listingStandard.Label($"Background Saturation: {(int) Math.Round(settings.backgroundSaturation * 100)}%");
            settings.backgroundSaturation = listingStandard.Slider(settings.backgroundSaturation, 0f, 1f);

            listingStandard.CheckboxLabeled("Alternate colors", ref settings.overrideColors, "Activates an alternate color scheme.");
            listingStandard.CheckboxLabeled("Sell from genebanks", ref settings.sellFromGeneBanks, "Allows selling genepacks directly from genebanks.");

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Random's Gene Assistant";
        }

        public static bool ToggleGeneIgnore(GeneDef gene)
        {
            if (ignoredGenes_.Contains(gene))
            {
                ignoredGenes_.Remove(gene);
                return false;
            }
            else
            {
                ignoredGenes_.Add(gene);
                return true;
            }
        }

        public static void CommitGeneIgnore()
        {
            settings.ignoredGenes.Clear();
            foreach (GeneDef gene in ignoredGenes_)
            {
                settings.ignoredGenes.Add(gene.defName);
            }
            settings.Write();
        }

        public static void RevertGeneIgnore()
        {
            ignoredGenes_.Clear();
            foreach (GeneDef gd in GeneUtility.GenesInOrder)
            {
                if (settings.ignoredGenes.Contains(gd.defName))
                {
                    ignoredGenes_.Add(gd);
                }
            }
        }

        public static void ResetGeneIgnore()
        {
            ignoredGenes_.Clear();
        }

        public static void AddToGeneIgnore(HashSet<GeneDef> genes)
        {
            ignoredGenes_.AddRange(genes);
        }

        public static void RemoveFromGeneIgnore(HashSet<GeneDef> genes)
        {
            ignoredGenes_.RemoveWhere(x => genes.Contains(x));
        }


        public static HashSet<GeneDef> GetGeneIgnore()
        {
            if (ignoredGenes_ == null)
            {
                ignoredGenes_ = new HashSet<GeneDef>();
                foreach (GeneDef gene in GeneUtility.GenesInOrder)
                {
                    if (settings.ignoredGenes.Contains(gene.defName))
                    {
                        ignoredGenes_.Add(gene);
                    }
                }
            }
            return ignoredGenes_;
        }
    }
}
