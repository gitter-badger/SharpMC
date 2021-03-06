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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Ionic.Zlib;

namespace SharpMC.Utils
{
	public class MSGBuffer
	{
		private readonly ClientWrapper _client;
		public byte[] BufferedData = new byte[4096];
		private int LastByte;
		public int Size = 0;

		public MSGBuffer(ClientWrapper client)
		{
			_client = client;
		}

		public MSGBuffer(byte[] data)
		{
			BufferedData = data;
		}

		public void Dispose()
		{
			BufferedData = null;
			LastByte = 0;
		}

		#region Reader

		public int ReadByte()
		{
			var returnData = BufferedData[LastByte];
			LastByte++;
			return returnData;
		}

		public byte[] Read(int Length)
		{
			var Buffered = new byte[Length];
			Array.Copy(BufferedData, LastByte, Buffered, 0, Length);
			LastByte += Length;
			return Buffered;
		}


		public int ReadInt()
		{
			var Dat = Read(4);
			var Value = BitConverter.ToInt32(Dat, 0);
			return IPAddress.NetworkToHostOrder(Value);
		}

		public float ReadFloat()
		{
			var Almost = Read(4);
			var f = BitConverter.ToSingle(Almost, 0);
			return NetworkToHostOrder(f);
		}

		public bool ReadBool()
		{
			var Answer = ReadByte();
			if (Answer == 1)
				return true;
			return false;
		}

		public double ReadDouble()
		{
			var AlmostValue = Read(8);
			return NetworkToHostOrder(AlmostValue);
		}

		public int ReadVarInt()
		{
			var value = 0;
			var size = 0;
			int b;
			while (((b = ReadByte()) & 0x80) == 0x80)
			{
				value |= (b & 0x7F) << (size++*7);
				if (size > 5)
				{
					throw new IOException("VarInt too long. Hehe that's punny.");
				}
			}
			return value | ((b & 0x7F) << (size*7));
		}

		public long ReadVarLong()
		{
			var value = 0;
			var size = 0;
			int b;
			while (((b = ReadByte()) & 0x80) == 0x80)
			{
				value |= (b & 0x7F) << (size++*7);
				if (size > 10)
				{
					throw new IOException("VarLong too long. That's what she said.");
				}
			}
			return value | ((b & 0x7F) << (size*7));
		}

		public short ReadShort()
		{
			var Da = Read(2);
			var D = BitConverter.ToInt16(Da, 0);
			return IPAddress.NetworkToHostOrder(D);
		}

		public ushort ReadUShort()
		{
			var Da = Read(2);
			return NetworkToHostOrder(BitConverter.ToUInt16(Da, 0));
		}

		public ushort[] ReadUShort(int count)
		{
			var us = new ushort[count];
			for (var i = 0; i < us.Length; i++)
			{
				var Da = Read(2);
				var D = BitConverter.ToUInt16(Da, 0);
				us[i] = D;
			}
			return NetworkToHostOrder(us);
			//return IPAddress.NetworkToHostOrder (D);
		}

		public ushort[] ReadUShortLocal(int count)
		{
			var us = new ushort[count];
			for (var i = 0; i < us.Length; i++)
			{
				var Da = Read(2);
				var D = BitConverter.ToUInt16(Da, 0);
				us[i] = D;
			}
			return us;
			//return IPAddress.NetworkToHostOrder (D);
		}

		public short[] ReadShortLocal(int count)
		{
			var us = new short[count];
			for (var i = 0; i < us.Length; i++)
			{
				var Da = Read(2);
				var D = BitConverter.ToInt16(Da, 0);
				us[i] = D;
			}
			return us;
			//return IPAddress.NetworkToHostOrder (D);
		}

		public string ReadString()
		{
			var Length = ReadVarInt();
			var StringValue = Read(Length);
			return Encoding.UTF8.GetString(StringValue);
		}

		public long ReadLong()
		{
			var l = Read(8);
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(l, 0));
		}

		public Vector3 ReadPosition()
		{
			var val = ReadLong();
			var x = Convert.ToDouble(val >> 38);
			var y = Convert.ToDouble((val >> 26) & 0xFFF);
			var z = Convert.ToDouble(val << 38 >> 38);
			return new Vector3(x, y, z);
		}

