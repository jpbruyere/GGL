using System;
using OpenTK;
using go;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
	public class Light
	{
		public static Color GlobalAmbient
		{ set { GL.LightModel (LightModelParameter.LightModelAmbient, value.floatArray); } }

		public static void initLighting()
		{			
			GL.LightModel (LightModelParameter.LightModelLocalViewer, 1f);
			GL.LightModel (LightModelParameter.LightModelTwoSide, 0f);
		}

		public Vector4 Position
		{ set { GL.Light(name , LightParameter.Position, value); } }
		public Color Ambient
		{ set { GL.Light(name , LightParameter.Ambient, value.floatArray); } }
		public Color Diffuse
		{ set { GL.Light(name , LightParameter.Diffuse, value.floatArray); } }
		public Color Specular
		{ set { GL.Light(name , LightParameter.Specular, value.floatArray); } }
		public float ConstantAttenuation
		{ set { GL.Light(name , LightParameter.ConstantAttenuation, value); } }
		public float LinearAttenuation
		{ set { GL.Light(name , LightParameter.LinearAttenuation, value); } }
		public float QuadraticAttenuation
		{ set { GL.Light(name , LightParameter.QuadraticAttenuation, value); } }

		LightName name;
		public LightName Name
		{ get { return name; } }

		public Light (LightName _name)
		{
			name = _name;

			ConstantAttenuation = 1.0f;
			LinearAttenuation = 0.0f;
			QuadraticAttenuation = 0.0f;

			Position = new Vector4 (0.0f, 5.0f, 5.0f, 1.0f);
			Ambient = new Color (0.5f, 0.5f, 0.5f, 1.0f);
			Diffuse = new Color (0.9f, 0.9f, 0.9f, 1.0f);
			Specular = new Color (1.0f, 1.0f, 1.0f, 1.0f);
		}
	}
}

