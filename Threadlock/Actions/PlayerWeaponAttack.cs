using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Actions
{
    public class PlayerWeaponAttack : BasicAction, ICloneable
    {
        public float? ComboInputDelay;
        public float? ComboStartTime;

        #region BASIC ACTION

        protected override TargetingInfo GetTargetingInfo()
        {
            var dc = Context.GetComponent<DirectionComponent>();
            var dir = dc != null ? dc.GetCurrentDirection() : Vector2.Zero;

            return new TargetingInfo()
            {
                Direction = dir,
            };
        }

        #endregion

        #region ICLONEABLE

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}
