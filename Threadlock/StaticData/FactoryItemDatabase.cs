using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.StaticData
{
    public static class FactoryItemDatabase
    {
        static Dictionary<string, FactoryItem> _items;
        public static IReadOnlyDictionary<string, FactoryItem> Items => _items;

        static bool _isLoaded = false;

        public static async Task LoadItemsAsync()
        {
            if (_isLoaded)
                return;

            _items = new Dictionary<string, FactoryItem>();

            if (File.Exists("Content/Data/FactoryItems.json"))
            {
                var json = await File.ReadAllTextAsync("Content/Data/FactoryItems.json");
                var items = Json.FromJson<Dictionary<string, FactoryItem>>(json);

                if (items != null)
                    _items = items;
            }

            _isLoaded = true;
        }
    }
}
