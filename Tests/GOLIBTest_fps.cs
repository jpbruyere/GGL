#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;


namespace test
{
	class GOLIBTest_fps : OpenTKGameWindow
	{
		#region FPS
		static int _fps = 0;

		public static int fps {
			get { return _fps; }
			set {
				_fps = value;
				if (_fps > fpsMax)
					fpsMax = _fps;
				else if (_fps < fpsMin)
					fpsMin = _fps;
			}

		}

		public static int fpsMin = int.MaxValue;
		public static int fpsMax = 0;

		static void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endregion

		public GOLIBTest_fps ()
			: base(800, 600,"test")
		{}


		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("ui/fps.goml");


		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}

		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			if (frameCpt > 200) {
				resetFps ();
				frameCpt = 0;

			}
			frameCpt++;
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_fps win = new GOLIBTest_fps( )) {
				win.Run (30.0);
			}
		}
	}
}