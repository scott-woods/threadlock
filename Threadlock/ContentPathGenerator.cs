

namespace Nez
{
    /// <summary>
    /// class that contains the names of all of the files processed by the Pipeline Tool
    /// </summary>
    /// <remarks>
    /// Nez includes a T4 template that will auto-generate the content of this file.
    /// See: https://github.com/prime31/Nez/blob/master/FAQs/ContentManagement.md#auto-generating-content-paths"
    /// </remarks>
    class Content
    {
		public static class CompiledEffects
		{
			public const string JitterDestroyer = @"Content\CompiledEffects\JitterDestroyer.fxb";
		}

		public static class Effects
		{
			public const string JitterDestroyer = @"Content\Effects\JitterDestroyer.fx";
		}

		public static class Textures
		{
			public static class Characters
			{
				public static class Player
				{
					public const string Gun1 = @"Content\Textures\Characters\Player\gun1.png";
					public const string Player_gun_projectile = @"Content\Textures\Characters\Player\player_gun_projectile.png";
					public const string Sci_fi_player_no_sword = @"Content\Textures\Characters\Player\sci_fi_player_no_sword.png";
					public const string Sci_fi_player_with_sword = @"Content\Textures\Characters\Player\sci_fi_player_with_sword.png";
				}

			}

			public static class Tilesets
			{
				public const string Dungeon_prison_props = @"Content\Textures\Tilesets\dungeon_prison_props.png";
				public const string Dungeon_prison_tileset = @"Content\Textures\Tilesets\dungeon_prison_tileset.png";
			}

		}

		public static class Tiled
		{
			public static class Tilemaps
			{
				public const string Test = @"Content\Tiled\Tilemaps\test.tmx";
			}

			public static class Tilesets
			{
				public const string Dungeon_prison = @"Content\Tiled\Tilesets\dungeon_prison.tsx";
			}

			public const string Threadlock = @"Content\Tiled\threadlock.tiled-project";
		}


    }
}

