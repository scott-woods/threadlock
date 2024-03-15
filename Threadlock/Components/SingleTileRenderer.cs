using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Nez.Content.Tiled;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class SingleTileRenderer : RenderableComponent
    {
        public override float Width => _tile != null ? _tile.Tileset.TileWidth : 16;
        public override float Height => _tile != null ? _tile.Tileset.TileHeight : 16;

        TmxLayerTile _tile;
        TmxTilesetTile _tilesetTile;
        Texture2D _tilesetTexture;
        Rectangle _sourceRect;
        bool _isWall = false;
        public int TileId;

        public SingleTileRenderer(TmxTileset tileset, int tileId, int renderLayer, bool isWall = false)
        {
            _tilesetTexture = tileset.Image.Texture;
            _sourceRect = tileset.TileRegions[tileId];
            RenderLayer = renderLayer;
            _isWall = isWall;
            TileId = tileId;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_isWall)
            {
                //var collider = Entity.AddComponent(new BoxCollider(_sourceRect.Width, _sourceRect.Height));
                //collider.SetLocalOffset(new Vector2(_sourceRect.Width / 2, _sourceRect.Height / 2));
                //Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.Environment);
            }
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            //batcher.Draw(_tile.Image.Texture, Entity.Position, new Rectangle((int)Entity.Position.X, (int)Entity.Position.Y, _tile.Tileset.TileWidth, _tile.Tileset.TileHeight), Color.White, 0, Vector2.Zero, Entity.Scale, SpriteEffects.None, LayerDepth);

            if (_tile != null)
                TiledRendering.RenderTile(_tile, batcher, Entity.Position, Entity.Scale, _tile.Tileset.TileWidth,
                    _tile.Tileset.TileHeight, Color.White, 1, OrientationType.Orthogonal,
                    _tile.Tileset.TileWidth, _tile.Tileset.TileHeight);
            else if (_tilesetTile != null)
            {
                batcher.Draw(_tilesetTile.Image.Texture, Entity.Position, _sourceRect, Color.White, 0, Vector2.Zero, Entity.Scale, SpriteEffects.None, LayerDepth);
            }
            else if (_tilesetTexture != null)
            {
                batcher.Draw(_tilesetTexture, Entity.Position, _sourceRect, Color.White, 0, Vector2.Zero, Entity.Scale, SpriteEffects.None, LayerDepth);
            }
        }
    }
}
