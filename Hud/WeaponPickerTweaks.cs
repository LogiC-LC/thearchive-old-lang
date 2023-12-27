using CellMenu;
using GameData;
using Gear;
using Player;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Models;
using TheArchive.Interfaces;
using TheArchive.Utilities;
using UnityEngine;

#if Unhollower
using UnhollowerBaseLib;
#elif Il2CppInterop
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

using static TheArchive.Utilities.Utils;
using static TheArchive.Features.Hud.WeaponPickerTweaks.WeaponPickerTweaksSettings;

namespace TheArchive.Features.Hud
{
    [RundownConstraint(RundownFlags.RundownSix, RundownFlags.Latest)]
    public class WeaponPickerTweaks : Feature
    {
        public override string Name => "Weapon Picker Tweaks";

        public override string Group => FeatureGroups.Hud;

        public override string Description => "Allows you to Favorite and Hide Gear in the weapon picker.";

        public new static IArchiveLogger FeatureLogger { get; set; }

        [FeatureConfig]
        public static WeaponPickerTweaksSettings Settings { get; set; }

        public class WeaponPickerTweaksSettings
        {
            [FSDisplayName("Favorite Gear Modifier")]
            [FSDisplayName("收藏装备修改键", Language.Chinese)]
            [FSDescription($"Hold this key and select the Gear in the weapon picker to mark is as <color=orange>{nameof(ItemFavState.Favorite)}</color>.")]
            [FSDescription($"按住此键并在武器选择器中选择装备，标记为<color=orange>{nameof(ItemFavState.Favorite)}</color>。", Language.Chinese)]
            public KeyCode FavoriteToggleKey { get; set; } = KeyCode.LeftAlt;

            [FSHide] // WIP / TODO
            [FSDisplayName("Sort Gear Up Modifier")]
            [FSDisplayName("装备排序上移修改键", Language.Chinese)]
            public KeyCode SortOrderUpKey { get; set; } = KeyCode.W;

            [FSHide] // WIP / TODO
            [FSDisplayName("Sort Gear Down Modifier")]
            [FSDisplayName("装备排序下移修改键", Language.Chinese)]
            public KeyCode SortOrderDownKey { get; set; } = KeyCode.S;

            [FSDisplayName("Hide Gear Modifier")]
            [FSDisplayName("隐藏装备修改键", Language.Chinese)]
            [FSDescription($"Hold this key and select the Gear in the weapon picker to mark is as <color=orange>{nameof(ItemFavState.Hidden)}</color>.")]
            [FSDescription($"按住此键并在武器选择器中选择装备，将其标记为 <color=orange>{nameof(ItemFavState.Hidden)}</color>。", Language.Chinese)]
            public KeyCode HideToggleKey { get; set; } = KeyCode.RightControl;

            [FSHeader(":// Other")]
            [FSHeader(":// 其他", Language.Chinese)]
            public ItemColors Colors { get; set; } = new ItemColors();

            [FSDisplayName("Sort Gear by ...")]
            [FSDisplayName("按...排序装备", Language.Chinese)]
            [FSDescription($"{nameof(SortMode.Default)}: Default Gear order\n{nameof(SortMode.Archetype)}: Sort by Archetype Name (the bottom one)\n{nameof(SortMode.Name)}: Sort by Gear name")]
            [FSDescription($"{nameof(SortMode.Default)}: 默认装备顺序\n{nameof(SortMode.Archetype)}: 按原型名称排序（最后一个）\n{nameof(SortMode.Name)}: 按装备名称排序", Language.Chinese)]
            public SortMode Mode { get; set; } = SortMode.Default;

            [FSDisplayName("Remove Hidden Completely")]
            [FSDisplayName("完全移除隐藏的装备", Language.Chinese)]
            [FSDescription($"Gear marked as <color=orange>{nameof(ItemFavState.Hidden)}</color> will be removed from the weapon picker.")]
            [FSDescription($"标记为 <color=orange>{nameof(ItemFavState.Hidden)}</color> 的装备将从武器选择器中移除。", Language.Chinese)]
            public bool HideRemovesFromMenu { get; set; } = false;

