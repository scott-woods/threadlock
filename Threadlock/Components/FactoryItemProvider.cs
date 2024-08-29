using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Components
{
    /// <summary>
    /// Handles providing factory items when requested with a passed in function
    /// </summary>
    public class FactoryItemProvider : Component
    {
        Func<FactoryItemStack> _itemProvider;

        public FactoryItemProvider(Func<FactoryItemStack> itemProvider)
        {
            _itemProvider = itemProvider;
        }

        public FactoryItemStack GetFactoryItem()
        {
            return _itemProvider();
        }
    }
}
