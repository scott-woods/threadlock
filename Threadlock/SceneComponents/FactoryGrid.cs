using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;

namespace Threadlock.SceneComponents
{
    public class FactoryGrid : SceneComponent
    {
        public event Action<Building> OnBuildingAdded;

        public Dictionary<Vector2, Building> Grid = new Dictionary<Vector2, Building>();

        /// <summary>
        /// get building by its grid position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Building GetBuilding(Vector2 position)
        {
            return Grid.GetValueOrDefault(position);
        }

        /// <summary>
        /// bounds are in tile size, not pixel size
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public List<Building> GetBuildings(Rectangle bounds)
        {
            var buildings = new List<Building>();

            for (int y =  bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    var pos = new Vector2(x, y);
                    var building = GetBuilding(pos);

                    if (building != null)
                        buildings.Add(building);
                }
            }

            return buildings;
        }

        /// <summary>
        /// bounds are in tile size, not pixel size
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool CanPlaceBuilding(Rectangle bounds)
        {
            var buildings = GetBuildings(bounds);
            return buildings.Count == 0;
        }

        public void RegisterBuilding(Building building, Rectangle bounds)
        {
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    var pos = new Vector2(x, y);
                    Grid[pos] = building;
                }
            }

            OnBuildingAdded?.Invoke(building);
        }
    }
}
