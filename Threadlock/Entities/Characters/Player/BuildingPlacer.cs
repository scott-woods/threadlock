using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections;
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

        Entity _currentBuildingEntity;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var factoryGrid = Entity.Scene.GetSceneComponent<FactoryGrid>();
            factoryGrid.OnBuildingAdded += OnBuildingPlaced;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            var factoryGrid = Entity.Scene.GetSceneComponent<FactoryGrid>();
            factoryGrid.OnBuildingAdded -= OnBuildingPlaced;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            InstanceBuilding(_currentType);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            RemoveBuilding();
        }

        public void Update()
        {
            //handle switching active building
            foreach (var kvm in _buildingMap)
            {
                if (Input.IsKeyPressed(kvm.Key))
                {
                    _currentType = kvm.Value;

                    InstanceBuilding(kvm.Value);
                }
            }
        }

        void InstanceBuilding(Type type)
        {
            //ensure that the type is a subclass of Entity
            if (type.BaseType != typeof(Entity))
                return;

            //get rid of existing building if exists
            RemoveBuilding();

            //create entity
            _currentBuildingEntity = Game1.Scene.AddEntity(Activator.CreateInstance(type) as Entity);

            ////ensure that the entity has a building component
            //if (ent.TryGetComponent<Building>(out var building))
            //{
            //    //remove current building if exists
            //    RemoveBuilding();

            //    _currentBuilding = building;
            //    _currentBuilding.OnPlaced += OnBuildingPlaced;
            //    building.Pickup();
            //}
            //else
            //    ent.Destroy();
        }

        void RemoveBuilding()
        {
            _currentBuildingEntity?.Destroy();
            _currentBuildingEntity = null;
        }

        void OnBuildingPlaced(Building building)
        {
            if (building.Entity == _currentBuildingEntity)
            {
                _currentBuildingEntity = null;
                InstanceBuilding(_currentType);
            }
        }
    }
}
