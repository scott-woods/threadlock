using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class ItemRecipe
    {
        public FactoryItem Item;
        public int Count;
        public List<ItemIngredient> Ingredients;
        public float TimeToCraft;
    }
}
