using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;
using Threadlock.UI;
using static Nez.Content.Textures;

namespace Threadlock.Components
{
    public class Building : Component, IUpdatable
    {
        bool _isPlaced;
        bool _isMenuOpen;

        string _name;

        SpriteRenderer _renderer;
        Collider _collider;
        Interactable _interactable;

        BuildingMenu _menu;

        public Building(string name)
        {
            _name = name;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _renderer = Entity.GetComponent<SpriteRenderer>();
            _collider = Entity.GetComponent<Collider>();

            _interactable = Entity.GetComponent<Interactable>();
            if (_interactable != null)
            {
                _interactable.Emitter.AddObserver(InteractableEvents.Interacted, OnInteracted);
            }
        }

        public override void OnRemovedFromEntity() 
        {
            base.OnRemovedFromEntity();

            _interactable?.Emitter.RemoveObserver(InteractableEvents.Interacted, OnInteracted);
        }

        #endregion

        #region IUPDATABLE

        public void Update()
        {
            var mousePos = Game1.Scene.Camera.MouseToWorldPoint();

            if (!_isPlaced)
            {
                var x = Mathf.FastFloorToInt(mousePos.X / 16f) * 16f;
                var y = Mathf.FastFloorToInt(mousePos.Y / 16f) * 16f;
                Entity.SetPosition(x + (_renderer.Width / 2), y + (_renderer.Height / 2));
            }
        }

        #endregion

        #region OBSERVERS

        void OnInteracted()
        {
            var canvas = Entity.Scene.FindComponentOfType<UICanvas>();
            canvas?.AddComponent(new BuildingMenu(_name));
        }

        #endregion

        public void Pickup()
        {
            _isPlaced = false;
            _collider.SetEnabled(false);
        }

        public bool Place()
        {
            var buildings = Entity.Scene.FindComponentsOfType<Building>();
            foreach (var building in buildings)
            {
                if (building == this)
                    continue;

                if (building.Entity.TryGetComponent<Collider>(out var collider))
                {
                    if (collider.Overlaps(_collider))
                        return false;
                }
            }

            _isPlaced = true;
            _collider.SetEnabled(true);

            return true;
        }
    }
}
