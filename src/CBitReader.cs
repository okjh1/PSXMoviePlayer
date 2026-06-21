/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System;

namespace HLPS1Str
{
	// Continuous word-le bitstream for macroblocks specifically
	public class CBitReader
	{
		private readonly byte[] Buffer;
		private uint BytePos;
		public ulong BitBuffer;
		private long BitBufferSize;

		public CBitReader(byte[] buffer)
		{
			Buffer = buffer;
		}

		private void UpdateBitsIfNecessary(int size)
		{
			long remaining = Buffer.Length - BytePos;
			while (BitBufferSize < size) {
				ushort word = 0;
				if (remaining > 1) {
					word = (ushort)(Buffer[BytePos] | (Buffer[BytePos + 1] << 8));
				} else if (remaining == 1) {
					word = (ushort)Buffer[BytePos];
				}
				BitBuffer = (BitBuffer << 16) | (ulong)word;
				BytePos += 2;
				BitBufferSize += 16;
			}
		}

		public uint PeekBits(int size)
		{
			if (size < 1) {
				throw new Exception("Trying to peek while not actually trying to");
			}
			UpdateBitsIfNecessary(size);
			return (uint)((uint)(BitBuffer >> (int)(BitBufferSize - size)) & ((1 << size) - 1));
		}

		public void IgnoreBits(int size)
		{
			if (size < 1) {
				throw new Exception("Size must be greater than or equal to 1");
			}
			UpdateBitsIfNecessary(size);
			BitBufferSize -= size;
		}

		public uint ReadBits(int size)
		{
			uint value = PeekBits(size);
			IgnoreBits(size);
			return value;
		}

		public int ReadSignedBits(int size)
		{
			return Utils.SignExtend(ReadBits(size), size);
		}
	}
}
