using System;
using System.Collections.Generic;

namespace TinyGiantProblems
{
    [Serializable]
    public struct ItemStack
    {
        public ItemId id;
        public int count;
        public ItemStack(ItemId id, int count) { this.id = id; this.count = count; }
        public bool IsEmpty => id == ItemId.None || count <= 0;
        public static readonly ItemStack Empty = new ItemStack(ItemId.None, 0);
    }

    // Minecraft-style container: slots 0-8 are the hotbar, 9-26 the backpack.
    // Pure C# so it can be unit tested and serialized without a scene.
    [Serializable]
    public sealed class Inventory
    {
        public const int HotbarSize = 9, PackSize = 18, Size = HotbarSize + PackSize;
        public ItemStack[] slots = new ItemStack[Size];
        public int selected;
        public int debrisStackLimit = 16;
        [NonSerialized] public Action Changed;

        public ItemStack Selected => slots[selected];
        public ItemDef SelectedDef => slots[selected].IsEmpty ? null : ItemCatalog.Get(slots[selected].id);

        public int MaxStack(ItemId id)
        {
            var d = ItemCatalog.Get(id);
            if (d == null) return 0;
            return d.IsDebris ? debrisStackLimit : 1;
        }

        public void Select(int index)
        {
            index = ((index % HotbarSize) + HotbarSize) % HotbarSize;
            if (index == selected) return;
            selected = index; Changed?.Invoke();
        }
        public void Scroll(int steps) { if (steps != 0) Select(selected + steps); }

        // Hotbar first, then backpack: same as picking up items in Minecraft.
        static IEnumerable<int> FillOrder() { for (int i = 0; i < Size; i++) yield return i; }

        public int RoomFor(ItemId id)
        {
            int max = MaxStack(id), room = 0;
            if (max <= 0) return 0;
            foreach (var s in slots)
            {
                if (s.IsEmpty) room += max;
                else if (s.id == id && max > 1) room += Math.Max(0, max - s.count);
            }
            return room;
        }

        // Returns how many could NOT be added.
        public int Add(ItemId id, int count)
        {
            int max = MaxStack(id);
            if (max <= 0 || count <= 0) return count;
            int left = count;
            if (max > 1)
                foreach (var i in FillOrder())
                {
                    if (left == 0) break;
                    if (slots[i].IsEmpty || slots[i].id != id || slots[i].count >= max) continue;
                    int put = Math.Min(left, max - slots[i].count);
                    slots[i].count += put; left -= put;
                }
            foreach (var i in FillOrder())
            {
                if (left == 0) break;
                if (!slots[i].IsEmpty) continue;
                int put = Math.Min(left, max);
                slots[i] = new ItemStack(id, put); left -= put;
            }
            if (left != count) Changed?.Invoke();
            return left;
        }

        public bool Has(ItemId id) { foreach (var s in slots) if (!s.IsEmpty && s.id == id) return true; return false; }
        public int Count(ItemId id) { int n = 0; foreach (var s in slots) if (!s.IsEmpty && s.id == id) n += s.count; return n; }

        public ItemStack Take(int slot, int count)
        {
            var s = slots[slot];
            if (s.IsEmpty || count <= 0) return ItemStack.Empty;
            int n = Math.Min(count, s.count);
            s.count -= n;
            slots[slot] = s.count > 0 ? s : ItemStack.Empty;
            Changed?.Invoke();
            return new ItemStack(s.id, n);
        }

        // Drag and drop: merge matching stacks, otherwise swap.
        public void MoveOrSwap(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= Size || to >= Size) return;
            var a = slots[from]; var b = slots[to];
            if (a.IsEmpty) return;
            int max = MaxStack(a.id);
            if (!b.IsEmpty && b.id == a.id && max > 1)
            {
                int put = Math.Min(a.count, max - b.count);
                b.count += put; a.count -= put;
                slots[to] = b; slots[from] = a.count > 0 ? a : ItemStack.Empty;
            }
            else { slots[to] = a; slots[from] = b; }
            Changed?.Invoke();
        }

        // Shift-click: jump between hotbar and backpack.
        public void QuickMove(int slot)
        {
            if (slot < 0 || slot >= Size || slots[slot].IsEmpty) return;
            var s = slots[slot];
            int start = slot < HotbarSize ? HotbarSize : 0, end = slot < HotbarSize ? Size : HotbarSize;
            int max = MaxStack(s.id);
            if (max > 1)
                for (int i = start; i < end && s.count > 0; i++)
                    if (!slots[i].IsEmpty && slots[i].id == s.id && slots[i].count < max)
                    { int put = Math.Min(s.count, max - slots[i].count); slots[i].count += put; s.count -= put; }
            for (int i = start; i < end && s.count > 0; i++)
                if (slots[i].IsEmpty) { slots[i] = s; s = ItemStack.Empty; break; }
            slots[slot] = s.count > 0 ? s : ItemStack.Empty;
            Changed?.Invoke();
        }

        // Removes every debris stack and reports the haul.
        public int TakeAllDebris(out int pieces, Dictionary<ItemId, int> breakdown = null)
        {
            int value = 0; pieces = 0;
            for (int i = 0; i < Size; i++)
            {
                var s = slots[i]; if (s.IsEmpty) continue;
                var d = ItemCatalog.Get(s.id); if (d == null || !d.IsDebris) continue;
                value += d.sellValue * s.count; pieces += s.count;
                if (breakdown != null) breakdown[s.id] = (breakdown.TryGetValue(s.id, out var n) ? n : 0) + s.count;
                slots[i] = ItemStack.Empty;
            }
            if (pieces > 0) Changed?.Invoke();
            return value;
        }

        public void Clear() { for (int i = 0; i < Size; i++) slots[i] = ItemStack.Empty; selected = 0; Changed?.Invoke(); }

        public void Sanitize()
        {
            if (slots == null || slots.Length != Size)
            {
                var old = slots ?? new ItemStack[0];
                slots = new ItemStack[Size];
                Array.Copy(old, slots, Math.Min(old.Length, Size));
            }
            for (int i = 0; i < Size; i++)
            {
                var s = slots[i];
                var d = s.IsEmpty ? null : ItemCatalog.Get(s.id);
                if (d == null || d.kind == ItemKind.Upgrade) { slots[i] = ItemStack.Empty; continue; }
                s.count = Math.Max(1, Math.Min(s.count, MaxStack(s.id)));
                slots[i] = s;
            }
            if (selected < 0 || selected >= HotbarSize) selected = 0;
        }
    }
}
