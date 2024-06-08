using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;

namespace Threadlock.Components.EnemyActions
{
    public class MeleeAttack : EnemyAction2
    {
        

        #region Enemy Action implementation

        public override void Abort(Enemy enemy)
        {

        }

        protected override IEnumerator Execute(Enemy enemy)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
