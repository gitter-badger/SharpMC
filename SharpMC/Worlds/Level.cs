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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using SharpMC.Blocks;
using SharpMC.Entity;
using SharpMC.Enums;
using SharpMC.Interfaces;
using SharpMC.Networking.Packages;
using SharpMC.Utils;

namespace SharpMC.Worlds
{
	public class Level
	{
		public Level()
		{
			CurrentWorldTime = 1200;
			Day = 1;
			OnlinePlayers = new List<Player>();
			DefaultGamemode = Gamemode.Creative;
			BlockWithTicks = new ConcurrentDictionary<Vector3, int>();
			StartTimeOfDayTimer();
			Entities = new List<Entity.Entity>();
			Dimension = 0;
		}

		internal int Dimension { get; set; }
		internal string LvlName { get; set; }
		internal int Difficulty { get; set; }
		internal Gamemode DefaultGamemode { get; set; }
		internal LVLType LevelType { get; set; }
		internal List<Player> OnlinePlayers { get; private set; }
		internal int CurrentWorldTime { get; set; }
		internal int Day { get; set; }
		public IWorldProvider Generator { get; set; }
		internal List<Entity.Entity> Entities { get; private set; }
		internal ConcurrentDictionary<Vector3, int> BlockWithTicks { get; private set; }

		#region APISpecific

		public Player[] GetOnlinePlayers
		{
			get { return OnlinePlayers.ToArray(); }
		}

		#endregion

		public void RemovePlayer(Player player)
		{
			lock (OnlinePlayers)
			{
				OnlinePlayers.Remove(player);
			}
		}

		public Player GetPlayer(int entityId)
		{
			foreach (var p in OnlinePlayers)
			{
				if (p.EntityId == entityId) return p;
			}
			return null;
		}

		public void AddPlayer(Player player)
		{
			OnlinePlayers.Add(player);

			new PlayerListItem(player.Wrapper)
			{
				Action = 0,
				Gamemode = player.Gamemode,
				Username = player.Username,
				UUID = player.Uuid
			}.Broadcast(this); //Send playerlist item to old players & player him self

			BroadcastExistingPlayers(player.Wrapper);
		}

		public void BroadcastChat(string message)
		{
			foreach (var i in OnlinePlayers)
			{
				new ChatMessage(i.Wrapper) {Message = @message}.Write();
			}
		}

		internal void BroadcastExistingPlayers(ClientWrapper caller)
		{
			foreach (var i in OnlinePlayers)
			{
				if (i.Wrapper != caller)
				{
					new PlayerListItem(caller)
					{
						Action = 0,
						Gamemode = i.Gamemode,
						Username = i.Username,
						UUID = i.Uuid
					}.Write(); //Send TAB Item
					new SpawnPlayer(caller) {Player = i}.Write(); //Spawn the old player to new player
					new SpawnPlayer(i.Wrapper) {Player = caller.Player}.Write(); //Spawn the new player to old player
					i.BroadcastEquipment();
				}
			}
		}

		internal void BroadcastPlayerRemoval(ClientWrapper caller)
		{
			new PlayerListItem(caller)
			{
				Action = 0,
				Gamemode = caller.Player.Gamemode,
				Username = caller.Player.Username,
				UUID = caller.Player.Uuid
			}.Broadcast(this);
		}

		public void SaveChunks()
		{
			Generator.SaveChunks(LvlName);
		}

		public Block GetBlock(Vector3 blockCoordinates)
		{
			var chunk = Generator.GenerateChunkColumn(new Vector2((int) blockCoordinates.X >> 4, (int) blockCoordinates.Z >> 4));

			var bid = chunk.GetBlock((int) blockCoordinates.X & 0x0f, (int) blockCoordinates.Y & 0x7f,
				(int) blockCoordinates.Z & 0x0f);

			var metadata = chunk.GetMetadata((int) blockCoordinates.X & 0x0f, (int) blockCoordinates.Y & 0x7f,
				(int) blockCoordinates.Z & 0x0f);

			//bid = (ushort) (bid >> 4);

			var block = BlockFactory.GetBlockById(bid, metadata);
			block.Coordinates = blockCoordinates;
			block.Metadata = metadata;

			return block;
		}

		public void SetBlock(Vector3 coordinates, Block block)
		{
			block.Coordinates = coordinates;
			SetBlock(block);
		}

