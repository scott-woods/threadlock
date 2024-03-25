using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class HealthOrb : Entity
    {
        Pickup _pickup;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _pickup = AddComponent(new Pickup(Nez.Content.Textures.Drops.Health_orb, Nez.Content.Audio.Sounds.Burp, false, OnCollision));

            Scale = new Vector2(.5f, .5f);
        }

        void OnCollision()
        {
            if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
                hc.Health += (int)(hc.MaxHealth * .1f);
            Destroy();
        }
    }
}
