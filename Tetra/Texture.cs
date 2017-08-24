using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using GGL;
using OpenTK.Graphics.OpenGL;

namespace Tetra
{
    [Serializable]
	public class Texture : IDisposable
    {
		public static int NumSamples = 1;
		public static TextureTarget DefaultTarget = TextureTarget.Texture2D;
		public static TextureMinFilter DefaultMinFilter = TextureMinFilter.Linear;
		public static TextureMagFilter DefaultMagFilter = TextureMagFilter.Linear;
		public static TextureWrapMode DefaultWrapMode = TextureWrapMode.ClampToEdge;
		public static bool GenerateMipMaps = true;
		public static bool FlipY = true;
		/// <summary>Compressed formats must have a border of 0, so this is constant.</summary>
		public static int Border = 0;
		static Tetra.Shader msTexSaveShader;

		public static void ResetToDefaultLoadingParams()
		{
			DefaultTarget = TextureTarget.Texture2D;
			DefaultMinFilter = TextureMinFilter.Linear;
			DefaultMagFilter = TextureMagFilter.Linear;
			DefaultWrapMode = TextureWrapMode.ClampToEdge;
			GenerateMipMaps = true;
			FlipY = true;
			int Border = 0;
		}


        public string MapPath;
		public TextureTarget TexTarget = TextureTarget.Texture2D;
		public PixelInternalFormat InternalFormat = PixelInternalFormat.Rgba;
		public OpenTK.Graphics.OpenGL.PixelFormat PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
		public PixelType PixelType = PixelType.UnsignedByte;
        public uint texRef;
		public int Width = -1;
		public int Height = -1;
		public int LayerCount = 0;
		public TextureMinFilter MinFilter = TextureMinFilter.Linear;
		public TextureMagFilter MagFilter = TextureMagFilter.Linear;
		public int Samples = 1;

		public Texture(int width, int height,
			PixelInternalFormat internalFormat = PixelInternalFormat.Rgba,
			OpenTK.Graphics.OpenGL.PixelFormat pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
			PixelType pixelType = PixelType.UnsignedByte)
		{
			Width = width;
			Height = height;

			InternalFormat = internalFormat;
			PixelFormat = pixelFormat;
			PixelType = pixelType;

			TexTarget = DefaultTarget;

			createTexture (IntPtr.Zero);

			if (TexTarget == TextureTarget.Texture2DMultisample)
				return;

			configureTexParameters ();
		}

