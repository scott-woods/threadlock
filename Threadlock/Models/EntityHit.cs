using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class EntityHit
    {
        public Entity HitEntity { get; set; }
        public Entity AttackOwner { get; set; }
        public int Damage { get; set; }

        public EntityHit(Entity hitEntity, Entity attackOwner, int damage)
        {
            HitEntity = hitEntity;
            AttackOwner = attackOwner;
            Damage = damage;
        }
    }
}