		private double NetworkToHostOrder(byte[] data)
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(data);
			}
			return BitConverter.ToDouble(data, 0);
		}

		private float NetworkToHostOrder(float network)
		{
			var bytes = BitConverter.GetBytes(network);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);

			return BitConverter.ToSingle(bytes, 0);
		}

		private ushort[] NetworkToHostOrder(ushort[] network)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(network);
			return network;
		}

		private ushort NetworkToHostOrder(ushort network)
		{
			var net = BitConverter.GetBytes(network);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(net);
			return BitConverter.ToUInt16(net, 0);
		}

		#endregion

		#region Writer

		public byte[] ExportWriter
		{
			get { return bffr.ToArray(); }
		}

		private readonly List<byte> bffr = new List<byte>();

		public void Write(byte[] Data, int Offset, int Length)
		{
			for (var i = 0; i < Length; i++)
			{
				bffr.Add(Data[i + Offset]);
			}
		}

		public void Write(byte[] Data)
		{
			foreach (var i in Data)
			{
				bffr.Add(i);
			}
		}

		public void WritePosition(Vector3 position)
		{
			var x = Convert.ToInt64(position.X);
			var y = Convert.ToInt64(position.Y);
			var z = Convert.ToInt64(position.Z);
			var toSend = ((x & 0x3FFFFFF) << 38) | ((y & 0xFFF) << 26) | (z & 0x3FFFFFF);
			WriteLong(toSend);
		}

		public void WriteVarInt(int Integer)
		{
			while ((Integer & -128) != 0)
			{
				bffr.Add((byte) (Integer & 127 | 128));
				Integer = (int) (((uint) Integer) >> 7);
			}
			bffr.Add((byte) Integer);
		}

		public void WriteVarLong(long i)
		{
			var Fuck = i;
			while ((Fuck & ~0x7F) != 0)
			{
				bffr.Add((byte) ((Fuck & 0x7F) | 0x80));
				Fuck >>= 7;
			}
			bffr.Add((byte) Fuck);
		}

		public void WriteInt(int Data)
		{
			var Buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Data));
			Write(Buffer);
		}

		public void WriteString(string Data)
		{
			var StringData = Encoding.UTF8.GetBytes(Data);
			WriteVarInt(StringData.Length);
			Write(StringData);
		}

		public void WriteShort(short Data)
		{
			var ShortData = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Data));
			Write(ShortData);
		}

		public void WriteUShort(ushort Data)
		{
			var UShortData = BitConverter.GetBytes(Data);
			Write(UShortData);
		}

		public void WriteByte(byte Data)
		{
			bffr.Add(Data);
		}

		public void WriteBool(bool Data)
		{
			Write(BitConverter.GetBytes(Data));
		}

		public void WriteDouble(double Data)
		{
			Write(HostToNetworkOrder(Data));
		}

		public void WriteFloat(float Data)
		{
			Write(HostToNetworkOrder(Data));
		}

		public void WriteLong(long Data)
		{
			Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Data)));
		}

		public void WriteUUID(Guid uuid)
		{
			var guid = uuid.ToByteArray();
			var Long1 = new byte[8];
			var Long2 = new byte[8];
			Array.Copy(guid, 0, Long1, 0, 8);
			Array.Copy(guid, 8, Long2, 0, 8);
			Write(Long1);
			Write(Long2);
		}

		/// <summary>
		///     Flush all data to the TCPClient NetworkStream.
		/// </summary>
		public void FlushData(bool quee = false)
		{
			try
			{
				var AllData = bffr.ToArray();
				bffr.Clear();

				if (Globals.UseCompression && _client.PacketMode == PacketMode.Play)
				{
					var mg = new MSGBuffer(_client); //ToWriteAllData
					var compressed = ZlibStream.CompressBuffer(AllData);

					mg.WriteVarInt(compressed.Length);
					mg.WriteVarInt(compressed.Length);

					mg.Write(compressed);
					_client.AddToQuee(mg.ExportWriter, quee);
				}
				else
				{
					WriteVarInt(AllData.Length);
					var Buffer = bffr.ToArray();

					var data = new List<byte>();
					foreach (var i in Buffer)
					{
						data.Add(i);
					}
					foreach (var i in AllData)
					{
						data.Add(i);
					}
					_client.AddToQuee(data.ToArray(), quee);
				}
				bffr.Clear();
			}
			catch (Exception ex)
			{
				ConsoleFunctions.WriteErrorLine("Failed to send a packet!\n" + ex);
			}
		}

		private byte[] HostToNetworkOrder(double d)
		{
			var data = BitConverter.GetBytes(d);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);

			return data;
		}

		private byte[] HostToNetworkOrder(float host)
		{
			var bytes = BitConverter.GetBytes(host);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);

			return bytes;
		}

		#endregion
	}
}