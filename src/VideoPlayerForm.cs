/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HLPS1Str
{
	public partial class VideoPlayerForm: Form
	{
		private Bitmap CurrentFrame;
		private readonly object FrameLock = new object();

		public VideoPlayerForm()
		{
			Width = 640;
			Height = 480;
			Text = "PSX Movie Player";
			BackColor = Color.Black;

			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
			UpdateStyles();

			Paint += VideoPanel_Paint;
		}

		public void DisplayNewFrame(Bitmap frame_bitmap)
		{
			lock (FrameLock) {
				CurrentFrame?.Dispose(); 
				CurrentFrame = frame_bitmap;
			}

			if (InvokeRequired) {
				BeginInvoke(new Action(() => Invalidate()));
			} else {
				Invalidate();
			}
		}

		private void VideoPanel_Paint(object sender, PaintEventArgs e)
		{
			lock (FrameLock) {
				if (CurrentFrame != null) {
					e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
					e.Graphics.InterpolationMode = InterpolationMode.Low;
					e.Graphics.DrawImage(CurrentFrame, ClientRectangle);
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			CurrentFrame?.Dispose();
			base.OnFormClosing(e);
		}
	}
}
