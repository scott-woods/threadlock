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

        public SingleTileRenderer(TmxLayerTile tile)
        {
            _tile = tile;        }

        public SingleTileRenderer(Texture2D tilesetTexture, Rectangle sourceRect)
        {
            _tilesetTexture = tilesetTexture;
            _sourceRect = sourceRect;
        }

        public SingleTileRenderer(TmxTilesetTile tilesetTile)
        {
            _tilesetTile = tilesetTile;
            _sourceRect = _tilesetTile.Tileset.TileRegions[_tilesetTile.Id];
        }

        public SingleTileRenderer(TmxTileset tileset, int tileId, int renderLayer)
        {
            _tilesetTexture = tileset.Image.Texture;
            _sourceRect = tileset.TileRegions[tileId];
            RenderLayer = renderLayer;
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