            [FSDisplayName("Disable with MTFO installed")]
            [FSDisplayName("安装MTFO时禁用", Language.Chinese)]
            [FSDescription("Disables this feature if MTFO is installed.")]
            [FSDescription("如果安装了MTFO，将禁用此功能。", Language.Chinese)]
            public bool DisableOnModded { get; set; } = false;

            [FSHide]
            [FSDisplayName("Favorites (Vanilla)")]
            [FSDisplayName("收藏（原版）", Language.Chinese)]
            public List<WeaponPickerEntry> Entries { get; set; } = new List<WeaponPickerEntry>();

            [FSHide]
            [FSDisplayName("Favorites (Modded)")]
            [FSDisplayName("收藏（修改版）", Language.Chinese)]
            public List<WeaponPickerEntry> EntriesModded { get; set; } = new List<WeaponPickerEntry>();

            public void SortUpwards(WeaponPickerEntry entry)
            {
                
            }

            public void SortDownwards(WeaponPickerEntry entry)
            {

            }

            public List<WeaponPickerEntry> GetEntries()
            {
                if (IsPlayingModded)
                    return EntriesModded;
                return Entries;
            }

            public WeaponPickerEntry GetOrCreateEntry(GearIDRange gid, ItemFavState initialState = ItemFavState.Normal)
            {
                var checksum = gid.GetChecksum();
                var entries = GetEntries();
                var entry = entries.FirstOrDefault(e => e.Checksum == checksum);

                if(entry == null)
                {
                    entry = new WeaponPickerEntry
                    {
                        Name = gid.PublicGearName,
                        Checksum = checksum,
                        Category = initialState,
                    };
                    entries.Add(entry);
                }
                return entry;
            }

            public enum SortMode
            {
                Default,
                Archetype,
                Name,
            }

            public class ItemColors
            {
                [FSHeader("Colors")]
                [FSHeader("颜色", Language.Chinese)]
                [FSDisplayName("Only color Indicator")]
                [FSDisplayName("仅颜色指示器", Language.Chinese)]
                [FSDescription("Only colorizes the indicator bar on the left side")]
                [FSDescription("仅对左侧指示栏进行颜色标记", Language.Chinese)]
                public bool IndicatorLeftOnly { get; set; } = false;

                [FSDisplayName("Favorites Color")]
                [FSDisplayName("收藏颜色", Language.Chinese)]
                public SColor ColorFavorite { get; set; } = new SColor(0.92f, 0.76f, 0f);

                //public SColor ColorNormal { get; set; } = new SColor(0.7f, 0.7f, 0.7f);

                [FSDisplayName("Hidden Color")]
                [FSDisplayName("隐藏颜色", Language.Chinese)]
                public SColor ColorHidden { get; set; } = new SColor(0.2f, 0.2f, 0.2f);

                public enum ColorOptions
                {
                    Indicator,
                    Icon,
                    Name,
                }
            }

            public class WeaponPickerEntry
            {
                [FSHeader("--------------------------------")]
                [FSReadOnly]
                public string Name { get; set; }

                [FSReadOnly]
                [FSDescription("The items unique identifier*\n\n<size=50%>*usually unique</size>")]
                [FSDescription("物品的唯一标识符*\n\n<size=50%>*通常是唯一的</size>", Language.Chinese)]
                public uint Checksum { get; set; }

                public ItemFavState Category { get; set; } = ItemFavState.Normal;

                public uint Index { get; set; } = 0;
            }

            public enum ItemFavState
            {
                Favorite,
                Normal,
                Hidden,
            }
        }

        public override bool LateShouldInit()
        {
            return !(IsPlayingModded && Settings.DisableOnModded);
        }

#if IL2CPP

        /*
         *  0: Text below
         *  1: Main Text
         *  2: text above (only alpha)
         */

