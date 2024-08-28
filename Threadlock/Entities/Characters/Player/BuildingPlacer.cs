using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.SaveData;
using Threadlock.SceneComponents;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player
{
    public class BuildingPlacer : Component, IUpdatable
    {
        Dictionary<Keys, Type> _buildingMap = new Dictionary<Keys, Type>()
        {
            { Keys.D1, typeof(Fabricator) },
            { Keys.D2, typeof(ConveyorBelt) }
        };

        Type _currentType = typeof(Fabricator);

        Entity _entityToPlace;

        public override void OnEnabled()
        {
            base.OnEnabled();

            _entityToPlace = Game1.Scene.AddEntity(new Fabricator());
            if (_entityToPlace.TryGetComponent<Building>(out var building))
                building.Pickup();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            _entityToPlace?.Destroy();
        }

        public void Update()
        {
            foreach (var kvm in _buildingMap)
            {
                if (Input.IsKeyPressed(kvm.Key))
                {
                    _entityToPlace?.Destroy();
                    _currentType = kvm.Value;

                    _entityToPlace = Game1.Scene.AddEntity((Entity)Activator.CreateInstance(kvm.Value));
                    if (_entityToPlace.TryGetComponent<Building>(out var building))
                        building.Pickup();
                }
            }

            if (Controls.Instance.Melee.IsPressed)
            {
                if (_entityToPlace.TryGetComponent<Building>(out var building))
                {
                    if (building.Place())
                        _entityToPlace = Game1.Scene.AddEntity((Entity)Activator.CreateInstance(_currentType));
                }
            }
        }
    }
}
