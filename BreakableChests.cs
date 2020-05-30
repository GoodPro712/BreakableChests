using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace BreakableChests
{
	public class BreakableChests : Mod
	{
		public static FieldInfo globalItems = null;
		public static PropertyInfo modItem = null;

		public override void Load()
		{
			globalItems = typeof(Item).GetField("globalItems", BindingFlags.NonPublic | BindingFlags.Instance);
			modItem = typeof(Item).GetProperty("modItem", BindingFlags.Instance | BindingFlags.Public);

			IL.Terraria.Chest.CanDestroyChest += CanDestroy;
			IL.Terraria.Chest.DestroyChest += Destroy;
		}

		public override void Unload()
		{
			IL.Terraria.Chest.CanDestroyChest -= CanDestroy;
			IL.Terraria.Chest.DestroyChest -= Destroy;

			globalItems = null;
			modItem = null;
		}

		private void CanDestroy(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);

			c.EmitDelegate<Func<int, int, bool>>((x, y) =>
			{
				return !Chest.isLocked(x, y);
			});

			c.Emit(OpCodes.Ret);
		}

		private void Destroy(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);

			c.EmitDelegate<Func<int, int, bool>>((x, y) =>
			{
				for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
				{
					Chest chest = Main.chest[chestIndex];

					if (chest == null || chest.x != x || chest.y != y)
						continue;

					for (int itemIndex = 0; itemIndex < chest.item.Length; itemIndex++)
					{
						Item item = chest.item[itemIndex];

						if (item?.IsAir == false)
						{
							Dropitem(item, new Rectangle(x * 16, y * 16, 32, 32));
							item.TurnToAir();
						}
					}

					Main.chest[chestIndex] = null;

					Player player = Main.player[Main.myPlayer];

					if (player.chest == chestIndex)
						player.chest = -1;

					Recipe.FindRecipes();
				}

				return true;
			});

			c.Emit(OpCodes.Ret);
		}

		public static void Dropitem(Item item, Rectangle rect)
		{
			int num = Item.NewItem(rect.X, rect.Y, rect.Width, rect.Height, item.type, 1, false, 0, false, false);

			Main.item[num].netDefaults(item.netID);
			Main.item[num].Prefix(item.prefix);
			Main.item[num].stack = item.stack;
			Main.item[num].velocity.Y = Main.rand.Next(-20, 1) * 0.2f;
			Main.item[num].velocity.X = Main.rand.Next(-20, 21) * 0.2f;
			Main.item[num].noGrabDelay = 100;
			Main.item[num].newAndShiny = false;

			modItem.SetValue(Main.item[num], item.modItem);
			globalItems.SetValue(Main.item[num], globalItems.GetValue(item));
		}
	}
}