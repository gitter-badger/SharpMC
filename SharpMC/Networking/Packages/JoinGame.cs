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

using SharpMC.Entity;
using SharpMC.Utils;

namespace SharpMC.Networking.Packages
{
	internal class JoinGame : Package<JoinGame>
	{
		public Player Player;

		public JoinGame(ClientWrapper client) : base(client)
		{
			SendId = 0x01;
		}

		public JoinGame(ClientWrapper client, MSGBuffer buffer) : base(client, buffer)
		{
			SendId = 0x01;
		}

		public override void Write()
		{
			if (Buffer != null)
			{
				Buffer.WriteVarInt(SendId);
				Buffer.WriteInt(Player.EntityId);
				Buffer.WriteByte((byte) Player.Gamemode);
				Buffer.WriteByte(Player.Dimension);
				Buffer.WriteByte((byte) Client.Player.Level.Difficulty);
				Buffer.WriteByte((byte) Globals.MaxPlayers);
				Buffer.WriteString(Client.Player.Level.LevelType.ToString());
				Buffer.WriteBool(false);
				Buffer.FlushData();
			}
		}
	}
}