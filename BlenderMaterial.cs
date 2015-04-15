using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using go;

namespace GGL
{
    [Serializable]
    public class Material
    {
		public static List<Material> Library;
		public static MaterialShader phongDiffuseTex;
		public static MaterialShader phongNormalShader;
		public static MaterialShader multiSpriteShader;
		public static MaterialShader simpleShader;

		public static void initShaders()
		{
			phongDiffuseTex = new MaterialShader ("phong1", "phong1");
			phongNormalShader = new MaterialShader ("phong1", "phong2");
			multiSpriteShader = new MultiSpriteShader ("simple.vert", "multiSprite1.frag");
			simpleShader = new MaterialShader ("simple.vert", "simpleTex.frag");
		}
		public Material()
		{
			shader = phongDiffuseTex;
		}
		public Material(string _name)
		{
			Name = _name;
			shader = phongDiffuseTex;
		}
        public string Name;
		public Shader shader = null;

		Color material_Ka = new Color( 0.1f, 0.1f, 0.1f, 1.0f );
		Color material_Kd = new Color( 0.9f, 0.9f, 0.9f, 1.0f );
		Color material_Ks = new Color( 0.5f, 0.5f, 0.5f, 1.0f );
		Color material_Ke = new Color( 0.0f, 0.0f, 0.0f, 0.0f );
		float material_Se = 20.0f;

		public Color Ambient {
			get { return material_Ka; }
			set { material_Ka = value; }
		}
		public Color Diffuse {
			get { return material_Kd; }
			set { material_Kd = value; }
		}
		public Color Specular {
			get { return material_Ks; }
			set { material_Ks = value; }
		}
		public Color Emission {
			get { return material_Ke; }
			set { material_Ke = value; }
		}
		public float Shininess {
			get { return material_Se; }
			set { material_Se = value; }
		}
        public float Transparency = 1.0f;

		public Texture DiffuseMap = null;
		public Texture NormalMap = null;
		public Texture AmbientMap = null;
		public Texture SpecularMap = null;
		public Texture SpecularHighlightMap = null;
		public Texture AlphaMap = null;
		public Texture BumpMap = null;
		public Texture DisplacementMap = null;
		public Texture StencilDecalMap = null;

		public virtual void Enable()
		{
			GL.PushAttrib (
				AttribMask.EnableBit|
				AttribMask.LightingBit|
				AttribMask.TextureBit| 
				AttribMask.ColorBufferBit);
				
			Shader.Enable (shader);
			GL.Disable (EnableCap.DepthTest);

			if (DiffuseMap != null) {
				GL.ActiveTexture (TextureUnit.Texture0);
				GL.BindTexture (TextureTarget.Texture2D, DiffuseMap);
			}
			if (NormalMap != null) {
				GL.ActiveTexture (TextureUnit.Texture1);
				GL.BindTexture (TextureTarget.Texture2D, NormalMap);
			}

			if (Transparency < 1.0) {
				GL.Enable(EnableCap.AlphaTest);
				GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);			
			}

			GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, material_Ka.floatArray);
			GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, material_Kd.floatArray);
			GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, material_Ks.floatArray);
			GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, material_Ke.floatArray);
			GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, material_Se);
		}
		public virtual void Disable()
		{
			GL.PopAttrib ();
			Shader.Disable (shader);
		}
    }

}
