using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using Threadlock.Models;
using Threadlock.SaveData;
using Threadlock.SceneComponents;

namespace Threadlock.Components
{
    public class Building : Component, IUpdatable
    {
        //events
        public event Action<Vector2> OnPlaced;
        public event Action<BuildingOrientation> OnOrientationChanged;

        public string Name;
        public string Description;
        public Dictionary<string, int> Cost;

        public Vector2 GridSize;
        public Vector2 GridPosition { get => (Entity.Position / 16) - new Vector2(GridSize.X / 2, GridSize.Y / 2); }
        
        public BuildingOrientation Orientation
        {
            get => _orientation;
            set
            {
                var oldOrientation = _orientation;
                _orientation = value;

                //call event if orientation changed
                if (oldOrientation != _orientation)
                    OnOrientationChanged?.Invoke(_orientation);
            }
        }
        BuildingOrientation _orientation = BuildingOrientation.Down;

        bool _isPlaced;
        bool _initialPlacementFinished;

        //components
        SpriteRenderer _renderer;
        Collider _collider;

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _renderer = Entity.GetComponent<SpriteRenderer>();
            _collider = Entity.GetComponent<Collider>();

            //only pickup if we're not already registered with the grid
            var factoryGrid = Entity.Scene.GetSceneComponent<FactoryGrid>();
            if (factoryGrid != null && !factoryGrid.Buildings.Contains(this))
            {
                _initialPlacementFinished = false;
                Pickup();
            }
            else
            {
                _isPlaced = true;
                _initialPlacementFinished = true;
            }
        }

        #endregion

        #region IUPDATABLE

        public void Update()
        {
            if (!_isPlaced)
            {
                //handle rotating
                if (Input.MouseWheelDelta < 0)
                {
                    //rotate left
                    var nextOrientation = (int)_orientation - 1;
                    if (nextOrientation < 0)
                        nextOrientation = Enum.GetValues(typeof(BuildingOrientation)).Length - 1;
                    Orientation = (BuildingOrientation)nextOrientation;
                }
                else if (Input.MouseWheelDelta > 0)
                {
                    //rotate right
                    Orientation = (BuildingOrientation)Math.Max(0, ((int)Orientation + 1) % Enum.GetValues(typeof(BuildingOrientation)).Length);
                }

                //get mouse pos
                var mousePos = Game1.Scene.Camera.MouseToWorldPoint();

                var topLeft = mousePos - new Vector2(GridSize.X * 8, GridSize.Y * 8);

                var x = Mathf.FastFloorToInt(topLeft.X / 16f);
                var y = Mathf.FastFloorToInt(topLeft.Y / 16f);

                var snappedGridPos = new Vector2(x, y);
                var snappedWorldPos = snappedGridPos * 16;

                //set entity position
                Entity.SetPosition(snappedWorldPos + new Vector2(GridSize.X * 8, GridSize.Y * 8));

                //get bounds
                var bounds = new Rectangle(x, y, Mathf.CeilToInt(GridSize.X), Mathf.CeilToInt(GridSize.Y));

                //validate placement
                var canPlace = CanBePlaced(bounds);

                //handle placement
                if (canPlace)
                {
                    _renderer.SetColor(new Color(Color.White.R, Color.White.G, Color.White.B, 128));
                    if (Controls.Instance.Melee.IsPressed)
                        Place(bounds);
                }
                else
                    _renderer.SetColor(new Color(Color.Red.R, Color.Red.G, Color.Red.B, 128));
            }
        }

        #endregion

        public void Pickup()
        {
            _isPlaced = false;
            _collider.SetEnabled(false);

            //unregister from grid
            var factoryGrid = Game1.Scene.GetSceneComponent<FactoryGrid>();
            var existingBuilding = factoryGrid.GetBuilding(GridPosition);
            if (existingBuilding == this)
                factoryGrid.UnregisterBuilding(existingBuilding);

            _renderer.SetColor(new Color(Color.White.R, Color.White.G, Color.White.B, 128));
        }

        void Place(Rectangle bounds)
        {
            _isPlaced = true;
            _collider.SetEnabled(true);
            Physics.UpdateCollider(_collider);
            _renderer.SetColor(Color.White);

            var factoryGrid = Game1.Scene.GetSceneComponent<FactoryGrid>();
            factoryGrid.RegisterBuilding(this, bounds);

            //handle cost
            if (!_initialPlacementFinished)
            {
                _initialPlacementFinished = true;
                foreach (var kvp in Cost)
                    PlayerData.Instance.Resources[kvp.Key] -= kvp.Value;
            }

            OnPlaced?.Invoke(Entity.Position);
        }

        /// <summary>
        /// checks that position and cost requirements are met
        /// </summary>
        /// <returns></returns>
        bool CanBePlaced(Rectangle bounds)
        {
            //get factory grid
            var factoryGrid = Game1.Scene.GetSceneComponent<FactoryGrid>();

            //validate position
            
            if (!factoryGrid.CanPlaceBuilding(bounds))
                return false;

            //validate cost
            if (Cost != null && !_initialPlacementFinished)
            {
                foreach (var kvp in Cost)
                {
                    var resourceName = kvp.Key;
                    var resourceQuantity = kvp.Value;

                    if (!PlayerData.Instance.Resources.TryGetValue(resourceName, out var playerResourceQuantity) || playerResourceQuantity < resourceQuantity)
                        return false;
                }
            }

            return true;
        }
    }

    public enum BuildingOrientation
    {
        Down,
        Left,
        Up,
        Right
    }
}
