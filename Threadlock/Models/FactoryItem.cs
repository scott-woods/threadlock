using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    /// <summary>
    /// defines the properties of a factory item
    /// </summary>
    public class FactoryItem
    {
        public string Name;
        public string Description;
        public string InventoryIcon;
        public int MaxStackSize = 100;
    }
}