        [ArchivePatch(typeof(CM_InventorySlotItem), nameof(CM_InventorySlotItem.SetupAsWeaponPicker))]
        internal static class CM_InventorySlotItem_SetupAsWeaponPicker_Patch
        {
            public static void Postfix(CM_InventorySlotItem __instance)
            {
                var entry = Settings.GetOrCreateEntry(__instance.m_gearID);

                switch (entry.Category)
                {
                    case ItemFavState.Favorite:
                        var colFavSelected = Settings.Colors.ColorFavorite.ToUnityColor();
                        __instance.m_colSeleced = colFavSelected;
                        __instance.m_colPassive = colFavSelected;
                        __instance.m_colHover = colFavSelected;
                        if (Settings.Colors.IndicatorLeftOnly)
                            break;
                        __instance.m_textColorOut[1] = colFavSelected;
                        __instance.m_textColorOut[1] = colFavSelected.WithAlpha(0.5f);
                        __instance.m_textColorOver[1] = colFavSelected;
                        __instance.m_nameText.color = __instance.m_textColorOut[1];

                        __instance.m_icon.color = colFavSelected.WithAlpha(0.6176f);// 1 1 1 0.6176
                        break;
                    case ItemFavState.Hidden:
                        var colHiddenSelected = Settings.Colors.ColorHidden.ToUnityColor();
                        __instance.m_colSeleced = colHiddenSelected;
                        __instance.m_colPassive = colHiddenSelected;
                        __instance.m_colHover = colHiddenSelected;
                        if (Settings.Colors.IndicatorLeftOnly)
                            break;
                        __instance.m_textColorOut[1] = colHiddenSelected;
                        __instance.m_textColorOut[1] = colHiddenSelected.WithAlpha(0.5f);
                        __instance.m_textColorOver[1] = colHiddenSelected;
                        __instance.m_nameText.color = __instance.m_textColorOut[1];

                        __instance.m_icon.color = colHiddenSelected.WithAlpha(0.6176f);// 1 1 1 0.6176
                        break;
                    default:
                        break;
                }
            }
        }

        //public void OnWeaponSlotItemSelected(CM_InventorySlotItem slotItem)
        [ArchivePatch(typeof(CM_PlayerLobbyBar), nameof(CM_PlayerLobbyBar.OnWeaponSlotItemSelected))]
        internal static class CM_PlayerLobbyBar_OnWeaponSlotItemSelected_Patch
        {
            private static bool _itsMeee = false;

            public static void Postfix(CM_PlayerLobbyBar __instance, CM_InventorySlotItem slotItem)
            {
                if (_itsMeee)
                    return;

                var gearID = slotItem.m_gearID;

                var entry = Settings.GetOrCreateEntry(gearID);

                var actionTaken = HandleInput(entry);

                if (actionTaken)
                {
                    MarkSettingsAsDirty(Settings);
                    __instance.UnSelect();
                    __instance.m_popupVisible = false;
                    __instance.m_popupHolder.gameObject.SetActive(false);
                    __instance.m_popupScrollWindow.ResetInfoBox();
                    __instance.m_popupScrollWindow.SetVisible(false);

                    var lobbyBarSlotItem = __instance.m_inventorySlotItems[slotItem.m_slot];

                    _itsMeee = true;
                    __instance.ShowWeaponSelectionPopup(slotItem.m_slot, lobbyBarSlotItem.m_guiAlign);
                    _itsMeee = false;
                }
            }

            private static bool HandleInput(WeaponPickerEntry entry)
            {
                if (Input.GetKey(Settings.FavoriteToggleKey))
                {
                    if (entry.Category == ItemFavState.Favorite)
                    {
                        entry.Category = ItemFavState.Normal;
                        return true;
                    }

                    entry.Category = ItemFavState.Favorite;
                    return true;
                }

                if (Input.GetKey(Settings.HideToggleKey))
                {
                    if (entry.Category == ItemFavState.Hidden)
                    {
                        entry.Category = ItemFavState.Normal;
                        return true;
                    }

                    entry.Category = ItemFavState.Hidden;
                    return true;
                }

                /* // TODO:
                if (Input.GetKey(Settings.SortOrderUpKey))
                {
                    Settings.SortUpwards(entry);
                    return true;
                }

                if (Input.GetKey(Settings.SortOrderDownKey))
                {
                    Settings.SortDownwards(entry);
                    return true;
                }
                */

                return false;
            }
        }

        // private void UpdateWeaponWindowInfo(InventorySlot slot, Transform align)
        [ArchivePatch(typeof(CM_PlayerLobbyBar), nameof(CM_PlayerLobbyBar.UpdateWeaponWindowInfo))]
        public static class CM_PlayerLobbyBar_UpdateWeaponWindowInfo_Patch
        {
            public static bool InMethod { get; private set; }

