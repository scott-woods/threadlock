using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.Models
{
    public class FloorTile
    {
        public Vector2 Position { get; set; }
        public TileDirection TileDirection { get; set; }
        public TileOrientation TileOrientation { get; set; }

        public FloorTile(Vector2 position, TileDirection tileDirection, TileOrientation tileOrientation)
        {
            Position = position;
            TileDirection = tileDirection;
            TileOrientation = tileOrientation;
        }

        public bool IsTileAdjacent(FloorTile nextTile)
        {
            if (TileOrientation != nextTile.TileOrientation)
                return false;

            if (TileOrientation == TileOrientation.TopEdge || TileOrientation == TileOrientation.BottomEdge)
            {
                if (Position.Y != nextTile.Position.Y)
                    return false;
                return Position.X + 16 == nextTile.Position.X;
            }
            else if (TileOrientation == TileOrientation.LeftEdge || TileOrientation == TileOrientation.RightEdge)
            {
                if (Position.X != nextTile.Position.X)
                    return false;
                return Position.Y + 16 == nextTile.Position.Y;
            }

            return false;
        }

        public static List<List<FloorTile>> GetTileSegments(List<FloorTile> tiles)
        {
            List<List<FloorTile>> segments = new List<List<FloorTile>>();
            var currentSegment = new List<FloorTile> { tiles.First() };
            for (int i = 1; i < tiles.Count; i++)
            {
                var currentTile = tiles[i - 1];
                var nextTile = tiles[i];

                if (currentSegment.Count < 8 && currentTile.IsTileAdjacent(nextTile))
                    currentSegment.Add(nextTile);
                else
                {
                    segments.Add(currentSegment);
                    currentSegment = new List<FloorTile> { nextTile };
                }
            }

            segments.Add(currentSegment);
            segments.RemoveAll(s => s.Count < 3);

            return segments;
        }
    }
}
