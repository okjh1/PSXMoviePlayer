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
	public class Demuxer
	{
		public static int VIDEO_PACKET_CHUNK_SIZE = 2016;

		public class Packet
		{}

		public class VideoPacket: Packet
		{
			public uint Number;
			public ushort Version,
					Width,
					Height;
			public byte[] Payload;

			public int GetMBWidth() {
				return (Width + 15) / 16;
			}

			public int GetMBHeight() {
				return (Height + 15) / 16;
			}

			public int GetNumOfUncompressedMBs() {
				return GetMBWidth() * GetMBHeight();
			}
		}

		public class AudioPacket: Packet
		{
			// TODO
		}

		private readonly MultiBinaryReader Reader;
		private readonly CD Cd;
		private MemoryStream DemuxingVideoStream;

		public Demuxer(MultiBinaryReader reader)
		{
			Reader = reader;
			Cd = new CD(reader);
			DemuxingVideoStream = new MemoryStream();
		}

		public bool HasMorePackets()
		{
			return Reader.GetNumOfUnreadBytes() > 0;
		}

		public Packet DemuxNextPacket()
		{
			DEMUX:
			if (!HasMorePackets()) {
				throw new Exception("No more packets to demultiplex.");
			}
			CDSector sector = Cd.ReadSector();
			if (sector == null) {
				throw new Exception("Invalid sync field. Got: " + BitConverter.ToString(sector.Header.SyncField) + ", Expected: " + BitConverter.ToString(CD.SYNC_BYTES));
			}
			if (sector.Header.XaSubHeader == null) {
				throw new Exception("Extended architecture sub header was not found.");
			}
			if (sector.UserData == null) {
				throw new Exception("Unable to find data in sector.");
			}
			// todo: it is possible to support files with the standard sector size(with no audio but maybe there may be a way to detect audio), however that should be done later.
			if ((sector.Header.XaSubHeader.SubMode & (byte)CDXaSubModes.Audio) != 0) { // todo
				goto DEMUX;
				// return new AudioPacket {};
			}
			MultiBinaryReader user_data_reader = new MultiBinaryReader(sector.UserData);
			StrFrameSectorHeader frame_header = new StrFrameSectorHeader();
			StrFrameSectorHeader.Decode(ref frame_header, user_data_reader);
			if (frame_header.Magic != StrFrameSectorHeader.MAGIC && frame_header.Unknown != StrFrameSectorHeader.UNKNOWN) {
				user_data_reader.Dispose();
				throw new Exception("Invalid frame header magic or the unknown field. Expected: (" + StrFrameSectorHeader.MAGIC + ", 0) Got: (" + frame_header.Magic + ", " + frame_header.Unknown + ").");
			}
			if (frame_header.FrameVersion != 2) {
				user_data_reader.Dispose();
				throw new Exception("Unsupported frame version: " + frame_header.FrameVersion + ". only version 2 is supported for now.");
			}
			DemuxingVideoStream.Write(user_data_reader.ReadBytes(VIDEO_PACKET_CHUNK_SIZE), 0, VIDEO_PACKET_CHUNK_SIZE);
			user_data_reader.Dispose();
			if (frame_header.MuxedFrameChunkNumber + 1 == frame_header.NumOfMuxedFrameChunks) {
				VideoPacket packet = new VideoPacket {
					Number = frame_header.FrameNumber,
					Version = frame_header.FrameVersion,
					Width = frame_header.FrameWidth,
					Height = frame_header.FrameHeight,
					Payload = DemuxingVideoStream.GetBuffer()
				};
				DemuxingVideoStream.SetLength(0);
				return packet;
			}
			goto DEMUX;
		}
	}
}
