using Player;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Utilities;
using static TheArchive.Utilities.Utils;

namespace TheArchive.Features.Hud
{
    [RundownConstraint(RundownFlags.RundownSix, RundownFlags.Latest)]
    public class InteractReviveFix : Feature
    {
        public override string Name => "Fix Multi-Revive UI";

        public override string Group => FeatureGroups.Hud;

        public override string Description => "Fix revive progress <i>visually</i> resetting whenever multiple revives are going on at the same time.";


        [FeatureConfig]
        public static InteractReviveFixSettings Settings { get; set; }

        public class InteractReviveFixSettings
        {
            [FSDisplayName("Add Multi-Res Warning")]
            [FSDisplayName("添加多人复活警告", Language.Chinese)]
            [FSDescription("Display a warning whenever multiple people are trying to revive the same person.")]
            [FSDescription("当多人尝试复活同一个人时显示警告。", Language.Chinese)]
            public bool AddMultiResWarning { get; set; } = true;

            [FSDisplayName("Multi-Res Warning Text")]
            [FSDisplayName("多人复活警告文本", Language.Chinese)]
            [FSDescription("The warning text to display.\nDefault is: <#F00><u>/!\\</u></color> <u>Double</u>")]
            [FSDescription("要显示的警告文本。\n默认为：<#F00><u>/!\\</u></color> <u>Double</u>", Language.Chinese)]
            public string MultiResWarning { get; set; } = "<#F00><u>/!\\</u></color> <u>Double</u>";

            [FSHeader("Advanced Multi-Res HUD Text")]
            [FSHeader("高级多人复活 HUD 文本", Language.Chinese)]
            [FSDisplayName("Ignore Localization")]
            [FSDisplayName("忽略本地化", Language.Chinese)]
            [FSDescription("If the revive text should not be translated using the game and instead be replaced completely by the settings below whenever multiple people are reviving the same person.\n\n(Enable this to use the settings below!)")]
            [FSDescription("如果复活文本不应使用游戏的翻译，而完全使用以下设置来替代，当有多人复活同一个人时。\n\n（启用此选项以使用以下设置！）", Language.Chinese)]
            public bool ForceOverrideStringsUsage { get; set; } = false;

            [FSDisplayName("Multi-Res Others Text")]
            [FSDisplayName("多人复活其他人文本", Language.Chinese)]
            [FSDescription("Displayed whenever you're reviving with multiple people.\n\nUse \"{0}\" to insert the name of the person you're reviving.\nUse \"{1}\" to insert the names of all the people reviving (excluding you)")]
            [FSDescription("当你与多人一起复活时显示。\n\n使用 \"{0}\" 来插入被复活的人的名字。\n使用 \"{1}\" 来插入所有正在复活的人的名字（不包括你）", Language.Chinese)]
            public string MultiReviveOthersOverride { get; set; } = "Reviving {0} together with {1}.";

            [FSDisplayName("Multi-Res Yourself Text")]
            [FSDisplayName("多人复活自己文本", Language.Chinese)]
            [FSDescription("Displayed whenever you're getting revived by multiple people.\n\nUse \"{0}\" to insert the name of all the people reviving you.\nUse \"{1}\" to insert your name.")]
            [FSDescription("当有多人为你复活时显示。\n\n使用 \"{0}\" 来插入所有为你复活的人的名字。\n使用 \"{1}\" 来插入你的名字。", Language.Chinese)]
            public string GettingMultiRevivedOverride { get; set; } = "{0} are reviving you.";
        }

#if IL2CPP
        // Called once at the start and at the end of every players revive interaction
        [ArchivePatch(typeof(Interact_Revive), nameof(Interact_Revive.SetUIState))]
        internal static class Interact_Revive_SetUIState_Patch
        {
            public static bool Prefix(Interact_Revive __instance)
            {
                var deadGuy = __instance.m_interactTargetAgent;
                if (deadGuy.IsLocallyOwned)
                    return ArchivePatch.RUN_OG;

                foreach (var interactors in __instance.m_interactors)
                {
                    if (interactors.Agent.IsLocallyOwned)
                    {
                        return ArchivePatch.RUN_OG;
                    }
                }

                return ArchivePatch.SKIP_OG;
            }

            private static readonly List<string> _interactorsTemp = new List<string>();

            public static void Postfix(Interact_Revive __instance, bool state)
            {
                if (state && __instance.m_interactors.Count > 1)
                {
                    _interactorsTemp.Clear();

                    foreach (var interactors in __instance.m_interactors)
                    {
                        _interactorsTemp.Add(interactors.Agent.InteractionName);
                    }


                    if (__instance.m_interactTargetAgent.IsLocallyOwned)
                    {
                        var combinedInteractors = string.Join(", ", _interactorsTemp);
                        var multiRevYouText = SharedUtils.GetLocalizedTextSafeAndFormat(3655755863U, Settings.ForceOverrideStringsUsage, Settings.GettingMultiRevivedOverride, combinedInteractors, __instance.m_interactTargetAgent.InteractionName);
                        GuiManager.InteractionLayer.SetInteractPrompt(multiRevYouText, "", ePUIMessageStyle.Default);
                        return;
                    }


                    _interactorsTemp.Remove(PlayerManager.GetLocalPlayerAgent().InteractionName);
                    var reviveText = SharedUtils.GetLocalizedTextSafeAndFormat(852U, Settings.ForceOverrideStringsUsage, Settings.MultiReviveOthersOverride, __instance.m_interactTargetAgent.InteractionName, string.Join(", ", _interactorsTemp));
                    
                    if(Settings.AddMultiResWarning)
                    {
                        reviveText = $"{Settings.MultiResWarning} {reviveText}";
                    }

                    GuiManager.InteractionLayer.SetInteractPrompt(reviveText, "", ePUIMessageStyle.Default);
                }
            }
        }

        // Called everytime the timer updates on any players revive
        [ArchivePatch(typeof(Interact_Timed), nameof(Interact_Timed.OnTimerUpdate))]
        internal static class Interact_Timed_OnTimerUpdate_Patch
        {
            public static bool Prefix(Interact_Timed __instance, float timeRel)
            {
                // Had to patch the base class here because Interact_Revive does not override this method
                // Ignore all instances that aren't revives!
                if (__instance.TryCastTo<Interact_Revive>() == null)
                    return ArchivePatch.RUN_OG;

                var deadGuy = __instance.m_interactTargetAgent;
                if (deadGuy.IsLocallyOwned)
                    return ArchivePatch.RUN_OG;

                foreach (var interactors in __instance.m_interactors)
                {
                    if (interactors.Agent.IsLocallyOwned)
                    {
                        return ArchivePatch.RUN_OG;
                    }
                }

                return ArchivePatch.SKIP_OG;
            }
        }
#endif
    }
}