            public static void Prefix()
            {
                InMethod = true;
            }

            public static void Postfix()
            {
                InMethod = false;
            }
        }

        //public static GearIDRange[] GetAllGearForSlot(InventorySlot slot)
        [ArchivePatch(typeof(GearManager), nameof(GearManager.GetAllGearForSlot))]
        public static class GearManager_GetAllGearForSlot_Patch
        {
            public static void Postfix(GearManager __instance, ref Il2CppReferenceArray<GearIDRange> __result)
            {
                if (!CM_PlayerLobbyBar_UpdateWeaponWindowInfo_Patch.InMethod)
                    return;

                FeatureLogger.Notice("Returning sorted Gear!");

                var allGear = __result.ToArray();

                var dict = new Dictionary<int, GearIDRange>();

                for(int i = 0; i < allGear.Length; i++)
                {
                    dict.Add(i, allGear[i]);
                }

                var orderedGear = dict.OrderBy((kvp) => GetOrderString(kvp))
                                        .Select(kvp => kvp.Value);
                __result = FilterDisabled(orderedGear).ToArray();
            }
        }

        public static IEnumerable<GearIDRange> FilterDisabled(IEnumerable<GearIDRange> gearIDs)
        {
            if (!Settings.HideRemovesFromMenu)
            {
                foreach (var gearID in gearIDs)
                    yield return gearID;

                yield break;
            }

            foreach(var gearID in gearIDs)
            {
                var entry = Settings.GetOrCreateEntry(gearID);

                if (entry.Category == ItemFavState.Hidden)
                    continue;

                yield return gearID;
            }
        }

        public static string GetOrderString(KeyValuePair<int, GearIDRange> kvp)
        {
            int id = kvp.Key;
            GearIDRange gid = kvp.Value;

            var builder = new StringBuilder();

            // num -> status
            // 0 - Fav
            // 1 - Normal
            // 2 - Hidden

            var entry = Settings.GetOrCreateEntry(gid);

            builder.Append((int)entry.Category);

            if(entry.Index > 999)
            {
                entry.Index = 999;
            }

            builder.Append($"_{entry.Index.ToString("D3")}");

            switch(Settings.Mode)
            {
                default:
                case SortMode.Default:
                    builder.Append($"_{id}");
                    break;
                case SortMode.Archetype:
                    builder.Append($"_{GetArchetypeName(gid)}");
                    break;
                case SortMode.Name:
                    builder.Append($"_{entry.Name}");
                    break;
            }

            builder.Append($"_{entry.Name}");

            builder.Append($"_{id}");

            return builder.ToString();
        }

        public static string GetArchetypeName(GearIDRange idRange)
        {
            uint categoryID = idRange.GetCompID(eGearComponent.Category);

            GearCategoryDataBlock gearCategoryDB = GameDataBlockBase<GearCategoryDataBlock>.GetBlock(categoryID);

            ItemDataBlock itemDB = ItemDataBlock.GetBlock(gearCategoryDB.BaseItem);

            if (itemDB.inventorySlot == InventorySlot.GearMelee)
            {
                if (Is.R6OrLater)
                    GetMeleeArchetypeName(gearCategoryDB);
            }
            else
            {
                eWeaponFireMode weaponFireMode = (eWeaponFireMode)idRange.GetCompID(eGearComponent.FireMode);

                bool isSentryGun = categoryID == 12; // => PersistentID of the Sentry Gun Category

                ArchetypeDataBlock archetypeDB = isSentryGun
                    ? SentryGunInstance_Firing_Bullets.GetArchetypeDataForFireMode(weaponFireMode)
                    : ArchetypeDataBlock.GetBlock(GearBuilder.GetArchetypeID(gearCategoryDB, weaponFireMode));

                if(archetypeDB != null)
                    return archetypeDB.PublicName;
            }

            return "zError";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetMeleeArchetypeName(GearCategoryDataBlock gearCatDB)
        {
            return MeleeArchetypeDataBlock.GetBlock(GearBuilder.GetMeleeArchetypeID(gearCatDB)).PublicName;
        }

#endif
    }
}
