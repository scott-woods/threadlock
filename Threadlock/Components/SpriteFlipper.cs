using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;

namespace Threadlock.Components
{
    public class SpriteFlipper : Component, IUpdatable
    {
        public DirectionSource DirectionSource;

        bool _flipped = false;

        public void Update()
        {
            //TryFlip(DirectionSource);
        }

        public void SetFlip(bool flip)
        {
            //if flip status changed
            if (_flipped != flip)
            {
                _flipped = flip;

                var renderers = Entity.GetComponents<SpriteRenderer>();

                foreach (var renderer in renderers)
                {
                    renderer.FlipX = flip;

                    var newOffsetX = renderer.LocalOffset.X * -1;
                    var newOffset = new Vector2(newOffsetX, renderer.LocalOffset.Y);
                    renderer.SetLocalOffset(newOffset);
                }
            }
        }

        public void TryFlip(DirectionSource directionSource)
        {
            var flip = _flipped;
            switch (directionSource)
            {
                case DirectionSource.Velocity:
                    if (Entity.TryGetComponent<VelocityComponent>(out var velocityComponent))
                        flip = velocityComponent.LastNonZeroDirection.X < 0;
                    break;
                case DirectionSource.Aiming:
                    if (Entity is Player player)
                        flip = player.GetFacingDirection().X < 0;
                    else if (Entity is SimPlayer simPlayer)
                        flip = simPlayer.GetFacingDirection().X < 0;
                    else if (Entity is Enemy enemy)
                        flip = (EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity)).X < 0;
                    break;
            }

            SetFlip(flip);
        }
    }
}
