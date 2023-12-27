using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Interfaces;
using TheArchive.Utilities;

namespace TheArchive.Features.QoL
{
    public class ReloadSoundCue : Feature
    {
        public override string Name => "Reload Sound Cue";

        public override string Group => FeatureGroups.QualityOfLife;

        public override string Description => "Play a sound cue on reload the moment the bullets have entered your gun.";

        public new static IArchiveLogger FeatureLogger { get; set; }

        [FeatureConfig]
        public static ReloadSoundCueSettings Settings { get; set; }

        public class ReloadSoundCueSettings
        {
            [FSDisplayName("Test Sound Event")]
            [FSDisplayName("测试音效事件", Language.Chinese)]
            [FSDescription("Plays the sound event below,\nit's a little janky and might not work depending on the sound, sorry!")]
            [FSDescription("播放下面的音效事件，可能有点不稳定，取决于音效，抱歉！", Language.Chinese)]
            public FButton TestSoundButton { get; set; } = new FButton("Play Sound");

            [FSDescription("The sound event to play whenever the reaload has happend.")]
            [FSDescription("重新加载发生时要播放的音效事件。", Language.Chinese)]
            public string SoundEvent { get; set; } = nameof(AK.EVENTS.HACKING_PUZZLE_CORRECT);
            
            [FSDisplayName("Print Sound Events To Console")]
            [FSDisplayName("打印声音事件到控制台", Language.Chinese)]
            [FSDescription("Prints all available sound events to the <b><#F00>console</color></b>.\n<color=orange>Warning! This might freeze your game for a few seconds!</color>\n\nSome events might not work because their playback conditions are not met!")]
            [FSDescription("将所有可用音效事件打印到<b><#F00>控制台</color></b>。\n<color=orange>警告！这可能会导致游戏停顿几秒钟！</color>\n\n某些事件可能无法正常工作，因为它们的播放条件未满足！", Language.Chinese)]
            public FButton PrintSoundEventsButton { get; set; } = new FButton("Print To Console");
        }

        private static bool _hasPrintedEvents = false;
        public override void OnButtonPressed(ButtonSetting setting)
        {
            if(setting.ButtonID == nameof(ReloadSoundCueSettings.TestSoundButton))
            {
                PlaySound();
            }

            if (setting.ButtonID == nameof(ReloadSoundCueSettings.PrintSoundEventsButton))
            {
                if(!_hasPrintedEvents)
                {
                    _hasPrintedEvents = true;
                    SoundEventCache.DebugLog(FeatureLogger);
                }
                else
                {
                    FeatureLogger.Info("Sound events have already been printed once, skipping! :)");
                }
            }
        }

        private static void PlaySound()
        {
            var localPlayer = Player.PlayerManager.GetLocalPlayerAgent();
            if (localPlayer != null && localPlayer.Sound != null)
            {
                if (SoundEventCache.TryResolve(Settings.SoundEvent, out var soundId))
                {
                    localPlayer.Sound.SafePost(soundId);
                }
                else
                {
                    localPlayer.Sound.SafePost(SoundEventCache.Resolve(nameof(AK.EVENTS.HACKING_PUZZLE_CORRECT)));
                }
            }
        }

        [ArchivePatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.DoReload))]
        internal static class PlayerInventoryLocal_DoReload_Patch
        {
            public static void Postfix()
            {
                PlaySound();
            }
        }
    }
}
