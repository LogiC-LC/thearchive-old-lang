using GameData;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Models;
using TheArchive.Interfaces;
using TheArchive.Loader;
using TheArchive.Utilities;
using UnityEngine;

namespace TheArchive.Features.Cosmetic
{
    [RundownConstraint(Utils.RundownFlags.RundownAltOne, Utils.RundownFlags.Latest)]
    public class Glowsticks : Feature
    {
        public override string Name => "Glowsticks!";

        public override string Group => FeatureGroups.Cosmetic;

        public override string Description => "Costomize your glow-y little friends!\n\nAllows you to change the built in glowstick type and/or customize the color to your liking, or color it based on the player who threw the glowstick.";

        public static new IArchiveLogger FeatureLogger { get; set; }

        [FeatureConfig]
        public static GlowstickSettings Settings { get; set; }

        public class GlowstickSettings
        {
            [FSHeader("://Synced to other players")]
            [FSHeader("://同步到其他玩家", Language.Chinese)]
            [FSDisplayName("Override")]
            [FSDisplayName("覆盖", Language.Chinese)]
            [FSDescription("Should any thrown glowsticks type be changed?")]
            [FSDescription("任何投掷荧光棒的类型应该被改变吗?", Language.Chinese)]
            public bool Override { get; set; } = false;

            [FSDisplayName("Override Color")]
            [FSDisplayName("覆盖颜色", Language.Chinese)]
            [FSDescription($"The color overriden glowsticks should be.\n(enable setting above!)\n({nameof(GlowstickType.Orange)} is only available in R5 and later!)\n({nameof(GlowstickType.Yellow)} is only available on ALT://R1 and later!)")]
            [FSDescription($"荧光棒的颜色应该是。\n(启用上述设置!)\n({nameof(GlowstickType.Orange)} 仅在R5和更高版本中可用!)\n({nameof(GlowstickType.Yellow)} 仅在ALT://R1和更高版本上可用!)", Language.Chinese)]
            public GlowstickType OverrideType { get; set; } = GlowstickType.Green;

            [FSDisplayName("Auto Festive Glowsticks")]
            [FSDisplayName("自动切换节日荧光棒", Language.Chinese)]
            [FSDescription("Automatically switch to festive versions on holidays.\n(<#F00>XMas => Red</color>)\n(<color=orange>Halloween => Orange</color>)")]
            [FSDescription("在节假日自动切换到节日版本。\n(<#F00>XMas => Red</color>)\n(<color=orange>Halloween => Orange</color>)", Language.Chinese)]
            public bool AutoFestiveGlowsticks { get; set; } = false;

            [FSHeader("://Locally visible only settings")]
            [FSHeader("://仅在本地可见的设置", Language.Chinese)]
            [FSDisplayName("Local Color")]
            [FSDisplayName("本地颜色", Language.Chinese)]
            [FSDescription($"Change colors for you only\n\n<#404>{nameof(LocalOverrideMode.Default)}</color>: Do whatever the game does\n<#404>{nameof(LocalOverrideMode.ForceColor)}</color>: Force Glowstick Colors\n<#404>{nameof(LocalOverrideMode.UsePlayerColor)}</color>: Player color determines Glowstick color\n\n<color=orange>(NOTICE: The first time per game start, after throwing the first glowstick, the game might stutter a little for a second to generate a texture)</color>")]
            [FSDescription($"只改变你的颜色\n\n<#404>{nameof(LocalOverrideMode.Default)}</color>: 跟随游戏设定\n<#404>{nameof(LocalOverrideMode.ForceColor)}</color>: 强制荧光棒的颜色\n<#404>{nameof(LocalOverrideMode.UsePlayerColor)}</color>: 玩家的颜色决定荧光棒的颜色\n\n<color=orange>(注意:每次游戏开始时，在投掷第一支荧光棒后，游戏可能会在生成纹理时停顿一秒钟)</color>", Language.Chinese)]
            public LocalOverrideMode Mode { get; set; } = LocalOverrideMode.Default;

            [FSDisplayName("Force Color")]
            [FSDisplayName("强制改变颜色")]
            [FSDescription($"Sets the Glowstick color for every glowstick\nSet Mode above to {nameof(LocalOverrideMode.ForceColor)} to use this!")]
            [FSDescription($"设置每个荧光棒的颜色\n将上面的模式设置为 {nameof(LocalOverrideMode.ForceColor)} 用这个!")]
            public SColor ForcedColor { get; set; } = new SColor(0, 1, 0);

