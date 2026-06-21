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
	class MDECEmulator
	{
		public static ushort END_OF_DATA = 0xfe00;

		public static int[,] ZIG_ZAG_MATRIX = {
			{  0,  1,  5,  6, 14, 15, 27, 28 },
			{  2,  4,  7, 13, 16, 26, 29, 42 },
			{  3,  8, 12, 17, 25, 30, 41, 43 },
			{  9, 11, 18, 24, 31, 40, 44, 53 },
			{ 10, 19, 23, 32, 39, 45, 52, 54 },
			{ 20, 22, 33, 38, 46, 51, 55, 60 },
			{ 21, 34, 37, 47, 50, 56, 59, 61 },
			{ 35, 36, 48, 49, 57, 58, 62, 63 }
		};

		public static int[,] PSX_QUANTIZATION_TABLE = {
			{  2, 16, 19, 22, 26, 27, 29, 34 },
			{ 16, 16, 22, 24, 27, 29, 34, 37 },
			{ 19, 22, 26, 27, 29, 34, 34, 38 },
			{ 22, 22, 26, 27, 29, 34, 37, 40 },
			{ 22, 26, 27, 29, 32, 35, 40, 48 },
			{ 26, 27, 29, 32, 35, 40, 48, 58 },
			{ 26, 27, 29, 34, 38, 46, 56, 69 },
			{ 27, 29, 35, 38, 46, 56, 69, 83 }
		};

		public static double[,] DEFAULT_DCT_MATRIX = {
			{ 0.3535533905932738,   0.3535533905932738,   0.3535533905932738,   0.3535533905932738,   0.3535533905932738,   0.3535533905932738,   0.3535533905932738, 0.3535533905932738    },
			{ 0.4903926402016152,   0.4157348061512726,   0.27778511650980114,  0.09754516100806417, -0.0975451610080641,  -0.277785116509801,   -0.4157348061512727, -0.4903926402016152   },
			{ 0.46193976625564337,  0.19134171618254492, -0.19134171618254486, -0.46193976625564337, -0.4619397662556434,  -0.19134171618254517,  0.191341716182545, 0.46193976625564326    },
			{ 0.4157348061512726,  -0.0975451610080641,  -0.4903926402016152,  -0.2777851165098011,   0.2777851165098009,   0.4903926402016152,   0.09754516100806439, -0.41573480615127256 },
			{ 0.3535533905932738,  -0.35355339059327373, -0.35355339059327384,  0.3535533905932737,   0.35355339059327384, -0.35355339059327334, -0.35355339059327356, 0.3535533905932733   },
			{ 0.27778511650980114, -0.4903926402016152,   0.09754516100806415,  0.4157348061512728,  -0.41573480615127256, -0.09754516100806401,  0.4903926402016153, -0.27778511650980076  },
			{ 0.19134171618254492, -0.4619397662556434,   0.46193976625564326, -0.19134171618254495, -0.19134171618254528,  0.46193976625564337, -0.4619397662556432, 0.19134171618254478   },
			{ 0.09754516100806417, -0.2777851165098011,   0.4157348061512728,  -0.4903926402016153,   0.49039264020161527, -0.4157348061512725,   0.27778511650980076, -0.09754516100806429 }
		};

		public double[,] DCT_Matrix = DEFAULT_DCT_MATRIX;

		public void SetDCT(double[,] dct_matrix)
		{
			if (dct_matrix.GetLength(0) + dct_matrix.GetLength(1) != 16) {
				throw new Exception("Invalid IDCT Matrix passed as argument");
			}
			DCT_Matrix = dct_matrix;
		}

		// This is what was used to generate the DEFAULT_DCT_MATRIX
		/*private void GenerateAndSetDCTMatrix()
		{
			DCT_Matrix = new double[8, 8];
			for (int dct_x = 0; dct_x < 8; ++dct_x) {
				for (int dct_y = 0; dct_y < 8; ++dct_y) {
					double value = 0.0;
					if (dct_x == 0.0) {
						value *= 0.3535533905932738;
					} else {
						value *= 0.5;
					}
					DCT_Matrix[dct_x, dct_y] = value * Math.Cos(dct_x * Math.PI * (2 * dct_y + 1) / 16);
				}
			}
		}*/

		public void DecodeBlock(byte[] buffer, int block, ref double[,,] out_blk)
		{
			BinaryReader reader = new BinaryReader(new MemoryStream(buffer));

			ushort first_word = reader.ReadUInt16();
			ushort quantization_scale = (ushort)(first_word >> 10);
			ushort dc_coefficient = (ushort)(first_word & 0x3ff);

			int[] coefficient_list = new int[64];
			coefficient_list[0] = Utils.SignExtend((uint)dc_coefficient, 10);

			ushort rlc = reader.ReadUInt16();
			int idx = 0;
			while (rlc != END_OF_DATA) {
				int run = (rlc >> 10) & 0x3f;
				int level = Utils.SignExtend((uint)(rlc & 0x3ff), 10);
				idx += 1 + run;
				if (idx > 63) { // probably should not happen if the file is not broken
					Console.WriteLine("COEFFICIENT > 63: " + idx + ", RUN: " + run + ", LEVEL: " + level + ", RLC: " + rlc);
					break;
				}
				coefficient_list[idx] = level;
				rlc = reader.ReadUInt16();
			}
			reader.Dispose();

			// Unzigzag + dequantize
			double[,] dequantized_matrix = new double[8, 8];
			for (int x = 0; x < 8; ++x) {
				for (int y = 0; y < 8; ++y) {
					int coefficient = coefficient_list[ZIG_ZAG_MATRIX[x, y]];
					if (x == 0 && y == 0) {
						dequantized_matrix[x, y] = coefficient * PSX_QUANTIZATION_TABLE[x, y];
					} else {
						dequantized_matrix[x, y] = 2 * coefficient * quantization_scale * PSX_QUANTIZATION_TABLE[x, y] / 16;
					}
					// todo: check for the limits specified in the doc?
				}
			}

			// IDCT_matrix^transposed * Dequantized_Matrix * IDCT_matrix
			double[,] sub_totals = new double[8, 8];
			for (int i = 0; i < 8; ++i) {
				for (int j = 0; j < 8; ++j) {
					double sub_total = 0.0;
					for (int k = 0; k < 8; ++k) {
						sub_total += DCT_Matrix[k, i] * dequantized_matrix[k, j];
					}
					sub_totals[i, j] = sub_total;
				}
			}
			for (int i = 0; i < 8; ++i) {
				for (int j = 0; j < 8; ++j) {
					double total = 0.0;
					for (int k = 0; k  < 8; ++k) {
						total += sub_totals[i, k] * DCT_Matrix[k, j];
					}
					out_blk[block, i, j] = total;
				}
			}
		}
	}
}
