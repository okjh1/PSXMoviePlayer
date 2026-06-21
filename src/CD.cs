/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System.IO;
using System.Linq;

namespace HLPS1Str
{
	public enum CDXaSubModes : byte
	{
		EndOfRecord = 1 << 0,
		Video		= 1 << 1,
		Audio		= 1 << 2,
		Data		= 1 << 3,
		Trigger		= 1 << 4,
		Form		= 1 << 5,
		RealTime	= 1 << 6,
		EndOfFile	= 1 << 7
	};

	public class CDXaSubHeader
	{
		public byte InterleavedFileNumber,
			InterleavedChannelNumber;
		public byte SubMode;
		public byte CodingInformation;
	};

	// public struct CDMSFAddress
	// {
	// 	public byte Minute,
	// 			Second,
	// 			Frac;
	// };

	public struct CDHeader
	{
		public byte[] SyncField;
		// public CDMSFAddress Address;
		public byte Mode;
		public CDXaSubHeader XaSubHeader;
	};

	public class CDSector
	{
		public CDHeader Header;
		public MemoryStream UserData;
	};

	public class CD
	{
		public static readonly int SECTOR_SIZE = 2352,
									USER_DATA_SIZE = 2048,
									XA_USER_DATA_SIZE = 2324;
		public static readonly byte[] SYNC_BYTES = { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00 };

		private readonly MultiBinaryReader Reader;

		public CD(MultiBinaryReader reader)
		{
			Reader = reader;
		}

		public void ReadHeader(ref CDHeader header)
		{
			header.SyncField = Reader.ReadBytes(SYNC_BYTES.Length);
			// maybe can be used
			// header.Address = new CDMSFAddress {
			// 	Minute = Reader.BaseReader.ReadByte(),
			// 	Second = Reader.BaseReader.ReadByte(),
			// 	Frac = Reader.BaseReader.ReadByte()
			// };
			Reader.IgnoreBytes(3);
			header.Mode = Reader.ReadByte();
		}

		public void ReadXaSubHeader(ref CDXaSubHeader sub_header)
		{
			sub_header.InterleavedFileNumber = Reader.ReadByte();
			sub_header.InterleavedChannelNumber = Reader.ReadByte();
			sub_header.SubMode = Reader.ReadByte();
			sub_header.CodingInformation = Reader.ReadByte();
			Reader.IgnoreBytes(4);
		}

		public void ReaderIgnoreRemainingSectorBytes(long offset)
		{
			Reader.IgnoreBytes(SECTOR_SIZE - (Reader.BaseStream.Position - offset));
		}

		public static int GetXaSectorFormFromSubMode(byte sub_mode)
		{
			if (sub_mode == 0) {
				return -1;
			}
			return (sub_mode & (byte)CDXaSubModes.Form) == 0 ? 1 : 2;
		}

		public void ReaderSkipSectors(int num)
		{
			Reader.IgnoreBytes(SECTOR_SIZE * num);
		}

		public CDSector ReadSector()
		{
			long starting_pos = Reader.BaseStream.Position;
			CDHeader header = new CDHeader {
				XaSubHeader = null
			};
			ReadHeader(ref header);
			if (!header.SyncField.SequenceEqual(SYNC_BYTES)) {
				return null;
			}
			CDSector sector = new CDSector {
				Header = header
			};

			if (header.Mode == 0) {
				sector.UserData = null;
				ReaderIgnoreRemainingSectorBytes(starting_pos);
			} else if (header.Mode == 1) {
				sector.UserData = MultiBinaryReader.CreatePublicMemoryStream(Reader.ReadBytes(USER_DATA_SIZE), USER_DATA_SIZE);
				Reader.IgnoreBytes(288);
			} else if (header.Mode == 2) {
				CDXaSubHeader sub_header = new CDXaSubHeader();
				ReadXaSubHeader(ref sub_header);
				int form = GetXaSectorFormFromSubMode(sub_header.SubMode);
				if (form == 1) {
					sector.UserData = MultiBinaryReader.CreatePublicMemoryStream(Reader.ReadBytes(USER_DATA_SIZE), USER_DATA_SIZE);
					Reader.IgnoreBytes(280);
				} else if (form == 2) {
					sector.UserData = MultiBinaryReader.CreatePublicMemoryStream(Reader.ReadBytes(XA_USER_DATA_SIZE), XA_USER_DATA_SIZE);
					Reader.IgnoreBytes(4);
				} else { // should never be reached unless the file is corrupted, which shouldn't even work.
					sector.UserData = null;
					ReaderIgnoreRemainingSectorBytes(starting_pos);
				}
				sector.Header.XaSubHeader = sub_header;
			}
			return sector;
		}
	}
}
