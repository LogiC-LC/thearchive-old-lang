using Player;
using System;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using static TheArchive.Utilities.Utils;

namespace TheArchive.Features.Accessibility
{
    [RundownConstraint(RundownFlags.RundownTwo, RundownFlags.Latest)]
    internal class GlassLiquidOverride : Feature
    {
        public override string Name => "Glass Liquid System Override";

        public override string Group => FeatureGroups.Accessibility;

        public override string Description => "Adjust the games \"Glass Liquid System\"\nThe thing that renders the blood splatters etc on your visor.";

        [FeatureConfig]
        public static GlassLiquidOverrideSettings Settings { get; set; }

        public class GlassLiquidOverrideSettings
        {
            [FSDisplayName("Enable Liquid System")]
            [FSDisplayName("启用液体系统", Language.Chinese)]
            [FSDescription("Turning this off stops the Glass Liquid System from updating.\n\nMake sure that nothing's left on your visor or else it'll be stuck there until you re-enable this setting.")]
            [FSDescription("关闭此选项会停止玻璃液体系统的更新。\n\n确保面罩上没有残留物，否则它会一直停留在那里，直到重新启用此设置。", Language.Chinese)]
            public bool EnableGlassLiquidSystem { get; set; } = true;

            [FSDescription("The resolution used to render the liquid effects.\n\nIncreasing this settings might decrease performance on weaker hardware!")]
            [FSDescription("用于渲染液体效果的分辨率。\n\n增加此设置可能会降低性能，特别是在性能较低的硬件上！", Language.Chinese)]
            public GlassLiquidSystemQuality Quality { get; set; } = GlassLiquidSystemQuality.Default;

            [FSDisplayName("Clean Screen")]
            [FSDisplayName("清理屏幕", Language.Chinese)]
            [FSDescription("Applies the Disinfect Station visuals to clear any other liquids.\n\n(Might fix bad cases of liquid bug if spammed a bunch!)")]
            [FSDescription("应用消毒站视觉效果以清除其他液体。\n\n（如果频繁使用可能修复液体BUG的严重情况！)", Language.Chinese)]
            public FButton ApplyDisinfectButton { get; set; } = new FButton("Clean");

            [FSHeader("Warning")]
            [FSHeader("警告", Language.Chinese)]
            [FSDisplayName("Completely Disable System")]
            [FSDisplayName("完全禁用系统", Language.Chinese)]
            [FSDescription("Toggling this option on <u>completely disables the Glass Liquid System</u> and makes you <u>unable to re-enable it mid game</u>.\nMight save some frames on lower end graphics hardware.\n\n<color=orange>A re-drop into the level is required after disabling this to re-enable the system!</color>")]
            [FSDescription("启用此选项<u>完全禁用玻璃液体系统</u>，使您<u>无法在游戏中重新启用它</u>。\n可能会在性能较低的图形硬件上提高一些帧率。\n\n<color=orange>禁用后，需要重新进入关卡才能重新启用系统！</color>", Language.Chinese)]
            public bool CompletelyDisable { get; set; } = false;
        }

        public enum GlassLiquidSystemQuality
        {
            VeryBad = 1,
            Bad,
            SlightlyWorse,
            Default,
            Good,
            Better,
            Amazing,
            Extraordinary
        }

        private static bool _isDisabling = false;

        public override void OnEnable()
        {
            _isDisabling = false;
            SetGlassLiquidSystemActive(Settings.EnableGlassLiquidSystem);
        }

        public override void OnDisable()
        {
            _isDisabling = true;
            SetGlassLiquidSystemActive(true);
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (Settings.CompletelyDisable)
                return;

            if (setting.ButtonID.Contains(nameof(GlassLiquidOverrideSettings.ApplyDisinfectButton)))
                ScreenLiquidManager.DirectApply(ScreenLiquidSettingName.disinfectionStation_Apply, new UnityEngine.Vector2(0.5f, 0.5f), UnityEngine.Vector2.zero);
        }

        public override void OnFeatureSettingChanged(FeatureSetting setting)
        {
            SetGlassLiquidSystemActive(Settings.EnableGlassLiquidSystem);
        }

        public static void SetGlassLiquidSystemActive(bool active, PlayerAgent player = null)
        {
            player ??= PlayerManager.GetLocalPlayerAgent();

            if (player == null || !player.IsLocallyOwned)
                return;

            var fpsCamera = player.FPSCamera;

            if (fpsCamera == null)
                return;

            var gls = fpsCamera.GetComponent<GlassLiquidSystem>();

            if (Settings.CompletelyDisable && !_isDisabling)
            {
                if (gls != null)
                {
                    UnityEngine.Object.Destroy(gls);
                }
                ScreenLiquidManager.Clear();
                ScreenLiquidManager.LiquidSystem = null;
                SetCollectCommandsScreenLiquid(fpsCamera, false);
                return;
            }

            if (gls == null)
                return;

            if (active)
            {
                SetCollectCommandsScreenLiquid(fpsCamera, true);
                gls.OnResolutionChange(UnityEngine.Screen.currentResolution);
            }
            else
            {
                ScreenLiquidManager.Clear();
                ScreenLiquidManager.LiquidSystem = null;
                SetCollectCommandsScreenLiquid(fpsCamera, false);
            }
        }

        private static void SetCollectCommandsScreenLiquid(FPSCamera camera, bool value)
        {
            if (Is.R6OrLater)
                SetCollectCommandsScreenLiquidR6(camera, value);
        }

        private static void SetCollectCommandsScreenLiquidR6(FPSCamera camera, bool value)
        {
#if IL2CPP
            camera.CollectCommandsScreenLiquid = value;
#endif
        }

#if IL2CPP
        [RundownConstraint(RundownFlags.RundownSix, RundownFlags.Latest)]
        [ArchivePatch(nameof(LocalPlayerAgent.Setup))]
        internal static class LocalPlayerAgent_Setup_Patch
        {
            public static Type Type() => typeof(LocalPlayerAgent);

            public static void Postfix(LocalPlayerAgent __instance) => PlayerAgent_Setup_Patch.Postfix(__instance);
        }
#endif

        [RundownConstraint(RundownFlags.RundownOne, RundownFlags.RundownFive)]
        [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
        internal static class PlayerAgent_Setup_Patch
        {
            public static void Postfix(PlayerAgent __instance)
            {
                if (!__instance.IsLocallyOwned)
                    return;

                SetGlassLiquidSystemActive(Settings.EnableGlassLiquidSystem, __instance);
            }
        }

        [ArchivePatch(nameof(GlassLiquidSystem.Setup))]
        internal static class GlassLiquidSystem_Setup_Patch
        {
            public static Type Type() => typeof(GlassLiquidSystem);
            public static void Prefix(ref int quality)
            {
                quality = (int)Settings.Quality;
            }
        }
    }
}
