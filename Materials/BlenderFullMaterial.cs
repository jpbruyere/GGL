using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using go;

namespace GGL
{
    [Serializable]
	public class BlenderFullMaterial : Material
    {
		public BlenderFullMaterial()
		{
			shader = phongDiffuseTex;
		}
		public BlenderFullMaterial(string _name)
		{
			Name = _name;
			shader = phongDiffuseTex;
		}
        
		public Texture AmbientMap = null;
		public Texture SpecularMap = null;
		public Texture SpecularHighlightMap = null;
		public Texture AlphaMap = null;
		public Texture BumpMap = null;
		public Texture DisplacementMap = null;
		public Texture StencilDecalMap = null;

		public virtual void Enable()
		{
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
			Shader.Disable (shader);
		}
    }

}
