using Microsoft.Xna.Framework;
using Nez;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    /// <summary>
    /// used to select SelectionComponents
    /// </summary>
    public class EntitySelector : Component, IUpdatable
    {
        public Color HighlightColor = Color.Yellow;

        SelectionComponent _selection;
        public SelectionComponent Selection
        {
            get => _selection;
            private set
            {
                if (_selection != null)
                    _selection.Unhighlight();

                _selection = value;
                _selection?.Highlight(HighlightColor);
            }
        }

        CircleCollider _collider;

        public EntitySelector(params int[] collidesWithLayers)
        {
            //create circle collider
            _collider = new CircleCollider(5f);
            _collider.IsTrigger = true;
            _collider.ShouldColliderScaleAndRotateWithTransform = false;

            //set physics layer
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.Selector);

            //set collides with layers
            _collider.CollidesWithLayers = 0;
            foreach (var layer in collidesWithLayers)
                Flags.SetFlag(ref _collider.CollidesWithLayers, layer);
        }

        public void Update()
        {
            if (_collider.CollidesWithAny(out var result))
            {
                if (result.Collider.Entity.TryGetComponent<SelectionComponent>(out var selectionComponent))
                {
                    if (Selection == null)
                    {
                        Selection = selectionComponent;
                        return;
                    }
                    else if (Vector2.Distance(Entity.Position, selectionComponent.Entity.Position) < Vector2.Distance(Entity.Position, Selection.Entity.Position))
                    {
                        Selection = selectionComponent;
                        return;
                    }
                }
            }
            else
            {
                Selection = null;
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            Entity.AddComponent(_collider);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            Selection = null;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            Selection = null;
        }
    }
}
