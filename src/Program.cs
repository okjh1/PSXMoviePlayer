/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HLPS1Str
{
	internal class Program
	{
		// [STAThread]
		public static void Main(string[] args)
		{
			if (args.Count() < 1) {
				Console.WriteLine("Usage: " + Environment.CommandLine + " <filename.str>");
				return;
			}
			if (!File.Exists(args[0])) {
				Console.WriteLine("Unable to find " + args[0] + " file.");
				return;
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			ManualResetEvent shutdown_event = new ManualResetEvent(false);

			VideoPlayerForm player_form = new VideoPlayerForm();

			player_form.FormClosed += (sender, e) => {
				shutdown_event.Set();
			};

			Console.CancelKeyPress += (sender, e) => {
				e.Cancel = true;
				if (player_form != null && player_form.IsHandleCreated) {
					player_form.BeginInvoke(new Action(() => {
						player_form.Close();
					}));
				} else {
					shutdown_event.Set();
				}
			};

			Str str = new Str();
			MultiBinaryReader reader = new MultiBinaryReader(File.OpenRead(args[0]));
			Demuxer demuxer = new Demuxer(reader);

			player_form.FormClosing += (sender, e) => {
				reader.Dispose();
			};

			Task.Run(() => StartVideoStreamWorker(player_form, str, demuxer));

			Application.Run(player_form);

			shutdown_event.WaitOne();
		}

		private static async Task StartVideoStreamWorker(VideoPlayerForm player, Str str, Demuxer demuxer)
		{
			try {
				const double target_fps = 14.985;
				const double target_fps_ms = 1000.0 / target_fps;
				Stopwatch stopwatch = new Stopwatch();

				while (demuxer.HasMorePackets()) {
					stopwatch.Restart();

					Demuxer.Packet packet = demuxer.DemuxNextPacket();
					if (packet is Demuxer.VideoPacket video_packet) {
						player.DisplayNewFrame(str.ProcessNextFrame(video_packet));
					}

					stopwatch.Stop();
					double elapsed_ms = stopwatch.Elapsed.TotalMilliseconds;
					int delay_ms = (int)(target_fps_ms - elapsed_ms);
					if (delay_ms > 0) {
						await Task.Delay(delay_ms);
					}
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
