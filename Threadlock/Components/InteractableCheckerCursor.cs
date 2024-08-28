using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class InteractableCheckerCursor : Component, IUpdatable
    {
        Interactable _activeInteractable;

        public void Update()
        {
            //check for interactables on mouse position
            var collider = Physics.OverlapCircle(Game1.Scene.Camera.MouseToWorldPoint(), 1, 1 << PhysicsLayers.Interactable);

            //handle interactable focus
            if (collider != null)
            {
                //check for an interactable component
                if (collider.Entity.TryGetComponent<Interactable>(out var interactable))
                {
                    if (_activeInteractable != interactable)
                    {
                        _activeInteractable?.Unfocus();
                        _activeInteractable = interactable;
                        _activeInteractable.Focus();
                    }

                    //if clicked, interact
                    if (Controls.Instance.AltAttack.IsPressed)
                    {
                        _activeInteractable.Interact();
                    }
                }
            }
            else //handle interactable unfocus
            {
                _activeInteractable?.Unfocus();
                _activeInteractable = null;
            }
        }
    }
}
