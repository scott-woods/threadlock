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

        HashSet<IInteractable> _activeInteractables = new HashSet<IInteractable>();

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

            //init hashset of interactables
            var currentFrameInteractables = new HashSet<IInteractable>();

            //check for interactables on each hit
            foreach (var hit in hits)
            {
                if (hit.Collider != null)
                {
                    //handle each interactable
                    var interactables = hit.Collider.Entity.GetComponents<IInteractable>();
                    foreach (var interactable in interactables)
                    {
                        currentFrameInteractables.Add(interactable);
                        if (_activeInteractables.Add(interactable))
                        {
                            interactable.OnFocusEntered();
                        }
                    }
                }
            }

            //handle removing interactables we're no longer looking at
            var interactablesToRemove = new List<IInteractable>();
            foreach (var interactable in _activeInteractables)
            {
                if (!currentFrameInteractables.Contains(interactable))
                {
                    interactable.OnFocusExited();
                    interactablesToRemove.Add(interactable);
                }
            }

            //remove the interactables
            foreach (var interactable in interactablesToRemove)
            {
                _activeInteractables.Remove(interactable);
            }
        }

        public bool TryCheck()
        {
            var hits = GetRaycastHits();

            foreach (var hit in hits)
            {
                if (hit.Collider != null)
                {
                    var interactables = hit.Collider.GetComponents<IInteractable>();
                    if (interactables.Count > 0)
                    {
                        foreach (var interactable in interactables)
                            interactable.OnInteracted();

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
            Physics.LinecastAll(basePos, checkEnd, hits, 1 << PhysicsLayers.PromptTrigger);

            return hits;
        }
    }
}
