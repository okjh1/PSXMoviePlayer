/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System;
using System.IO;

namespace HLPS1Str
{
	// todo: bit reader here?
	public class MultiBinaryReader: BinaryReader
	{
		public MultiBinaryReader(Stream stream): base(stream)
		{}

		public static MemoryStream CreatePublicMemoryStream(byte[] bytes, int size)
		{
			return new MemoryStream(bytes, 0, size, false, true);
		}

		public byte[] ReadReversedBytes(int size)
		{
			byte[] bytes = ReadBytes(size);
			Array.Reverse(bytes);
			return bytes;
		}

		public int ReadInt32(bool little_endian = true)
		{
			if (BitConverter.IsLittleEndian && little_endian) {
				return base.ReadInt32();
			} else {
				return BitConverter.ToInt32(ReadReversedBytes(4), 0);
			}
		}

		public uint ReadUInt32(bool little_endian = true)
		{
			if (BitConverter.IsLittleEndian && little_endian) {
				return base.ReadUInt32();
			} else {
				return BitConverter.ToUInt32(ReadReversedBytes(4), 0);
			}
		}

		public short ReadInt16(bool little_endian = true)
		{
			if (BitConverter.IsLittleEndian && little_endian) {
				return base.ReadInt16();
			} else {
				return BitConverter.ToInt16(ReadReversedBytes(2), 0);
			}
		}

		public ushort ReadUInt16(bool little_endian = true)
		{
			if (BitConverter.IsLittleEndian && little_endian) {
				return base.ReadUInt16();
			} else {
				return BitConverter.ToUInt16(ReadReversedBytes(2), 0);
			}
		}

		public void IgnoreBytes(long value)
		{
			BaseStream.Position += value;
		}

		public void Regress(long bytes)
		{
			BaseStream.Position -= bytes;
		}

		public int GetNumOfUnreadBytes()
		{
			return (int)(base.BaseStream.Length - base.BaseStream.Position);
		}

		public byte[] ReadRemaining()
		{
			return base.ReadBytes(GetNumOfUnreadBytes());
		}
	}
}
