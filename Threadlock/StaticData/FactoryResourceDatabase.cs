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
    public static class FactoryResourceDatabase
    {
        static Dictionary<string, FactoryResource> _resources;
        public static IReadOnlyDictionary<string, FactoryResource> Resources => _resources;

        static bool _isLoaded = false;

        public static async Task LoadResourcesAsync()
        {
            if (_isLoaded)
                return;

            _resources = new Dictionary<string, FactoryResource>();

            if (File.Exists("Content/Data/FactoryResources.json"))
            {
                var json = await File.ReadAllTextAsync("Content/Data/FactoryResources.json");
                var resources = Json.FromJson<Dictionary<string, FactoryResource>>(json);

                if (resources != null)
                    _resources = resources;
            }

            _isLoaded = true;
        }
    }
}
