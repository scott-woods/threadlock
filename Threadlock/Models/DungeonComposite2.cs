using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;

namespace Threadlock.Models
{
    public class DungeonComposite2
    {
        public List<DungeonRoom> DungeonRooms = new List<DungeonRoom>();

        public List<DungeonRoom> GetRoomsFromChildrenComposites()
        {
            var allRooms = new List<DungeonRoom>();
            AddAllChildrenRecursive(allRooms);
            return allRooms;
        }

        void AddAllChildrenRecursive(List<DungeonRoom> allRooms)
        {
            allRooms.AddRange(DungeonRooms);
            foreach (var room in DungeonRooms.Where(r => r.ChildrenOutsideComposite != null && r.ChildrenOutsideComposite.Count > 0))
            {
                foreach (var child in room.ChildrenOutsideComposite)
                {
                    child.ParentComposite.AddAllChildrenRecursive(allRooms);
                }
            }
        }
    }
}
