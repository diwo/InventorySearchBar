﻿using CriticalCommonLib.Models;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace InventorySearchBar.Inventories
{
    internal abstract unsafe class Inventory
    {
        public abstract string AddonName { get; }

        protected IntPtr _addon = IntPtr.Zero;
        public IntPtr Addon => _addon;

        protected AtkUnitBase* _node => (AtkUnitBase*)_addon;

        protected List<List<bool>> _emptyFilter = null!;
        protected List<List<bool>>? _filter = null!;

        protected abstract ulong CharacterId { get; }
        protected abstract InventoryCategory Category { get; }
        protected abstract int FirstBagOffset { get; }

        protected const int kGridItemCount = 35;

        public bool IsVisible => _node != null && _node->IsVisible;
        public bool IsFocused()
        {
            if (_node == null || !_node->IsVisible) { return false; }
            if (_node->UldManager.NodeListCount < 2) { return false; }

            AtkComponentNode* window = _node->UldManager.NodeList[1]->GetAsAtkComponentNode();
            if (window == null || window->Component->UldManager.NodeListCount < 4) { return false; }

            return window->Component->UldManager.NodeList[3]->IsVisible;
        }

        public void UpdateAddonReference()
        {
            _addon = Plugin.GameGui.GetAddonByName(AddonName, 1);
        }

        public void ApplyFilter(string searchTerm)
        {
            if (searchTerm.Length < 1)
            {
                _filter = null;
                return;
            }

            _filter = new List<List<bool>>(_emptyFilter);

            string text = searchTerm.ToUpper();
            List<InventoryItem> items = Plugin.InventoryMonitor.GetSpecificInventory(CharacterId, Category);

            foreach (InventoryItem item in items)
            {
                bool highlight = false;
                if (item.Item != null)
                {
                    highlight = item.Item.Name.ToString().ToUpper().Contains(text);
                }

                int bagIndex = (int)item.SortedContainer - FirstBagOffset;
                if (_filter.Count > bagIndex)
                {
                    List<bool> bag = _filter[bagIndex];
                    int slot = kGridItemCount - 1 - item.SortedSlotIndex;
                    if (bag.Count > slot)
                    {
                        bag[slot] = highlight;
                    }
                }
            }
        }

        public void UpdateHighlights()
        {
            InternalUpdateHighlights(false);
        }

        protected abstract void InternalUpdateHighlights(bool forced = false);

        public void ClearHighlights()
        {
            _filter = null;
            InternalUpdateHighlights(true);
        }

        protected unsafe void UpdateGridHighlights(AtkUnitBase* grid, int startIndex, int bagIndex, int count = kGridItemCount)
        {
            if (grid == null) { return; }

            for (int j = startIndex; j < startIndex + count; j++)
            {
                bool highlight = true;
                if (_filter != null && _filter[bagIndex].Count > j - startIndex)
                {
                    highlight = _filter[bagIndex][j - startIndex];
                }

                SetNodeHighlight(grid->UldManager.NodeList[j], highlight);
            }
        }

        protected static unsafe void SetNodeHighlight(AtkResNode* node, bool highlight)
        {
            node->MultiplyRed = highlight ? (byte)100 : (byte)20;
            node->MultiplyGreen = highlight ? (byte)100 : (byte)20;
            node->MultiplyBlue = highlight ? (byte)100 : (byte)20;
        }

        public static unsafe void SetTabHighlight(AtkResNode* tab, bool highlight)
        {
            tab->MultiplyRed = highlight ? (byte)250 : (byte)100;
            tab->MultiplyGreen = highlight ? (byte)250 : (byte)100;
            tab->MultiplyBlue = highlight ? (byte)250 : (byte)100;
        }

        public static unsafe bool GetTabEnabled(AtkComponentBase* tab)
        {
            if (tab->UldManager.NodeListCount < 2) { return false; }

            return tab->UldManager.NodeList[2]->IsVisible;
        }

        public static unsafe bool GetSmallTabEnabled(AtkComponentBase* tab)
        {
            if (tab->UldManager.NodeListCount < 1) { return false; }

            return tab->UldManager.NodeList[1]->IsVisible;
        }
    }
}
