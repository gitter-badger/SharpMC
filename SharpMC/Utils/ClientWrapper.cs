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

using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using SharpMC.Entity;
using SharpMC.Networking.Packages;
using Timer = System.Timers.Timer;

namespace SharpMC.Utils
{
	public enum PacketMode
	{
		Ping,
		Login,
		Play
	}

	public class ClientWrapper
	{
		private readonly Queue<byte[]> _commands = new Queue<byte[]>();
		private readonly Timer _kTimer = new Timer();
		private readonly AutoResetEvent _resume = new AutoResetEvent(false);
		private readonly Timer _tickTimer = new Timer();
		internal bool EncryptionEnabled = false;
		//public bool PlayMode = false;
		public PacketMode PacketMode = PacketMode.Ping;
		public Player Player;
		public TcpClient TcpClient;
		public MyThreadPool ThreadPool;

		public ClientWrapper(TcpClient client)
		{
			TcpClient = client;
			ThreadPool = new MyThreadPool();
			ThreadPool.LaunchThread(ThreadRun);

			var bytes = new byte[8];
			Globals.Rand.NextBytes(bytes);
			ConnectionId = Encoding.ASCII.GetString(bytes).Replace("-", "");
		}

		internal byte[] SharedKey { get; set; }
		internal ICryptoTransform Encrypter { get; set; }
		internal ICryptoTransform Decrypter { get; set; }
		internal string ConnectionId { get; set; }
		internal string Username { get; set; }

		public void AddToQuee(byte[] data, bool quee = false)
		{
			if (quee)
			{
				lock (_commands)
				{
					_commands.Enqueue(data);
				}
				_resume.Set();
			}
			else
			{
				SendData(data);
			}
		}

		private void ThreadRun()
		{
			while (_resume.WaitOne())
			{
				byte[] command;
				lock (_commands)
				{
					command = _commands.Dequeue();
				}
				SendData(command);
			}
		}

		public void SendData(byte[] data)
		{
			try
			{
				if (Encrypter != null)
				{
					var toEncrypt = data;
					data = new byte[toEncrypt.Length];
					Encrypter.TransformBlock(toEncrypt, 0, toEncrypt.Length, data, 0);

					var a = TcpClient.GetStream();
					a.Write(data, 0, data.Length);
					a.Flush();
				}
				/*	if (EncryptionEnabled)
				{
					AesStream aes = new AesStream(TcpClient.GetStream(), (byte[])SharedKey.Clone());
					aes.Write(data, 0, data.Length);
					aes.Flush();
				}*/
				else
				{
					var a = TcpClient.GetStream();
					a.Write(data, 0, data.Length);
					a.Flush();
				}
			}
			catch
			{
				ConsoleFunctions.WriteErrorLine("Failed to send a packet!");
			}
		}

		public void StartKeepAliveTimer()
		{
			_kTimer.Elapsed += DisplayTimeEvent;
			_kTimer.Interval = 5000;
			_kTimer.Start();

			_tickTimer.Elapsed += DoTick;
			_tickTimer.Interval = 50;
			//_tickTimer.Start();
		}

		public void StopKeepAliveTimer()
		{
			_kTimer.Stop();
		}

		public void DisplayTimeEvent(object source, ElapsedEventArgs e)
		{
			new KeepAlive(Player.Wrapper).Write();
		}

		public void DoTick(object source, ElapsedEventArgs e)
		{
			//if (Player != null)
			//{
			//	Player.OnTick();
			//}
		}
	}
}