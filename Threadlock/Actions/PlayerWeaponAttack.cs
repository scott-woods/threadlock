using Nez;
using Nez.Persistence;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
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
            var player = Context as Player;

            return new TargetingInfo()
            {
                Direction = player.GetFacingDirection(),
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
