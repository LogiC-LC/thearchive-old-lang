using ChainedPuzzles;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Utilities;

namespace TheArchive.Features.Hud
{
#warning TODO: Port to older rundowns
    [RundownConstraint(Utils.RundownFlags.RundownSix, Utils.RundownFlags.Latest)]
    public class PuzzleHUDTweaks : Feature
    {
        public override string Name => "Scan HUD Tweaks";

        public override string Group => FeatureGroups.Hud;

        public override string Description => "Adds an overall alarm class counter to the HUD message for door alarms etc";

        [FeatureConfig]
        public static PuzzleHUDTweaksSettings Settings { get; set; }

        public class PuzzleHUDTweaksSettings
        {
            [FSDisplayName("Use Roman Numerals")]
            [FSDisplayName("使用罗马数字", Language.Chinese)]
            [FSDescription("If Roman Numerals should be used instead of numbers:\nI, II, III, IV, V, VI, ...")]
            [FSDescription("是否使用罗马数字代替数字：\nI、II、III、IV、V、VI 等。", Language.Chinese)]
            public bool UseRomanNumerals { get; set; } = true;

            [FSDisplayName("Ignore Single Scans")]
            [FSDisplayName("忽略单次扫描", Language.Chinese)]
            [FSDescription("If alarms with only a single scan should hide the <color=white>(I/I)</color> text")]
            [FSDescription("如果只有一个扫描的警报是否应隐藏 <color=white>(I/I)</color> 文本", Language.Chinese)]
            public bool IgnoreSingleScans { get; set; } = true;
        }

#if IL2CPP
        [ArchivePatch(typeof(CP_Bioscan_Hud), nameof(CP_Bioscan_Hud.SetVisible))]
        internal static class CP_Bioscan_Hud_SetVisible_Patch
        {
            private static string _ogAtText;
            private static string _ogEnterScanText;

            public static void Postfix(CP_Bioscan_Hud __instance, int puzzleIndex, bool visible)
            {
                if (!__instance.m_atText.Contains("("))
                {
                    _ogAtText = __instance.m_atText;
                    _ogEnterScanText = __instance.m_enterSecurityScanText;
                }

                if (!visible)
                    return;

                // " AT " <- original ("AT" is localized)
                // " (I/IV) AT " <- modified
                var allPuzzleInstances = __instance?.transform?.parent?.GetComponentsInChildren<ChainedPuzzleInstance>();
                
                ChainedPuzzleInstance puzzleInstance;
                if(allPuzzleInstances.Count > 1)
                {
                    // Get the correct ChainedPuzzleInstance if there are multiple; ex: on Reactor Shutdown gameobjects
                    puzzleInstance = allPuzzleInstances
                        .Where(cpi =>
                            cpi.m_chainedPuzzleCores.FirstOrDefault(core =>
                                GetHudFromCore(core)?.Pointer == __instance.Pointer
                            ) != null
                        ).FirstOrDefault();
                }
                else
                {
                    puzzleInstance = allPuzzleInstances.FirstOrDefault();
                }

                if (puzzleInstance == null)
                    return;

                var current = puzzleIndex + 1;
                var total = puzzleInstance.m_chainedPuzzleCores.Count;

                if (total == 1 && Settings.IgnoreSingleScans)
                    return;

                string scanPuzzleProgress;

                if (Settings.UseRomanNumerals)
                {
                    scanPuzzleProgress = $" <color=white>({Utils.ToRoman(current)}/{Utils.ToRoman(total)})</color>";
                }
                else
                {
                    scanPuzzleProgress = $" <color=white>({current}/{total})</color>";
                }

                __instance.m_atText = $"{scanPuzzleProgress}{_ogAtText}";
                __instance.m_enterSecurityScanText = $"{_ogEnterScanText}{scanPuzzleProgress}";
            }

            public static iChainedPuzzleHUD GetHudFromCore(iChainedPuzzleCore core)
            {
                if(core.TryCastTo<CP_Bioscan_Core>(out var bioScan))
                {
                    return bioScan.m_hud;
                }

                if (core.TryCastTo<CP_Cluster_Core>(out var clusterScan))
                {
                    // Cluster Hud is a middle man between core and normal hud
                    return clusterScan.m_hud?.TryCastTo<CP_Cluster_Hud>()?.m_hud;
                }

                return null;
            }
        }
#endif
    }
}
