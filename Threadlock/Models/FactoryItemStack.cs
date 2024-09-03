using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class FactoryItemStack
    {
        public event Action<int> CountChanged;

        public FactoryItem Item { get; set; }

        int _count;
        public int Count
        {
            get => _count;
            set
            {
                var oldCount = _count;
                _count = Math.Max(0, value);
                
                if (_count != oldCount)
                    CountChanged?.Invoke(_count);
            }
        }

        public FactoryItemStack(FactoryItem item, int initialCount = 0)
        {
            Item = item;
            Count = initialCount;
        }
    }
}