            [FSDisplayName("Scale up dark colors")]
             [FSDisplayName("增强阴影效果", Language.Chinese)]
            [FSDescription("Turns dark colors bright to actually make them glow")]
            [FSDescription("将暗色变亮使其发光", Language.Chinese)]
            public bool ScaleUpColors { get; set; } = true;

            [FSDisplayName("Blue is Blue")]
            [FSDisplayName("纯粹的蓝色", Language.Chinese)]
            [FSDescription("Adds a third of the blue component as green to the current color if the green component is below that amount.\n\nBasically makes blue look blue instead of purple.")]
            [FSDescription("如果绿色分量低于某个数值，则将当前颜色的蓝色分量的三分之一添加到绿色分量中。\n\n基本上使蓝色看起来是蓝色而不是紫色。", Language.Chinese)]
            public bool BlueIsBlue { get; set; } = true;

            public enum LocalOverrideMode
            {
                Default,
                ForceColor,
                UsePlayerColor
            }

            public enum GlowstickType
            {
                Green,
                Red,
                Orange,
                Yellow,
            }
        }

        public static bool IsXMas { get; private set; } = false;
        public static bool IsHalloween { get; private set; } = false;

        public override void Init()
        {
            var now = DateTime.UtcNow;
            var xmas = new DateTime(now.Year, 12, 25);
            IsXMas = xmas.AddDays(-1) <= now && now <= xmas.AddDays(7);
            var halloween = new DateTime(now.Year, 10, 31);
            IsHalloween = halloween.AddDays(-1) <= now && now <= halloween.AddDays(7);
        }

        public static Dictionary<GlowstickSettings.GlowstickType, uint> GlowstickLookup = new Dictionary<GlowstickSettings.GlowstickType, uint>()
        {
            { GlowstickSettings.GlowstickType.Green, 114 },
            { GlowstickSettings.GlowstickType.Red, 130 },
            { GlowstickSettings.GlowstickType.Orange, 167 }, // R5+ only!
            { GlowstickSettings.GlowstickType.Yellow, 174 }, // A1+ only!
        };

        [ArchivePatch(typeof(ItemReplicationManager), nameof(ItemReplicationManager.OnItemSpawn))]
        internal static class ItemReplicationManager_OnItemSpawn_Patch
        {
            public static SNet_Player LastItemSpawner { get; set; }

            public static void Prefix(pItemSpawnData spawnData)
            {
                spawnData.owner.GetPlayer(out var player);
                LastItemSpawner = player;
            }
        }

        [ArchivePatch(typeof(GlowstickInstance), nameof(GlowstickInstance.Setup))]
        internal static class GlowstickInstance_Setup_Patch
        {
            public static bool DebugForceEmissionMapRegeneration { get; set; } = false;
            public static Texture2D EmissiveMapMonochrome { get; private set; } = null;
            private static bool _creatingTexture = false;

            public static void Postfix(GlowstickInstance __instance)
            {
                if (Settings.Mode == GlowstickSettings.LocalOverrideMode.Default)
                    return;

                LoaderWrapper.StartCoroutine(ColorGlowstick(__instance));
            }

            public static IEnumerator ColorGlowstick(GlowstickInstance glowstickInstance)
            {
                // Wait a single frame so we don't have to patch a generic method lol
                yield return null;

                Color col;

                switch (Settings.Mode)
                {
                    default:
                    case GlowstickSettings.LocalOverrideMode.Default:
                        yield break;
                    case GlowstickSettings.LocalOverrideMode.ForceColor:
                        col = Settings.ForcedColor.ToUnityColor();
                        break;
                    case GlowstickSettings.LocalOverrideMode.UsePlayerColor:
                        col = ItemReplicationManager_OnItemSpawn_Patch.LastItemSpawner?.PlayerColor ?? new Color(1, 0, 1);
                        break;
                }

                if (Settings.ScaleUpColors)
                {
                    float big = 0;

                    if (col.r > big)
                        big = col.r;

                    if (col.g > big)
                        big = col.g;

                    if (col.b > big)
                        big = col.b;

                    if (big > 0)
                    {
                        float multi = 1 / big;
                        col = new Color(col.r * multi, col.g * multi, col.b * multi);
                    }
                    else
                    {
                        // All 0es - make glowstick white instead of black :p
                        col = Color.white;
                    }
                }

                if (Settings.BlueIsBlue)
                {
                    col = new Color(col.r, Math.Max(col.g, 0.333f * col.b), col.b);
                }

                // Colorize the actual light source
                ChangeLightColor(glowstickInstance, col);

                yield return CreateAndOrAssignEmission(glowstickInstance, col);
            }

