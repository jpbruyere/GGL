using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;	

namespace GameLib
{
	public class GlowShader : Shader
	{		
		public GlowShader ()
		{
			vertSource = @"
					#version 330
		
					precision highp float;
		
					uniform mat4 Projection;
					uniform mat4 ModelView;
					uniform mat4 Model;
		
					in vec3 in_position;
					in vec2 in_tex;
					in vec3 in_normal;
					out vec2 texCoord;
					
					out float vertexID;
		
					void main(void)
					{
						//mat4 normalMatrix = transpose(inverse(ModelView * Model));
						
						texCoord = in_tex;
						vertexID = float(gl_VertexID);
		
						gl_Position = Projection * ModelView * Model * vec4(in_position, 1);
					}";
		
			fragSource = @"
					#version 330
					precision highp float;
		
					uniform vec4 color;
					uniform sampler2D tex;
					uniform float width;
		
					in vec2 texCoord;
					in float vertexID;			
								
					out vec4 out_frag_color;
		
					void main(void)
					{						
						vec4 diffTex = texture( tex, texCoord);
						if (fract(texCoord.x) > width && fract(texCoord.y) > width &&
							fract(texCoord.x) < 1.0-width && fract(texCoord.y) < 1.0-width)
							discard;
						else
							out_frag_color = diffTex * color ;
					}";
		
			Compile ();
		}
		protected int widthLocation;
		public float BorderWidth {
			set { GL.Uniform1 (widthLocation, value); }
		}

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			widthLocation = GL.GetUniformLocation (pgmId, "width");
		}			
//		protected override void BindSamplesSlots ()
//		{
//			base.BindSamplesSlots ();
//		}	
//		protected override void GetUniformLocations ()
//		{
//			ProjectionLocation = GL.GetUniformLocation(pgmId, "Projection");
//			ModelViewLocation = GL.GetUniformLocation(pgmId, "ModelView");
//			ModelLocation = GL.GetUniformLocation(pgmId, "Model");
//			colorLocation = GL.GetUniformLocation (pgmId, "color");		
//		}
	}
}
//void main(void)
//{
//	float t = texCoord.y-0.5 ;
//	if (t>0.0)
//		out_frag_color = color * (1.0 - t*2.0);
//	else
//		out_frag_color = color * (1.0 + t*2.0);
//};

