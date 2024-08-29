using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    /// <summary>
    /// Component to handle interactions with the player via cursor or linecast check. Has an Emitter for Focus, Unfocus, and Interact events.
    /// </summary>
    public class Interactable : Component
    {
        public Emitter<InteractableEvents> Emitter = new Emitter<InteractableEvents>();

        /// <summary>
        /// constructor with specific collider to interact with
        /// </summary>
        /// <param name="collider"></param>
        public Interactable(Collider collider)
        {
            //set interactable flag on collider
            Flags.SetFlag(ref collider.PhysicsLayer, PhysicsLayers.Interactable);
        }

        public void Interact()
        {
            Emitter.Emit(InteractableEvents.Interacted);
        }

        public void Focus()
        {
            Emitter.Emit(InteractableEvents.FocusEntered);
        }

        public void Unfocus()
        {
            Emitter.Emit(InteractableEvents.FocusExited);
        }
    }

    public enum InteractableEvents
    {
        FocusEntered,
        FocusExited,
        Interacted
    }
}
