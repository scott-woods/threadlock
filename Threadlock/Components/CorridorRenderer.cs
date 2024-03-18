using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class CorridorRenderer : RenderableComponent
    {
        public override float Width
        {
            get
            {
                if (_tileDictionary.Any())
                {
                    var minX = _tileDictionary.Keys.Select(t => t.X).Min();
                    var maxX = _tileDictionary.Keys.Select(t => t.X).Max();
                    var tileWidth = _tileset != null ? _tileset.TileWidth : 16;

                    return maxX - minX + tileWidth;
                }

                return 0;
            }
        }
        public override float Height
        {
            get
            {
                if (_tileDictionary.Any())
                {
                    var minY = _tileDictionary.Keys.Select(t => t.Y).Min();
                    var maxY = _tileDictionary.Keys.Select(t => t.Y).Max();
                    var tileHeight = _tileset != null ? _tileset.TileHeight : 16;

                    return maxY - minY + tileHeight;
                }

                return 0;
            }
        }

        public int PhysicsLayer = PhysicsLayers.Environment;

        Dictionary<Vector2, SingleTile> _tileDictionary = new Dictionary<Vector2, SingleTile>();
        Dictionary<Vector2, SingleTile> _collisionTiles { get => _tileDictionary.Where(t => t.Value.IsCollider).ToDictionary(t => t.Key, t => t.Value); }
        List<Rectangle> _tileRects { get => _tileDictionary.Keys.Select(k => new Rectangle((int)k.X, (int)k.Y, _tileset.TileWidth, _tileset.TileHeight)).ToList(); }
        TmxTileset _tileset;
        public TmxTileset Tileset { get => _tileset; }
        bool _shouldAddColliders = false;
        List<BoxCollider> _colliders;

        public CorridorRenderer(TmxTileset tileset, Dictionary<Vector2, SingleTile> tileDictionary, bool addColliders = false)
        {
            _tileDictionary = tileDictionary;
            _tileset = tileset;
            _shouldAddColliders = addColliders;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_tileDictionary.Any())
            {
                var minX = _tileDictionary.Select(t => t.Key.X).Min();
                var minY = _tileDictionary.Select(t => t.Key.Y).Min();
                Entity.SetPosition(minX, minY);
            }

            if (_shouldAddColliders)
                AddColliders();
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (_colliders == null)
                return;

            foreach (var collider in _colliders)
                Physics.RemoveCollider(collider);
            _colliders = null;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            var culledTileDictionary = GetCulledTiles(camera.Bounds);

            foreach (var tile in culledTileDictionary)
            {
                batcher.Draw(_tileset.Image.Texture, tile.Key, _tileset.TileRegions[tile.Value.TileId], Color.White, 0, Vector2.Zero, Entity.Scale, SpriteEffects.None, LayerDepth);
            }
        }

        void AddColliders()
        {
            var collisionRects = GetCollisionRectangles();

            for (var i = 0; i < collisionRects.Count; i++)
            {
                var collider = Entity.AddComponent(new BoxCollider(collisionRects[i].Width, collisionRects[i].Height));
                collider.SetLocalOffset(collisionRects[i].Location.ToVector2() + new Vector2(collider.Width / 2, collider.Height / 2));
                Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.Environment);

                if (_colliders == null)
                    _colliders = new List<BoxCollider>();

                _colliders.Add(collider);
                Physics.AddCollider(collider);
                //var collider = new BoxCollider(collisionRects[i].X + _localOffset.X,
                //    collisionRects[i].Y + _localOffset.Y, collisionRects[i].Width, collisionRects[i].Height);
                //collider.PhysicsLayer = PhysicsLayer;
                //collider.Entity = Entity;

                //Physics.AddCollider(collider);
            }
        }

        Dictionary<Vector2, SingleTile> GetCulledTiles(RectangleF cameraClipBounds)
        {
            var padding = 4;
            Point min, max;
            min.X = (int)cameraClipBounds.Left - (_tileset.TileWidth * padding);
            min.Y = (int)cameraClipBounds.Top - (_tileset.TileHeight * padding);
            max.X = (int)cameraClipBounds.Right + (_tileset.TileWidth * padding);
            max.Y = (int)cameraClipBounds.Bottom + (_tileset.TileHeight * padding);

            return _tileDictionary.Where(t =>
            {
                return t.Key.X >= min.X && t.Key.X <= max.X && t.Key.Y >= min.Y && t.Key.Y <= max.Y;
            }).ToDictionary(t => t.Key, t => t.Value);
        }

        List<Rectangle> GetCollisionRectangles()
        {
            var checkedIndexes = new bool?[((int)Width / _tileset.TileWidth) * ((int)Height / _tileset.TileWidth)];
            var rectangles = new List<Rectangle>();
            var startCol = -1;
            var index = -1;

            for (var y = 0; y < Height / _tileset.TileHeight; y++)
            {
                for (var x = 0; x < Width / _tileset.TileWidth; x++)
                {
                    index = y * ((int)Width / _tileset.TileWidth) + x;
                    var isTilePresent = _collisionTiles.ContainsKey(Entity.Position + new Vector2(x * _tileset.TileWidth, y * _tileset.TileHeight));

                    if (isTilePresent && (checkedIndexes[index] == false || checkedIndexes[index] == null))
                    {
                        if (startCol < 0)
                            startCol = x;

                        checkedIndexes[index] = true;
                    }
                    else if (!isTilePresent || checkedIndexes[index] == true)
                    {
                        if (startCol >= 0)
                        {
                            rectangles.Add(FindBoundsRect(startCol, x, y, checkedIndexes));
                            startCol = -1;
                        }
                    }
                } // end for x

                if (startCol >= 0)
                {
                    rectangles.Add(FindBoundsRect(startCol, ((int)Width / _tileset.TileWidth), y, checkedIndexes));
                    startCol = -1;
                }
            }

            return rectangles;
        }

        public Rectangle FindBoundsRect(int startX, int endX, int startY, bool?[] checkedIndexes)
        {
            var index = -1;

            for (var y = startY + 1; y < Height / 16; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    index = y * ((int)Width / 16) + x;
                    var isTilePresent = _collisionTiles.ContainsKey(Entity.Position + new Vector2(x * _tileset.TileWidth, y * _tileset.TileHeight));

                    if (!isTilePresent || checkedIndexes[index] == true)
                    {
                        // Set everything we've visited so far in this row to false again because it won't be included in the rectangle and should be checked again
                        for (var _x = startX; _x < x; _x++)
                        {
                            index = y * ((int)Width / _tileset.TileHeight) + _x;
                            checkedIndexes[index] = false;
                        }

                        return new Rectangle((startX * _tileset.TileWidth), (startY * _tileset.TileHeight),
                            (endX - startX) * _tileset.TileWidth, (y - startY) * _tileset.TileHeight);
                    }

                    checkedIndexes[index] = true;
                }
            }

            return new Rectangle((startX * _tileset.TileWidth), (startY * _tileset.TileHeight),
                (endX - startX) * _tileset.TileWidth, (((int)Height / _tileset.TileHeight) - startY) * _tileset.TileHeight);
        }

        public List<Vector2> GetTilesIntersectingBounds(Rectangle bounds)
        {
            var result = new List<Vector2>();

            var tiles = _tileRects.Where(t => t.Intersects(bounds));
            if (tiles.Any())
                result = tiles.Select(t => t.Location.ToVector2()).ToList();

            return result;
        }
    }
}