		public void SetBlock(Block block, bool broadcast = true, bool applyPhysics = true)
		{
			var chunk =
				Generator.GenerateChunkColumn(new ChunkCoordinates((int) block.Coordinates.X >> 4, (int) block.Coordinates.Z >> 4));
			chunk.SetBlock((int) block.Coordinates.X & 0x0f, (int) block.Coordinates.Y & 0x7f, (int) block.Coordinates.Z & 0x0f,
				block);
			chunk.IsDirty = true;

			if (applyPhysics) ApplyPhysics((int) block.Coordinates.X, (int) block.Coordinates.Y, (int) block.Coordinates.Z);

			if (!broadcast) return;
			BlockChange.Broadcast(block, this);
		}

		internal void ApplyPhysics(int x, int y, int z)
		{
			DoPhysics(x - 1, y, z);
			DoPhysics(x + 1, y, z);
			DoPhysics(x, y - 1, z);
			DoPhysics(x, y + 1, z);
			DoPhysics(x, y, z - 1);
			DoPhysics(x, y, z + 1);
		}

		private void DoPhysics(int x, int y, int z)
		{
			var block = GetBlock(new Vector3(x, y, z));
			if (block is BlockAir) return;
			block.DoPhysics(this);
		}

		public void ScheduleBlockTick(Block block, int tickRate)
		{
			BlockWithTicks[block.Coordinates] = CurrentWorldTime + tickRate;
		}

		public void AddEntity(Entity.Entity entity)
		{
			Entities.Add(entity);
		}

		public void RemoveEntity(Entity.Entity entity)
		{
			if (Entities.Contains(entity)) Entities.Remove(entity);
		}

		public PlayerLocation GetSpawnPoint()
		{
			var spawn = Generator.GetSpawnPoint();
			return new PlayerLocation(spawn.X, spawn.Y, spawn.Z);
		}

		#region TickTimer

		private Task _gameTickThread;

		internal void StartTimeOfDayTimer()
		{
			_gameTickThread = new Task(StartTickTimer);
			_gameTickThread.Start();
		}


		private static readonly Timer KtTimer = new Timer();

		private void StartTickTimer()
		{
			KtTimer.Elapsed += GameTick;
			KtTimer.Interval = 50;
			KtTimer.Start();
		}

		private void DayTick()
		{
			if (CurrentWorldTime < 24000)
			{
				CurrentWorldTime += 1;
			}
			else
			{
				CurrentWorldTime = 0;
				Day++;
			}

			lock (OnlinePlayers)
			{
				foreach (var i in OnlinePlayers)
				{
					new TimeUpdate(i.Wrapper) {Time = CurrentWorldTime, Day = Day}.Write();
				}
			}
		}

		private readonly Stopwatch _sw = new Stopwatch();
		private long _lastCalc;

		public int CalculateTps(Player player = null)
		{
			var average = _lastCalc;

			var d = 1000 - _lastCalc;
			d = d/50;
			var exact = d;

			var color = "a";
			if (exact <= 10) color = "c";
			if (exact <= 15 && exact > 10) color = "e";


			if (player != null)
			{
				player.SendChat("TPS: \\u00A7" + color + exact);
				player.SendChat("Miliseconds in Tick: " + average + "ms");
			}

			return (int) exact;
		}

		private int _clockTick = 0;
		private int _saveTick;

		private void GameTick(object source, ElapsedEventArgs e)
		{
			_sw.Start();

			DayTick();

			foreach (var blockEvent in BlockWithTicks.ToArray())
			{
				if (blockEvent.Value <= CurrentWorldTime)
				{
					GetBlock(blockEvent.Key).OnTick(this);
					int value;
					BlockWithTicks.TryRemove(blockEvent.Key, out value);
				}
			}

			foreach (var player in OnlinePlayers.ToArray())
			{
				player.OnTick();
			}

			foreach (var entity in Entities.ToArray())
			{
				entity.OnTick();
			}

			if (_saveTick == 1500)
			{
				_saveTick = 0;
				ConsoleFunctions.WriteInfoLine("Saving chunks");
				var sw = new Stopwatch();
				sw.Start();
				SaveChunks();
				sw.Stop();
				ConsoleFunctions.WriteInfoLine("Saving chunks took: " + sw.ElapsedMilliseconds + "MS");

				ConsoleFunctions.WriteInfoLine("Clearing chunk cache...");
				Generator.ClearCache(); //Clear chunk cache
				GC.Collect(); //Collect garbage
			}
			else
			{
				_saveTick++;
			}

			if (_saveTick == 750) GC.Collect();

			_sw.Stop();
			_lastCalc = _sw.ElapsedMilliseconds;
			_sw.Reset();
		}

		#endregion
	}
}