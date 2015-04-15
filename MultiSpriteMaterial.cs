using System;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
	public class MultiSpriteMaterial : Material
	{
		public int wSprites;
		public int hSprites;
		public int nbSprites
		{ get { return wSprites * hSprites; } }

		public MultiSpriteMaterial (Texture spritesTex, int _wSprites, int _hSprites) : base()
		{
			DiffuseMap = spritesTex;
			shader = Material.multiSpriteShader;
			wSprites = _wSprites;
			hSprites = _hSprites;
		}

		public override void Enable ()
		{
			MultiSpriteShader mss = shader as MultiSpriteShader;

			GL.PushAttrib (
				AttribMask.EnableBit|
				AttribMask.LightingBit|
				AttribMask.TextureBit| 
				AttribMask.ColorBufferBit);

			mss.Enable ();

			mss.SpriteCountW = wSprites;
			mss.SpriteCountH = hSprites;

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, DiffuseMap);
			GL.Enable (EnableCap.Texture2D);
		}
		public override void Disable ()
		{
			MultiSpriteShader mss = shader as MultiSpriteShader;
			mss.Disable ();
			base.Disable ();
		}

	}
}