		public Texture(){
			TexTarget = DefaultTarget;
		}
		public void SetFilters(TextureMinFilter minFilter, TextureMagFilter magFilter){
			GL.BindTexture(TexTarget, texRef);
			GL.TexParameter(TexTarget, TextureParameterName.TextureMinFilter, (int)minFilter);
			GL.TexParameter(TexTarget, TextureParameterName.TextureMagFilter, (int)magFilter);
			GL.BindTexture(TexTarget, 0);
		}
		public void Create()
		{
			createTexture (IntPtr.Zero);
		}
		void createTexture(IntPtr data)
		{
			GL.GenTextures(1, out texRef);
			GL.BindTexture(TexTarget, texRef);
			if (TexTarget == TextureTarget.Texture2DMultisample) {
				GL.TexImage2DMultisample ((TextureTargetMultisample)TexTarget, Samples, InternalFormat, Width, Height, false);
			}else
				GL.TexImage2D(TexTarget, 0, InternalFormat, Width, Height, 0,
					PixelFormat, PixelType, data);
		}
		void configureTexParameters()
		{
			if (GenerateMipMaps) {
				GL.GenerateMipmap((GenerateMipmapTarget)TexTarget);
				GL.TexParameter(TexTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			}else
				GL.TexParameter(TexTarget, TextureParameterName.TextureMinFilter, (int)DefaultMinFilter);
			GL.TexParameter(TexTarget, TextureParameterName.TextureMagFilter, (int)DefaultMagFilter);
//			GL.TexParameter(TexTarget, TextureParameterName.TextureWrapS, (int)DefaultWrapMode);
//			GL.TexParameter(TexTarget, TextureParameterName.TextureWrapT, (int)DefaultWrapMode);
//			if (TexTarget == TextureTarget.Texture3D || TexTarget == TextureTarget.TextureCubeMap)
//				GL.TexParameter(TexTarget, TextureParameterName.TextureWrapR, (int)DefaultWrapMode);
		}

        public static implicit operator int(Texture t)
        {
			return t == null ? 0: (int)t.texRef;
        }

		public static Texture Load(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			Texture tmp = null;

			try {
				Stream stream = FileSystemHelpers.GetStreamFromPath (path);

				if (path.EndsWith (".dds", StringComparison.InvariantCultureIgnoreCase)) {
					byte[] imgbuff = new byte[stream.Length];
					stream.Read (imgbuff, 0, (int)stream.Length);

					TextureLoaders.ImageDDS.LoadFromByteArray (imgbuff, out tmp);

					tmp.MapPath = path;
				} else {
					Bitmap bitmap = new Bitmap(stream);

					if(FlipY)
						bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

					BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
						ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					tmp = new Texture ();
					tmp.MapPath = path;
					tmp.Width = data.Width;
					tmp.Height = data.Height;
					tmp.createTexture (data.Scan0);

					bitmap.UnlockBits(data);
				}
				stream.Dispose();
				tmp.configureTexParameters ();

			}
			catch (Exception ex) {
				throw new Exception ("Error loading texture: " + path + ":" + ex.Message);
			}
			return tmp;
		}
		public static Texture LoadCubeMap(params string[] _mapPath){
			Texture tmp = new Texture();
			GL.ActiveTexture (TextureUnit.Texture0);
			tmp.texRef = (uint)GL.GenTexture();
			GL.BindTexture(TextureTarget.TextureCubeMap, tmp.texRef);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
				(int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
				(int)TextureMinFilter.Linear);

			int i = 0;
			try {

				foreach (string path in _mapPath) {
					if (string.IsNullOrEmpty(path))
						continue;
					using (Stream fs = FileSystemHelpers.GetStreamFromPath (path)) {
						Bitmap bmp = new Bitmap (fs);

						if (tmp.Width < 0) {
							tmp.Width = bmp.Width;
							tmp.Height = bmp.Height;
						}

						if (tmp.Width != bmp.Width || tmp.Height != bmp.Height)
							throw new Exception ("Different size for cube textures");

						if(FlipY)
							bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

						BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
							ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

						tmp.TexTarget = TextureTarget.TextureCubeMapPositiveX + i;
						GL.TexImage2D(tmp.TexTarget, 0, tmp.InternalFormat, bmp.Width, bmp.Height, 0,
							tmp.PixelFormat, tmp.PixelType, bmpdata.Scan0);
						bmp.UnlockBits(bmpdata);
					}
					i++;
				}
				while(i<6){
					tmp.TexTarget = TextureTarget.TextureCubeMapPositiveX + i;
					GL.TexImage2D(tmp.TexTarget, 0, tmp.InternalFormat, tmp.Width, tmp.Height, 0,
						tmp.PixelFormat, tmp.PixelType, IntPtr.Zero);

					i++;
				}
			} catch (Exception ex) {
				throw new Exception ("Error loading cube textures: " + ex.Message);
			}
			tmp.TexTarget = TextureTarget.TextureCubeMap;
			return tmp;
		}
		public static Texture Load(TextureTarget textureTarget, params string[] _mapPath)
		{

			byte[] data = null;

			int imgSizeInByte = 0,
				offset = 0;

			Texture tmp = new Texture () {
				TexTarget = textureTarget,
				LayerCount = _mapPath.Length
			};

			try {

				foreach (string path in _mapPath) {
					if (string.IsNullOrEmpty(path))
						continue;
					using (Stream fs = FileSystemHelpers.GetStreamFromPath (path)) {
						Bitmap bmp = new Bitmap (fs);

						if (tmp.Width < 0) {
							tmp.Width = bmp.Width;
							tmp.Height = bmp.Height;
							imgSizeInByte = tmp.Width * tmp.Height * 4;
							data = new byte[imgSizeInByte * tmp.LayerCount];
						}

						if (tmp.Width != bmp.Width || tmp.Height != bmp.Height){
							bmp = new Bitmap(bmp,tmp.Width,tmp.Height);
							if (tmp.Width != bmp.Width || tmp.Height != bmp.Height)
								throw new Exception ("Different size for image array");
						}

						if(FlipY)
							bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);


						BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
							ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

						Marshal.Copy(bmpdata.Scan0, data, offset, imgSizeInByte);


						bmp.UnlockBits(bmpdata);

						offset += imgSizeInByte;
					}
				}
				GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
				IntPtr pointer = pinnedArray.AddrOfPinnedObject();

				GL.GenTextures(1, out tmp.texRef);
				GL.BindTexture(tmp.TexTarget, tmp.texRef);
				GL.TexImage3D(tmp.TexTarget, 0,
					tmp.InternalFormat, tmp.Width, tmp.Height, tmp.LayerCount, 0,
					tmp.PixelFormat, tmp.PixelType, pointer);

				pinnedArray.Free();

				tmp.configureTexParameters ();

				GL.TexParameter(tmp.TexTarget, TextureParameterName.TextureWrapR, (int)DefaultWrapMode);

				GL.BindTexture(tmp.TexTarget, 0);

			} catch (Exception ex) {
				throw new Exception ("Error loading textures: " + ex.Message);
			}

			return tmp;
		}
		public void Add3DTextureLayer(string path){

			int imgSizeInByte = Width * Height * 4;
			byte[] oldData = new byte[imgSizeInByte * LayerCount];

			GL.BindTexture (TexTarget, texRef);
			GL.GetTexImage (TexTarget, 0, PixelFormat, PixelType, oldData);

			byte[] newData = new byte[imgSizeInByte * (LayerCount + 1)];

			Array.Copy (oldData, newData, oldData.Length);

			using (Stream fs = FileSystemHelpers.GetStreamFromPath (path)) {
				Bitmap bmp = new Bitmap (fs);

				if (Width != bmp.Width || Height != bmp.Height){
					bmp = new Bitmap (bmp, Width, Height);
					if (Width != bmp.Width || Height != bmp.Height)
						throw new Exception ("Different size for image array");
				}

				if(FlipY)
					bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

				BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
					ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				Marshal.Copy(bmpdata.Scan0, newData, oldData.Length, imgSizeInByte);

				bmp.UnlockBits(bmpdata);
			}

			LayerCount++;

			GCHandle pinnedArray = GCHandle.Alloc(newData, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			GL.TexImage3D(TexTarget, 0,
				InternalFormat, Width, Height, LayerCount, 0,
				PixelFormat, PixelType, newData);

			pinnedArray.Free();
			GL.BindTexture(TexTarget, 0);
		}

		static TextureTarget bindAndGetTexTargetFromId(int texId){
			GL.BindTextures(0, 1,new int[]{texId});
			GL.ActiveTexture(TextureUnit.Texture0);
			int boundId = 0;

			boundId = GL.GetInteger(GetPName.TextureBinding1D);
			if (boundId == texId)
				return TextureTarget.Texture1D;
			boundId = GL.GetInteger(GetPName.TextureBinding2D);
			if (boundId == texId)
				return TextureTarget.Texture2D;
			boundId = GL.GetInteger(GetPName.TextureBinding3D);
			if (boundId == texId)
				return TextureTarget.Texture3D;
			boundId = GL.GetInteger(GetPName.TextureBinding2DMultisample);
			if (boundId == texId)
				return TextureTarget.Texture2DMultisample;

			return TextureTarget.Texture2D;
		}
//		public static Texture RecreateTextureFromId(int texId){
//			Texture tmp = new Texture ();
//			tmp.texRef = texId;
//			tmp.TexTarget = bindAndGetTexTargetFromId (texId);
//
//			int depthSize, stencilSize, alphaSize, redSize, greenSize, blueSize;
//			int depthType, alphaType, redType, greenType, blueType;
//
//
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureWidth, out tmp.Width);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureHeight, out tmp.Height);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureDepthSize, out depthSize);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureAlphaSize, out alphaSize);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureRedSize, out redSize);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureGreenSize, out greenSize);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureBlueSize, out blueSize);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureDepthType, out depthType);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureAlphaType, out alphaType);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureRedType, out redType);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureGreenType, out greenType);
//			GL.GetTexLevelParameter (tmp.TexTarget, 0, GetTextureParameter.TextureBlueType, out blueType);
//
//			string strInternalFormat = "";
//
//			if (depthType > 0) {
//				tmp.PixelType = (PixelType)depthType;
//
//				strInternalFormat = "Depth";
//				if (depthSize > 8)
//					strInternalFormat += depthSize.ToString ();
//
//				if (stencilSize > 0) {
//					tmp.PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.DepthStencil;
//					strInternalFormat += "Stencil";
//				} else {
//					fbAttachment = FramebufferAttachment.DepthAttachment;
//					strInternalFormat += "Component" + depthSize.ToString ();
//
//					if (PixelType == PixelType.Float)
//						strInternalFormat += "f";
//				}
//			} else {
//				//PixelType = (PixelType)redType;
//
//				if (redSize > 0)
//					strInternalFormat += "R";
//				if (greenSize > 0)
//					strInternalFormat += "G";
//				if (blueSize > 0)
//					strInternalFormat += "B";
//				if (alphaSize > 0)
//					strInternalFormat += "A";
//				if (redSize == greenSize && redSize == blueSize && redSize == alphaSize)
//					strInternalFormat += redSize.ToString ();
//
//				if (PixelType == PixelType.Float)
//					strInternalFormat += "f";
//
//				PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
//			}
//			if (!Enum.TryParse (strInternalFormat, true, out tmpInternalFormat)) {
//				Debug.WriteLine ("unable to determine internal format: " + strInternalFormat);
//				return;
//			}
//
//			InternalFormat = tmpInternalFormat;
//
//		}

		void getTexDatas<T>(){
			T[] raw = new T[Height * Width];
			byte[] data = new byte[Height * Width * 4];
			T min = raw.Min ();
			T max = raw.Max ();
			T diff = MiscUtil.Operator.Subtract (max, min);
//			for (int i = 0; i < raw.Length; i++) {
//				byte b = (byte)(MiscUtil.Operator.Divide (MiscUtil.Operator.Subtract (raw [i], min), diff));
//				data [i * 4] = b;
//				data [i * 4 + 1] = b;
//				data [i * 4 + 2] = b;
//				data [i * 4 + 3] = 255;
//			}

		}
		#region IDisposable implementation

		public void Dispose ()
		{
			if (GL.IsTexture (texRef))
				GL.DeleteTexture (texRef);
		}

		#endregion
    }

}
