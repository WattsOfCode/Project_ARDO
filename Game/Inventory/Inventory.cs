using System.Collections.Generic;
using ARDO.Core.Entities;

namespace ARDO.Game.Inventory
{
    public class Inventory
    {
        private readonly List<GameItem> _items;

        public int Capacity { get; }

        public Inventory(int capacity)
        {
            Capacity = capacity;
            _items = new List<GameItem>();
        }

        public bool AddItem(GameItem item)
        {
            if (_items.Count >= Capacity)
                return false;

            _items.Add(item);
            return true;
        }

        public void RemoveItem(GameItem item)
        {
            _items.Remove(item);
        }

        public List<GameItem> GetItems()
        {
            return _items;
        }
    }
}