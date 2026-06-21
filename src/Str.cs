/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace HLPS1Str
{
	public class StrFrameSectorHeader
	{
		public static uint VIDEO_FRAME_BITFLAGS = 0x80010160;
		public static ushort MAGIC = 0x3800;
		public static uint UNKNOWN = 0x00000000;

		public uint Bitflags;
		public ushort MuxedFrameChunkNumber,
				NumOfMuxedFrameChunks;
		public uint FrameNumber,
				DemuxedFrameBytesOfData;
		public ushort FrameWidth,
				FrameHeight,
				NumOfMDECCodes,
				Magic,
				FrameQuantizationScale,
				FrameVersion;
		public uint Unknown;

		public static void Decode(ref StrFrameSectorHeader self, MultiBinaryReader reader)
		{
			self.Bitflags = reader.ReadUInt32();
			self.MuxedFrameChunkNumber = reader.ReadUInt16();
			self.NumOfMuxedFrameChunks = reader.ReadUInt16();
			self.FrameNumber = reader.ReadUInt32();
			// any fractional number is discarded in c# if LHS and RHS are not of the float type
			self.DemuxedFrameBytesOfData = ((reader.ReadUInt32() + 3) / 4) * 4;
			self.FrameWidth = reader.ReadUInt16();
			self.FrameHeight = reader.ReadUInt16();
			self.NumOfMDECCodes = (ushort)((((reader.ReadUInt16() + 3) / 64) * 64) / 2);
			self.Magic = reader.ReadUInt16();
			self.FrameQuantizationScale = reader.ReadUInt16();
			self.FrameVersion = reader.ReadUInt16();
			self.Unknown = reader.ReadUInt32();
		}
	}

	public class StrFrameDataHeader
	{
		public static ushort MAGIC = 0x3800;

		public ushort NumOfUncompressedMDECBlks,
					Magic,
					FrameQuantizationScale,
					Version;

		public static void Decode(ref StrFrameDataHeader self, MultiBinaryReader reader)
		{
			self.NumOfUncompressedMDECBlks = reader.ReadUInt16();
			self.Magic = reader.ReadUInt16();
			self.FrameQuantizationScale = reader.ReadUInt16();
			self.Version = reader.ReadUInt16();
		}
	};

	public class AC
	{
		public class RLC
		{
			public string Code;
			public int Run;
			public int Level;
		};

		public static RLC[] RunLengthCodes = {
			new RLC { Code = "11", Run = 0, Level = 1 },
			new RLC { Code = "011", Run = 1, Level = 1 },
			new RLC { Code = "0100", Run = 0, Level = 2 },
			new RLC { Code = "0101", Run = 2, Level = 1 },
			new RLC { Code = "00101", Run = 0, Level = 3 },
			new RLC { Code = "00110", Run = 4, Level = 1 },
			new RLC { Code = "00111", Run = 3, Level = 1 },
			new RLC { Code = "000100", Run = 7, Level = 1 },
			new RLC { Code = "000101", Run = 6, Level = 1 },
			new RLC { Code = "000110", Run = 1, Level = 2 },
			new RLC { Code = "000111", Run = 5, Level = 1 },
			new RLC { Code = "0000100", Run = 2, Level = 2 },
			new RLC { Code = "0000101", Run = 9, Level = 1 },
			new RLC { Code = "0000110", Run = 0, Level = 4 },
			new RLC { Code = "0000111", Run = 8, Level = 1 },
			new RLC { Code = "00100000", Run = 13, Level = 1 },
			new RLC { Code = "00100001", Run = 0, Level = 6 },
			new RLC { Code = "00100010", Run = 12, Level = 1 },
			new RLC { Code = "00100011", Run = 11, Level = 1 },
			new RLC { Code = "00100100", Run = 3, Level = 2 },
			new RLC { Code = "00100101", Run = 1, Level = 3 },
			new RLC { Code = "00100110", Run = 0, Level = 5 },
			new RLC { Code = "00100111", Run = 10, Level = 1 },
			new RLC { Code = "0000001000", Run = 16, Level = 1 },
			new RLC { Code = "0000001001", Run = 5, Level = 2 },
			new RLC { Code = "0000001010", Run = 0, Level = 7 },
			new RLC { Code = "0000001011", Run = 2, Level = 3 },
			new RLC { Code = "0000001100", Run = 1, Level = 4 },
			new RLC { Code = "0000001101", Run = 15, Level = 1 },
			new RLC { Code = "0000001110", Run = 14, Level = 1 },
			new RLC { Code = "0000001111", Run = 4, Level = 2 },
			new RLC { Code = "000000010000", Run = 0, Level = 11 },
			new RLC { Code = "000000010001", Run = 8, Level = 2 },
			new RLC { Code = "000000010010", Run = 4, Level = 3 },
			new RLC { Code = "000000010011", Run = 0, Level = 10 },
			new RLC { Code = "000000010100", Run = 2, Level = 4 },
			new RLC { Code = "000000010101", Run = 7, Level = 2 },
			new RLC { Code = "000000010110", Run = 21, Level = 1 },
			new RLC { Code = "000000010111", Run = 20, Level = 1 },
			new RLC { Code = "000000011000", Run = 0, Level = 9 },
			new RLC { Code = "000000011001", Run = 19, Level = 1 },
			new RLC { Code = "000000011010", Run = 18, Level = 1 },
			new RLC { Code = "000000011011", Run = 1, Level = 5 },
			new RLC { Code = "000000011100", Run = 3, Level = 3 },
			new RLC { Code = "000000011101", Run = 0, Level = 8 },
			new RLC { Code = "000000011110", Run = 6, Level = 2 },
			new RLC { Code = "000000011111", Run = 17, Level = 1 },
			new RLC { Code = "0000000010000", Run = 10, Level = 2 },
			new RLC { Code = "0000000010001", Run = 9, Level = 2 },
			new RLC { Code = "0000000010010", Run = 5, Level = 3 },
			new RLC { Code = "0000000010011", Run = 3, Level = 4 },
			new RLC { Code = "0000000010100", Run = 2, Level = 5 },
			new RLC { Code = "0000000010101", Run = 1, Level = 7 },
			new RLC { Code = "0000000010110", Run = 1, Level = 6 },
			new RLC { Code = "0000000010111", Run = 0, Level = 15 },
			new RLC { Code = "0000000011000", Run = 0, Level = 14 },
			new RLC { Code = "0000000011001", Run = 0, Level = 13 },
			new RLC { Code = "0000000011010", Run = 0, Level = 12 },
			new RLC { Code = "0000000011011", Run = 26, Level = 1 },
			new RLC { Code = "0000000011100", Run = 25, Level = 1 },
			new RLC { Code = "0000000011101", Run = 24, Level = 1 },
			new RLC { Code = "0000000011110", Run = 23, Level = 1 },
			new RLC { Code = "0000000011111", Run = 22, Level = 1 },
			new RLC { Code = "00000000010000", Run = 0, Level = 31 },
			new RLC { Code = "00000000010001", Run = 0, Level = 30 },
			new RLC { Code = "00000000010010", Run = 0, Level = 29 },
			new RLC { Code = "00000000010011", Run = 0, Level = 28 },
			new RLC { Code = "00000000010100", Run = 0, Level = 27 },
			new RLC { Code = "00000000010101", Run = 0, Level = 26 },
			new RLC { Code = "00000000010110", Run = 0, Level = 25 },
			new RLC { Code = "00000000010111", Run = 0, Level = 24 },
			new RLC { Code = "00000000011000", Run = 0, Level = 23 },
			new RLC { Code = "00000000011001", Run = 0, Level = 22 },
			new RLC { Code = "00000000011010", Run = 0, Level = 21 },
			new RLC { Code = "00000000011011", Run = 0, Level = 20 },
			new RLC { Code = "00000000011100", Run = 0, Level = 19 },
			new RLC { Code = "00000000011101", Run = 0, Level = 18 },
			new RLC { Code = "00000000011110", Run = 0, Level = 17 },
			new RLC { Code = "00000000011111", Run = 0, Level = 16 },
			new RLC { Code = "000000000010000", Run = 0, Level = 40 },
			new RLC { Code = "000000000010001", Run = 0, Level = 39 },
			new RLC { Code = "000000000010010", Run = 0, Level = 38 },
			new RLC { Code = "000000000010011", Run = 0, Level = 37 },
			new RLC { Code = "000000000010100", Run = 0, Level = 36 },
			new RLC { Code = "000000000010101", Run = 0, Level = 35 },
			new RLC { Code = "000000000010110", Run = 0, Level = 34 },
			new RLC { Code = "000000000010111", Run = 0, Level = 33 },
			new RLC { Code = "000000000011000", Run = 0, Level = 32 },
			new RLC { Code = "000000000011001", Run = 1, Level = 14 },
			new RLC { Code = "000000000011010", Run = 1, Level = 13 },
			new RLC { Code = "000000000011011", Run = 1, Level = 12 },
			new RLC { Code = "000000000011100", Run = 1, Level = 11 },
			new RLC { Code = "000000000011101", Run = 1, Level = 10 },
			new RLC { Code = "000000000011110", Run = 1, Level = 9 },
			new RLC { Code = "000000000011111", Run = 1, Level = 8 },
			new RLC { Code = "0000000000010000", Run = 1, Level = 18 },
			new RLC { Code = "0000000000010001", Run = 1, Level = 17 },
			new RLC { Code = "0000000000010010", Run = 1, Level = 16 },
			new RLC { Code = "0000000000010011", Run = 1, Level = 15 },
			new RLC { Code = "0000000000010100", Run = 6, Level = 3 },
			new RLC { Code = "0000000000010101", Run = 16, Level = 2 },
			new RLC { Code = "0000000000010110", Run = 15, Level = 2 },
			new RLC { Code = "0000000000010111", Run = 14, Level = 2 },
			new RLC { Code = "0000000000011000", Run = 13, Level = 2 },
			new RLC { Code = "0000000000011001", Run = 12, Level = 2 },
			new RLC { Code = "0000000000011010", Run = 11, Level = 2 },
			new RLC { Code = "0000000000011011", Run = 31, Level = 1 },
			new RLC { Code = "0000000000011100", Run = 30, Level = 1 },
			new RLC { Code = "0000000000011101", Run = 29, Level = 1 },
			new RLC { Code = "0000000000011110", Run = 28, Level = 1 },
			new RLC { Code = "0000000000011111", Run = 27, Level = 1 }
		};

		public static Dictionary<uint, RLC> RLC_LUT = null;
		// public static Dictionary<int, int> RLC_COMB_TO_KEY_MAP = null;

		public static RLC ESCAPE_CODE = new RLC { Code = "000001" };
		public static RLC END_OF_BLOCK = new RLC { Code = "10" };

		public static RLC V2_END_OF_FRAME = new RLC { Code = "0111111111" };
		public static RLC V3_END_OF_FRAME = new RLC { Code = "1111111111" };

		public static void Init()
		{
			if (RLC_LUT != null/* && RLC_COMB_TO_KEY_MAP != null*/) {
				return;
			}
			RLC_LUT = new Dictionary<uint, RLC>();
			// RLC_COMB_TO_KEY_MAP = new Dictionary<int, int>();
			// It is possible and probably way easier to just put the ESCAPE_CODE and END_OF_BLOCK inside the lut.
			foreach (RLC rlc in RunLengthCodes.Skip(1)) {
				uint base10_code = Convert.ToUInt32(rlc.Code, 2) << (16 - rlc.Code.Length);
				uint number_of_combinations = 1u << (16 - rlc.Code.Length);
				RLC_LUT[base10_code] = rlc;
				for (uint i = 0; i < number_of_combinations; ++i) {
					RLC_LUT[base10_code + i] = rlc;
				}
			}
		}

		// EOB: End of Block
		// EC: Escape Code, 6 bits for the run and 10 bits for the level
		public static (RLC zrlc, bool EC, bool EOB) Lookup(uint vlc)
		{
			if (vlc > ushort.MaxValue) {
				throw new Exception("VLC given in AC.Lookup is more than the bits that should be able to hold it.");
			}
			RLC rlc;
			if ((vlc & 0b1000000000000000) != 0) {
				if ((vlc & 0b0100000000000000) != 0) {
					rlc = RunLengthCodes[0];
				} else {
					// giving the zrlc for those is unnecessary
					return (null, false, true);
				}
			} else {
				rlc = RLC_LUT.TryGetValue(vlc, out RLC out_rlc) ? out_rlc : null;
			}
			if (rlc == null && (vlc & 0b0000010000000000) == 1024) {
				return (null, true, false);
			}
			return (rlc, false, false);
		}
	};

	public class MacroBlockYCbCr
	{
		public double Y,
			Cb,
			Cr;
	};

	public class MacroBlockRGB
	{
		public byte R,
				G,
				B;
	};

	public class Str
	{
		private readonly MDECEmulator MDECEmulator;

		private MacroBlockYCbCr[,] MacroBlock_YCbCrs;
		private MacroBlockRGB[,] MacroBlock_RGBs;

		public Str()
		{
			AC.Init();

			MDECEmulator = new MDECEmulator();

			MacroBlock_YCbCrs = new MacroBlockYCbCr[16, 16];
			MacroBlock_RGBs = new MacroBlockRGB[16, 16];
			for (int x = 0; x < 16; ++x) {
				for (int y = 0; y < 16; ++y) {
					MacroBlock_YCbCrs[x, y] = new MacroBlockYCbCr();
					MacroBlock_RGBs[x, y] = new MacroBlockRGB();
				}
			}
		}

		public unsafe Bitmap ProcessNextFrame(Demuxer.VideoPacket packet)
		{
			// todo: mdec emulator should do its stuff in a new thread i think if possible
			MemoryStream mdec_stream = new MemoryStream();
			BinaryWriter mdec_writer = new BinaryWriter(mdec_stream);

			StrFrameDataHeader data_header = new StrFrameDataHeader();
			CBitReader cmacro_block_bit_reader;

			using (MultiBinaryReader payload_reader = new MultiBinaryReader(new MemoryStream(packet.Payload))) {
				StrFrameDataHeader.Decode(ref data_header, payload_reader);
				if (data_header.Magic != StrFrameDataHeader.MAGIC) {
					throw new Exception("Invalid frame data header magic.");
				}
				cmacro_block_bit_reader = new CBitReader(payload_reader.ReadRemaining());
			}

			Bitmap video_frame_image = new Bitmap((int)packet.Width, (int)packet.Height, PixelFormat.Format32bppRgb);
			BitmapData video_frame_data = video_frame_image.LockBits(new Rectangle(0, 0, video_frame_image.Width, video_frame_image.Height), ImageLockMode.WriteOnly, video_frame_image.PixelFormat);
			int *video_frame_data_ptr = (int *)video_frame_data.Scan0;
			int stride_in_pixels = video_frame_data.Stride / 4;

			for (int mbx = 0; mbx < packet.GetMBWidth(); ++mbx) {
				for (int mby = 0; mby < packet.GetMBHeight(); ++mby) {
					double [,,] blocks = new double[6, 8, 8];
					for (int blk = 0; blk < 6; ++blk) {
						uint dc_coef;
						if (data_header.Version == 2) {
							dc_coef = cmacro_block_bit_reader.ReadBits(10);
						} else {
							throw new Exception("For now only version 2 frames is supported.");
						}
						uint ac_vlc = cmacro_block_bit_reader.PeekBits(16);

						mdec_writer.Write((ushort)((ushort)((data_header.FrameQuantizationScale & 0x3f) << 10) | (dc_coef & 0x3ff)));
						// limit by 0xff
						for (int limit = 0; limit < 0xff; ++limit) {
							(AC.RLC rlc, bool EC, bool EOB) = AC.Lookup(ac_vlc);
							int rlc_run;
							int rlc_level;
							if (EOB) {
								cmacro_block_bit_reader.IgnoreBits(AC.END_OF_BLOCK.Code.Length);
								mdec_writer.Write(MDECEmulator.END_OF_DATA);
								break;
							} else if (EC) {
								cmacro_block_bit_reader.IgnoreBits(AC.ESCAPE_CODE.Code.Length);
								rlc_run = (int)cmacro_block_bit_reader.ReadBits(6);
								rlc_level = cmacro_block_bit_reader.ReadSignedBits(10);
							} else {
								if (rlc == null) {
									throw new Exception("Failed ac coefficient lookup: " + ac_vlc);
								}
								cmacro_block_bit_reader.IgnoreBits(rlc.Code.Length);
								rlc_run = rlc.Run;
								uint sign_bit = cmacro_block_bit_reader.ReadBits(1);
								if (sign_bit == 0) {
									rlc_level = rlc.Level;
								} else {
									rlc_level = -rlc.Level;
								}
							}

							mdec_writer.Write((ushort)(((rlc_run & 0x3f) << 10) | (rlc_level & 0x3ff)));

							ac_vlc = cmacro_block_bit_reader.PeekBits(16);
						}

						MDECEmulator.DecodeBlock(mdec_stream.GetBuffer(), blk, ref blocks);
						mdec_stream.SetLength(0);
					}

					// im aware the below stuff can be simplified
					for (int x = 0; x < 8; ++x) {
						for (int y = 0; y < 8; ++y) {
							MacroBlock_YCbCrs[x,     y    ].Y = blocks[2, y, x] + 128;
							MacroBlock_YCbCrs[x + 8, y    ].Y = blocks[3, y, x] + 128;
							MacroBlock_YCbCrs[x,     y + 8].Y = blocks[4, y, x] + 128;
							MacroBlock_YCbCrs[x + 8, y + 8].Y = blocks[5, y, x] + 128;

							MacroBlock_YCbCrs[x * 2    , y * 2    ].Cb = blocks[1, y, x];
							MacroBlock_YCbCrs[x * 2 + 1, y * 2    ].Cb = blocks[1, y, x];
							MacroBlock_YCbCrs[x * 2    , y * 2 + 1].Cb = blocks[1, y, x];
							MacroBlock_YCbCrs[x * 2 + 1, y * 2 + 1].Cb = blocks[1, y, x];

							MacroBlock_YCbCrs[x * 2    , y * 2    ].Cr = blocks[0, y, x];
							MacroBlock_YCbCrs[x * 2 + 1, y * 2    ].Cr = blocks[0, y, x];
							MacroBlock_YCbCrs[x * 2    , y * 2 + 1].Cr = blocks[0, y, x];
							MacroBlock_YCbCrs[x * 2 + 1, y * 2 + 1].Cr = blocks[0, y, x];
						}
					}

					for (int x = 0; x < 16; ++x) {
						for (int y = 0; y < 16; ++y) {
							double Y = MacroBlock_YCbCrs[x, y].Y,
								Cb = MacroBlock_YCbCrs[x, y].Cb,
								Cr = MacroBlock_YCbCrs[x, y].Cr;
							double r = Y + 1.402 * Cr;
							double g = Y - 0.3437 * Cb - 0.7143 * Cr;
							double b = Y + 1.772 * Cb;

							MacroBlock_RGBs[x, y].R = (byte)Math.Max(Math.Min(r, 255), 0);
							MacroBlock_RGBs[x, y].G = (byte)Math.Max(Math.Min(g, 255), 0);
							MacroBlock_RGBs[x, y].B = (byte)Math.Max(Math.Min(b, 255), 0);
						}
					}

					for (int y = 0; y < 16; ++y) {
						for (int x = 0; x < 16; ++x) {
							int px = (mbx * 16) + x;
							int py = (mby * 16) + y;
							if (px < packet.Width && py < packet.Height) {
								MacroBlockRGB rgb = MacroBlock_RGBs[x, y];
								video_frame_data_ptr[(py * stride_in_pixels) + px] = (255 << 24) | (rgb.R << 16) | (rgb.G << 8) | rgb.B;
							}
						}
					}
				}
			}
			video_frame_image.UnlockBits(video_frame_data);

			mdec_writer.Dispose();
			return video_frame_image;
		}
	}
}
