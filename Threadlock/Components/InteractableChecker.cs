using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class InteractableChecker : Component, IUpdatable
    {
        public float CheckDistance = 20f;

        OriginComponent _originComponent;
        DirectionComponent _directionComponent;

        Interactable _activeInteractable;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _originComponent = Entity.GetComponent<OriginComponent>();
            _directionComponent = Entity.GetComponent<DirectionComponent>();
        }

        public void Update()
        {
            //get raycast hits
            var hits = GetRaycastHits();

            //init current interactable
            Interactable currInteractable = null;

            //check for interactables on each hit
            foreach (var hit in hits)
            {
                if (hit.Collider != null)
                {
                    if (hit.Collider.Entity.TryGetComponent<Interactable>(out var interactable))
                    {
                        currInteractable = interactable;
                        break;
                        if (_activeInteractable != interactable)
                        {
                            _activeInteractable?.Unfocus();
                            _activeInteractable = interactable;
                            _activeInteractable.Focus();
                            break;
                        }
                    }
                }
            }

            //handle focus
            if (_activeInteractable != currInteractable)
            {
                _activeInteractable?.Unfocus();
                _activeInteractable = currInteractable;
                _activeInteractable?.Focus();
            }
        }

        public bool TryCheck()
        {
            var hits = GetRaycastHits();

            foreach (var hit in hits)
            {
                if (hit.Collider != null)
                {
                    if (hit.Collider.Entity.TryGetComponent<Interactable>(out var interactable))
                    {
                        interactable.Interact();
                        return true;
                    }
                }
            }

            return false;
        }

        RaycastHit[] GetRaycastHits()
        {
            var basePos = _originComponent.Origin;
            var dir = _directionComponent.GetCurrentDirection();
            var checkEnd = basePos + (dir * CheckDistance);

            var hits = new RaycastHit[10];
            Physics.LinecastAll(basePos, checkEnd, hits, 1 << PhysicsLayers.Interactable);

            return hits;
        }
    }
}
