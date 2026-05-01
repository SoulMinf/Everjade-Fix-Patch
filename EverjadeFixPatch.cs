using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace EverjadeFixPatch
{
	public class EverjadeFixPatch : Mod
	{

    }

    public class FurnitureDropFixSystem : ModSystem
    {
        private static readonly HashSet<string> _excludedTypes = new()
        {
            // "JadeFables.Tiles.SomeTileWithTileEntity",
            "JadeFables.Tiles.JadePylon.JadePylonTile",
            "JadeFables.Tiles.WarriorStatue.WarriorStatue"
        };

        private static readonly List<IDisposable> _hooks = new();

        public override void Load()
        {
            if (!ModLoader.TryGetMod("JadeFables", out Mod jadeFables))
            return;

            Assembly asm = jadeFables.GetType().Assembly;
            int patched = 0;

            foreach (Type type in asm.GetTypes())
            {
                if (!typeof(ModTile).IsAssignableFrom(type) || type.IsAbstract)
                continue;

                if (_excludedTypes.Contains(type.FullName ?? string.Empty))
                continue;

                var method = type.GetMethod(
                nameof(ModTile.KillMultiTile),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                new[] { typeof(int), typeof(int), typeof(int), typeof(int) },
                null);

                if (method is null)
                continue;

                _hooks.Add(new Hook(method,
                (Action<ModTile, int, int, int, int> orig,
                ModTile self, int i, int j, int frameX, int frameY) => { }));

                patched++;
            }
        }

        public override void Unload()
        {
            foreach (IDisposable hook in _hooks)
                hook.Dispose();

            _hooks.Clear();
        }
    }

    public class JadeOreDropFix : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            var tile = TileLoader.GetTile(type);

            if (tile?.Mod?.Name == "JadeFables" && tile.Name == "JadeOre")
            {
                noItem = false;
            }
        }

        public override void Drop(int i, int j, int type)
        {
            var tile = TileLoader.GetTile(type);

            if (tile?.Mod?.Name == "JadeFables" && tile.Name == "JadeOre")
            {
                Mod jadeMod = ModLoader.GetMod("JadeFables");
                if (jadeMod == null)
                    return;

                int itemType = jadeMod.Find<ModItem>("JadeChunk").Type;

                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), new Microsoft.Xna.Framework.Vector2(i * 16, j * 16), itemType, 1);
            }
        }
    }
}
