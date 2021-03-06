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
	internal class PlayerPositionAndLook : Package<PlayerPositionAndLook>
	{
		public int X = (int) Globals.LevelManager.MainLevel.Generator.GetSpawnPoint().X;
		public int Y = (int) Globals.LevelManager.MainLevel.Generator.GetSpawnPoint().Y;
		public int Z = (int) Globals.LevelManager.MainLevel.Generator.GetSpawnPoint().Z;

		public PlayerPositionAndLook(ClientWrapper client) : base(client)
		{
			SendId = 0x08;
			ReadId = 0x06;
		}

		public PlayerPositionAndLook(ClientWrapper client, MSGBuffer buffer) : base(client, buffer)
		{
			SendId = 0x08;
			ReadId = 0x06;
		}

		public override void Write()
		{
			if (Buffer != null)
			{
				Buffer.WriteVarInt(SendId);
				Buffer.WriteDouble(X);
				Buffer.WriteDouble(Y);
				Buffer.WriteDouble(Z);
				Buffer.WriteFloat(0f);
				Buffer.WriteFloat(0f);
				Buffer.WriteByte(111);
				Buffer.FlushData();
			}
		}

		public override void Read()
		{
			if (Buffer != null)
			{
				var X = Buffer.ReadDouble();
				var FeetY = Buffer.ReadDouble();
				var Z = Buffer.ReadDouble();
				var Yaw = Buffer.ReadFloat();
				var Pitch = Buffer.ReadFloat();
				var OnGround = Buffer.ReadBool();

				//Client.Player.KnownPosition.OnGround = OnGround;
				//Client.Player.KnownPosition.Yaw = Yaw;
				//Client.Player.KnownPosition.Pitch = Pitch;
				//Client.Player.KnownPosition = new PlayerLocation(X, FeetY, Z);

				//var movement = Client.Player.KnownPosition - originalCoordinates;
				//new EntityRelativeMove(Client) {Player = Client.Player, Movement = movement}.Broadcast(false, Client.Player);
				Client.Player.PositionChanged(new Vector3(X, FeetY, Z), Yaw, Pitch, OnGround);
			}
		}
	}
}