using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    /// <summary>
    /// model that represents a partition of the overall dungeon
    /// </summary>
    public class DungeonLeaf
    {
        const int _minSize = 35;

        public DungeonLeaf LeftChild;
        public DungeonLeaf RightChild;
        public DungeonRoom Room;

        public Vector2 Position;
        public Vector2 Size;

        public int Generation;

        public DungeonLeaf(int generation, Vector2 position, Vector2 size)
        {
            Generation = generation;
            Position = position;
            Size = size;
        }

        public DungeonLeaf(int generation, int x, int y, int width, int height)
        {
            Generation = generation;
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }

        /// <summary>
        /// returns true if the split was successful
        /// </summary>
        /// <returns></returns>
        public bool Split()
        {
            //if we've already split, don't do anything
            if (LeftChild != null || RightChild != null)
                return false;

            //if width is 25% larger, split vertically. if height is 25% larger, split horizontally
            //if neither applies, split randomly
            var splitH = Nez.Random.Chance(.5f);
            if (Size.X > Size.Y && Size.X / Size.Y >= 1.25f)
                splitH = false;
            else if (Size.Y > Size.X && Size.Y / Size.X >= 1.25f)
                splitH = true;

            //determine if area is too small to split
            var max = splitH ? Size.Y : Size.X;
            max -= _minSize;

            //adjust min and max to be in factors of 5
            int adjustedMin = (_minSize + 4) / 5;
            int adjustedMax = (int)max / 5;
            if (adjustedMax <= adjustedMin)
                return false;

            var split = Nez.Random.Range(adjustedMin, adjustedMax + 1) * 5;

            //create children from the split
            if (splitH)
            {
                LeftChild = new DungeonLeaf(Generation + 1, Position, new Vector2(Size.X, split));
                RightChild = new DungeonLeaf(Generation + 1, new Vector2(Position.X, Position.Y + split), new Vector2(Size.X, Size.Y - split));
            }
            else
            {
                LeftChild = new DungeonLeaf(Generation + 1, Position, new Vector2(split, Size.Y));
                RightChild = new DungeonLeaf(Generation + 1, new Vector2(Position.X + split, Position.Y), new Vector2(Size.X - split, Size.Y));
            }

            return true;
        }

        public void GetRooms(ref List<DungeonRoom> rooms)
        {
            if (Room != null)
            {
                rooms.Add(Room);
                return;
            }
            else
            {
                LeftChild?.GetRooms(ref rooms);
                RightChild?.GetRooms(ref rooms);
            }
        }

        //public DungeonRoom GetRoom()
        //{
        //    if (Room != null)
        //        return Room;
        //    else
        //    {
        //        DungeonRoom leftRoom = null;
        //        DungeonRoom rightRoom = null;
        //        if (LeftChild != null)
        //            leftRoom = LeftChild.GetRoom();
        //        if (RightChild != null)
        //            rightRoom = RightChild.GetRoom();

        //        if (leftRoom == null && rightRoom == null)
        //            return null;
        //        else if (rightRoom == null)
        //            return leftRoom;
        //        else if (leftRoom == null)
        //            return rightRoom;
        //        else if (Nez.Random.Chance(.5f))
        //            return leftRoom;
        //        else
        //            return rightRoom;
        //    }
        //}
    }
}
