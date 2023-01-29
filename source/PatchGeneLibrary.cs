using System;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RandomsGeneAssistant
{
    [HarmonyPatch]
    public static class PatchGeneLibrary
    {
        public static bool lastCloseWasButton = false;
        public static int hideState = 0;

        private static readonly CachedTexture ViewTex = new CachedTexture("UI/Gizmos/ViewGenes");

        [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetGizmos))]
        [HarmonyPostfix]
        public static void GetGizmos_Postfix(Building_GeneAssembler __instance, ref IEnumerable<Gizmo> __result)
        {
            Command_Action commandActionEject = new Command_Action();
            commandActionEject.defaultLabel = "View Library";
            commandActionEject.defaultDesc = "Open the gene library.";
            commandActionEject.icon = (Texture)ViewTex.Texture;
            commandActionEject.action = new Action(() => Find.WindowStack.Add(new Dialog_GeneLibrary(__instance)));

            List<Gizmo> gizmoList = new List<Gizmo>(__result);
            gizmoList.Add(commandActionEject);
            __result = gizmoList;
        }
    }

    public class Dialog_GeneLibrary : Window
    {
        private static readonly List<GeneDef> geneDefs = new List<GeneDef>();
        private static readonly Dictionary<GeneDef, int> geneStatus = new Dictionary<GeneDef, int>();
        private static float xenogenesHeight;
        private static float endogenesHeight;
        private static float scrollHeight;
        private static readonly CachedTexture GeneBackground_Archite = new CachedTexture("UI/Icons/Genes/GeneBackground_ArchiteGene");
        private static readonly CachedTexture GeneBackground_Xenogene = new CachedTexture("UI/Icons/Genes/GeneBackground_Xenogene");
        private Vector2 scrollPosition;
        private Building_GeneAssembler parent;

        public Dialog_GeneLibrary(Building_GeneAssembler _parent)
        {
            parent = _parent;
        }


        public override Vector2 InitialSize => new Vector2(736f, 700f);

        public override void PostOpen()
        {
            if (!ModLister.CheckBiotech("gene library"))
                Close(false);
            else
                PatchGeneLibrary.lastCloseWasButton = false;
                base.PostOpen();
        }


        public override void PreClose()
        {
            if (!PatchGeneLibrary.lastCloseWasButton)
            {
                SettingsRef.RevertGeneIgnore();
            }
            base.PreClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= CloseButSize.y;
            var rect = inRect;
            rect.xMin += 34f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "Gene Library");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            inRect.yMin += 34f;
            var zero = Vector2.zero;
            DrawGenesInfo(inRect, InitialSize.y, ref zero, ref scrollPosition);
            float sep = (inRect.width - (CloseButSize.x) * 5) / 4f;
            //  Cancel
            if (Widgets.ButtonText(
                    new Rect(inRect.xMin, inRect.yMax, CloseButSize.x,
                        CloseButSize.y), "Cancel"))
            {
                SettingsRef.RevertGeneIgnore();
                PatchGeneLibrary.lastCloseWasButton = true;
                Close();
            }

            //  Ignore
            if (Widgets.ButtonText(
                    new Rect(inRect.xMin + (CloseButSize.x + sep) * 1, inRect.yMax, CloseButSize.x,
                        CloseButSize.y), "Ignore"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Cosmetic", () => { GeneToggleCosmetic(true); }));
                options.Add(new FloatMenuOption("Positive", () => { GeneTogglePositive(true); }));
                options.Add(new FloatMenuOption("Negative", () => { GeneToggleNegative(true); }));
                options.Add(new FloatMenuOption("Archite", () => { GeneToggleArchite(true); }));
                options.Add(new FloatMenuOption("All", () => { GeneToggleAll(true); }));
                Find.WindowStack.Add((Window)new FloatMenu(options));
            }

            //  Allow
            if (Widgets.ButtonText(
                    new Rect(inRect.xMin + (CloseButSize.x + sep) * 2, inRect.yMax, CloseButSize.x,
                        CloseButSize.y), "Allow"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Cosmetic", () => { GeneToggleCosmetic(false); }));
                options.Add(new FloatMenuOption("Positive", () => { GeneTogglePositive(false); }));
                options.Add(new FloatMenuOption("Negative", () => { GeneToggleNegative(false); }));
                options.Add(new FloatMenuOption("Archite", () => { GeneToggleArchite(false); }));
                options.Add(new FloatMenuOption("All", () => { GeneToggleAll(false); }));
                Find.WindowStack.Add((Window)new FloatMenu(options));
            }

            //  Hide Collected Genes
            if (Widgets.ButtonText(
                    new Rect(inRect.xMin + (CloseButSize.x + sep) * 3, inRect.yMax, CloseButSize.x,
                        CloseButSize.y), "Switch Visibility"))
            {
                PatchGeneLibrary.hideState = (PatchGeneLibrary.hideState + 1) % 6;
            }

            //  Save and close
            if (Widgets.ButtonText(
                    new Rect(inRect.xMin + (CloseButSize.x + sep) * 4, inRect.yMax, CloseButSize.x,
                        CloseButSize.y), "Save and Close"))
            {
                SettingsRef.CommitGeneIgnore();
                PatchGeneLibrary.lastCloseWasButton = true;
                Close();
            }
        }

        private void GeneToggleCosmetic(bool isIgnore)
        {
            HashSet<GeneDef> geneList = new HashSet<GeneDef>();
            foreach (GeneDef gene in GeneUtility.GenesInOrder)
            {
                if ((gene.biostatCpx == 0) && (gene.biostatMet == 0))
                {
                    geneList.Add(gene);
                }
            }
            if (isIgnore)
            {
                SettingsRef.AddToGeneIgnore(geneList);
            }
            else
            {
                SettingsRef.RemoveFromGeneIgnore(geneList);
            }
        }

        private void GeneTogglePositive(bool isIgnore)
        {
            HashSet<GeneDef> geneList = new HashSet<GeneDef>();
            foreach (GeneDef gene in GeneUtility.GenesInOrder)
            {
                if (gene.biostatMet < 0)
                {
                    geneList.Add(gene);
                }
            }
            if (isIgnore)
            {
                SettingsRef.AddToGeneIgnore(geneList);
            }
            else
            {
                SettingsRef.RemoveFromGeneIgnore(geneList);
            }
        }

        private void GeneToggleNegative(bool isIgnore)
        {
            HashSet<GeneDef> geneList = new HashSet<GeneDef>();
            foreach (GeneDef gene in GeneUtility.GenesInOrder)
            {
                if (gene.biostatMet > 0)
                {
                    geneList.Add(gene);
                }
            }
            if (isIgnore)
            {
                SettingsRef.AddToGeneIgnore(geneList);
            }
            else
            {
                SettingsRef.RemoveFromGeneIgnore(geneList);
            }
        }

        private void GeneToggleArchite(bool isIgnore)
        {
            HashSet<GeneDef> geneList = new HashSet<GeneDef>();
            foreach (GeneDef gene in GeneUtility.GenesInOrder)
            {
                if (gene.biostatArc > 0)
                {
                    geneList.Add(gene);
                }
            }
            if (isIgnore)
            {
                SettingsRef.AddToGeneIgnore(geneList);
            }
            else
            {
                SettingsRef.RemoveFromGeneIgnore(geneList);
            }
        }

        private void GeneToggleAll(bool isIgnore)
        {
            HashSet<GeneDef> geneList = new HashSet<GeneDef>();
            foreach (GeneDef gene in GeneUtility.GenesInOrder)
            {
                geneList.Add(gene);
            }
            if (isIgnore)
            {
                SettingsRef.AddToGeneIgnore(geneList);
            }
            else
            {
                SettingsRef.RemoveFromGeneIgnore(geneList);
            }
        }


        private void DrawGenesInfo(
            Rect rect,
            float initialHeight,
            ref Vector2 size,
            ref Vector2 scrollPosition)
        {
            var rect1 = rect;
            var position = rect1.ContractedBy(10f);
            GUI.BeginGroup(position);
            var height = 0;
            var rect2 = new Rect(0.0f, 0.0f, position.width, (float)(position.height - (double)height - 12.0));
            DrawGeneSections(rect2, ref scrollPosition);
            var rect3 = new Rect(0.0f, rect2.yMax + 6f, (float)(position.width - 140.0 - 4.0), height);
            rect3.yMax = (float)(rect2.yMax + (double)height + 6.0);
            rect3.width = position.width;

            if (Event.current.type == EventType.Layout)
            {
                var a = (float)(endogenesHeight + (double)xenogenesHeight + height + 12.0 + 70.0);
                size.y = a <= (double)initialHeight
                    ? initialHeight
                    : Mathf.Min(a, (float)(UI.screenHeight - 35 - 165.0 - 30.0));
                xenogenesHeight = 0.0f;
                endogenesHeight = 0.0f;
            }

            GUI.EndGroup();
        }

        private void DrawGeneSections(
            Rect rect,
            ref Vector2 scrollPosition)
        {
            RecacheGenes();
            GUI.BeginGroup(rect);
            var viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, scrollHeight);
            var curY = 0.0f;
            Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, viewRect);
            var containingRect = viewRect;
            containingRect.y = scrollPosition.y;
            containingRect.height = rect.height;

            DrawSection(rect, false, geneDefs.Count, ref curY, ref xenogenesHeight,
                (i, r) => DrawGeneDef(geneDefs[i], r, GeneType.Endogene, null), containingRect);

            if (Event.current.type == EventType.Layout)
                scrollHeight = curY;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void RecacheGenes()
        {
            geneDefs.Clear();
            geneStatus.Clear();

            geneDefs.AddRange(GeneUtility.GenesInOrder);
            geneDefs.SortGeneDefs();

            foreach (Genepack gp in parent.GetGenepacks(true, true))
            {
                //  Scan each gene
                foreach (GeneDef g in gp.GeneSet.GenesListForReading)
                {
                    if (geneStatus.ContainsKey(g))
                    {
                        if (gp.GeneSet.GenesListForReading.Count == 1)
                        {
                            geneStatus[g] = 2;
                        }
                    }
                    else
                    {
                        geneStatus.Add(g, gp.GeneSet.GenesListForReading.Count == 1 ? 2 : 1);
                    }
                }
            }
        }

        private void DrawSection(
            Rect rect,
            bool xeno,
            int count,
            ref float curY,
            ref float sectionHeight,
            Action<int, Rect> drawer,
            Rect containingRect)
        {
            var num1 = curY;
            var rect1 = new Rect(rect.x, curY, rect.width, sectionHeight);
            
            Widgets.DrawMenuSection(rect1);
            var num2 = (float)((rect.width - 12.0 - 630.0 - 36.0) / 2.0);
            curY += num2;
            var num3 = 0;
            var num4 = 0;
            bool drawn = true;
            for (var index = 0; index < count; ++index)
            {
                if (drawn)
                {
                    if (num4 >= 6)
                    {
                        num4 = 0;
                        ++num3;
                    }
                    else if (index > 0)
                    {
                        ++num4;
                    }
                }

                var other = new Rect((float)(num2 + num4 * 90.0 + num4 * 6.0), (float)(curY + num3 * 90.0 + num3 * 6.0),
                    90f, 90f);
                if (containingRect.Overlaps(other))
                    drawn = DrawGeneDef(geneDefs[index], other, GeneType.Endogene, null);
            }

            curY += (float)((num3 + 1) * 90.0 + num3 * 6.0) + num2;
            

            if (Event.current.type != EventType.Layout)
                return;
            sectionHeight = curY - num1;
        }

        public bool DrawGeneDef(
            GeneDef gene,
            Rect geneRect,
            GeneType geneType,
            string extraTooltip)
        {
            bool res = DrawGeneBasics(gene, geneRect, geneType);
            if (res && !Mouse.IsOver(geneRect))
                return res;
            TooltipHandler.TipRegion(geneRect, (Func<string>)(() =>
            {
                var str = gene.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + gene.DescriptionFull;
                if (!extraTooltip.NullOrEmpty())
                    str = str + "\n\n" + extraTooltip.Colorize(ColorLibrary.RedReadable);
                return str;
            }), 795135468);
            return res;
        }

        private bool DrawGeneBasics(
            GeneDef gene,
            Rect geneRect,
            GeneType geneType)
        {

            Color c = SettingsRef.red;
            if (SettingsRef.ignoredGenes.Contains(gene))
            {
                if (PatchGeneLibrary.hideState > 2) { return false; }
                c = SettingsRef.gray;
            } 
            else if (geneStatus.ContainsKey(gene))
            {
                if (geneStatus[gene] == 1)
                {
                    if (PatchGeneLibrary.hideState % 3 == 2) { return false; }
                    c = SettingsRef.yellow;
                }
                else if (geneStatus[gene] == 2)
                {
                    if (PatchGeneLibrary.hideState % 3 != 0) { return false; }
                    c = SettingsRef.green;
                }
            }
            GUI.BeginGroup(geneRect);
            var rect1 = geneRect.AtZero();
            Widgets.DrawBoxSolid(rect1, c);

            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            Widgets.DrawBox(rect1);
            GUI.color = Color.white;

            var num = rect1.width - Text.LineHeight;
            var rect2 = new Rect((float)(geneRect.width / 2.0 - num / 2.0), 0.0f, num, num);
            var iconColor = gene.IconColor;

            if (gene.biostatArc != 0)
                GUI.DrawTexture(rect2, GeneBackground_Archite.Texture);
            else
                GUI.DrawTexture(rect2, GeneBackground_Xenogene.Texture);

            Widgets.DefIcon(rect2, gene, scale: 0.9f, color: iconColor);

            Text.Font = GameFont.Tiny;
            var height = Text.CalcHeight((string)gene.LabelCap, rect1.width);
            var rect3 = new Rect(0.0f, rect1.yMax - height, rect1.width, height);
            GUI.DrawTexture(new Rect(rect3.x, rect3.yMax - height, rect3.width, height), TexUI.GrayTextBG);
            Text.Anchor = TextAnchor.LowerCenter;
            if (height < (Text.LineHeight - 2.0) * 2.0)
                rect3.y -= 3f;
            Widgets.Label(rect3, gene.LabelCap);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(rect1))
            {
                SettingsRef.ToggleGeneIgnore(gene);
            }

            GUI.EndGroup();
            return true;
        }
    }
}
