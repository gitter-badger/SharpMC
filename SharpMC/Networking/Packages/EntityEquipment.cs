﻿// Distrubuted under the MIT license
// ===================================================
// SharpMC uses the permissive MIT license.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the “Software”), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software
// 
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ©Copyright Kenny van Vulpen - 2015

using SharpMC.Utils;

namespace SharpMC.Networking.Packages
{
	public enum EquipmentSlot
	{
		Held = 0,
		Boots = 1,
		Leggings = 2,
		Chestplate = 3,
		Helmet = 4
	}

	public class EntityEquipment : Package<EntityEquipment>
	{
		public int EntityId = 0;
		public ItemStack Item;
		public EquipmentSlot Slot = EquipmentSlot.Held;

		public EntityEquipment(ClientWrapper client) : base(client)
		{
			SendId = 0x04;
		}

		public EntityEquipment(ClientWrapper client, MSGBuffer buffer) : base(client, buffer)
		{
			SendId = 0x04;
		}

		public override void Write()
		{
			if (Buffer != null)
			{
				Buffer.WriteVarInt(SendId);
				Buffer.WriteVarInt(EntityId);
				Buffer.WriteShort((short) Slot);
				Buffer.WriteShort(Item.ItemId);
				if (Item.ItemId != -1)
				{
					Buffer.WriteByte(1);
					Buffer.WriteShort(Item.MetaData);
					Buffer.WriteByte(0);
				}
				Buffer.FlushData();
			}
		}
	}
}