            public static void ChangeLightColor(GlowstickInstance glowstickInstance, Color col)
            {
                if (Is.A1OrLater)
                    ChangeLightColorAltAndLater(glowstickInstance, col);

                // Todo: Implement color change for R7 and below ...
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ChangeLightColorAltAndLater(GlowstickInstance glowstickInstance, Color col)
            {
#if IL2CPP
                glowstickInstance.m_LightColorTarget = col;
#endif
            }

            public static IEnumerator CreateAndOrAssignEmission(GlowstickInstance glowstickInstance, Color col)
            {
                // "Glostick_Something_Something" / "GlowStick"
                var material = glowstickInstance.transform.GetChild(0).GetChild(0).GetChildWithExactName("Glowstick_1").GetComponent<MeshRenderer>().material;

                // Colorize the glowstick model
                material.SetVector("_EmissiveColor", col.WithAlpha(0.2f));

                while (_creatingTexture && EmissiveMapMonochrome == null)
                    yield return null;

                if (EmissiveMapMonochrome == null || DebugForceEmissionMapRegeneration)
                {
                    _creatingTexture = true;
                    // The original emission map is colored! Turn into monochrome first or else colors are gonna be all weird ...
                    var originalEmissiveMap = material.GetTexture("_EmissiveMap").TryCastTo<Texture2D>();

                    // Original texture is not readable, Blit into new, readable one to access pixel data
                    RenderTexture tmp = RenderTexture.GetTemporary(originalEmissiveMap.width, originalEmissiveMap.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                    RenderTexture previous = RenderTexture.active;
                    Graphics.Blit(originalEmissiveMap, tmp);
                    RenderTexture.active = previous;

                    Texture2D newEmissiveMap = new Texture2D(originalEmissiveMap.width, originalEmissiveMap.height);

                    yield return null;

                    previous = RenderTexture.active;
                    RenderTexture.active = tmp;

                    newEmissiveMap.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                    newEmissiveMap.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(tmp);

                    yield return null;

                    var ogPixels = newEmissiveMap.GetPixels();
                    var newPixels = new Color[ogPixels.Length];
                    float big = 0f;
                    for (int i = 0; i < ogPixels.Length; i++)
                    {
                        var value = ogPixels[i].grayscale;
                        if (value > big)
                            big = value;
                        newPixels[i] = new Color(value, value, value, ogPixels[i].a);
                        if (i % 17600 == 17599)
                            yield return null;
                    }

                    if (big < 1f && big > 0)
                    {
                        var multi = 1 / big;
                        // Push up all values to get a range from 0 to 1 on the map
                        for (int i = 0; i < newPixels.Length; i++)
                        {
                            newPixels[i] = newPixels[i] * multi;
                            if (i % 17600 == 17599)
                                yield return null;
                        }
                    }

                    newEmissiveMap.SetPixels(newPixels);
                    newEmissiveMap.Apply();

                    EmissiveMapMonochrome = newEmissiveMap;
                    EmissiveMapMonochrome.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

                    _creatingTexture = false;
                }

                material.SetTexture("_EmissiveMap", EmissiveMapMonochrome);
            }
        }

        [ArchivePatch(typeof(ItemReplicationManager), nameof(ItemReplicationManager.ThrowItem))]
        internal static class ItemReplicationManager_ThrowItem_Patch
        {
            private static HashSet<uint> _glowstickIDs;
            public static void Init()
            {
                _glowstickIDs = new HashSet<uint>(GlowstickLookup.Values);
            }

            public static void Prefix(ref pItemData data)
            {
                if (_glowstickIDs.Contains(data.itemID_gearCRC))
                {
                    // Only replace glowsticks!

                    if (Settings.Override && GlowstickLookup.TryGetValue(Settings.OverrideType, out var overrideId))
                    {
                        if (Settings.AutoFestiveGlowsticks)
                        {
                            if (IsXMas)
                                overrideId = GlowstickLookup[GlowstickSettings.GlowstickType.Red];

                            if (IsHalloween)
                                overrideId = GlowstickLookup[GlowstickSettings.GlowstickType.Orange];
                        }

                        if (!ItemDataBlock.GetAllBlocks().Any(x => x.persistentID == overrideId))
                            overrideId = GlowstickLookup[GlowstickSettings.GlowstickType.Green];

                        data.itemID_gearCRC = overrideId;
                        return;
                    }
                }
            }
        }
    }
}
