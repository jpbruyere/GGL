using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace GameLib
{
	public class SingleLightSimpleShader : Shader
	{
		public SingleLightSimpleShader ()
		{
			vertSource = @"
			#version 330

			precision highp float;

			uniform mat4 Projection;
			uniform mat4 ModelView;
			uniform mat4 Model;
			uniform mat4 Normal;
			uniform vec4 lightPos;
			

			in vec3 in_position;
			in vec2 in_tex;
			in vec3 in_normal;
			out vec2 texCoord;
			out vec3 v;
			out vec3 N;
			out vec3 lPos;
			out vec4 vPos;
			

			void main(void)
			{				

				texCoord = in_tex;
				N = normalize(vec3(Normal * Model * vec4(in_normal, 0)));

				v = vec3(ModelView * Model * vec4(in_position, 1));

				lPos = vec3(ModelView * lightPos);
				gl_Position = Projection * ModelView * Model * vec4(in_position, 1);
			}";

			fragSource = @"
			#version 330
			precision highp float;

			uniform vec4 color;
			uniform sampler2D tex;


			in vec2 texCoord;			
			in vec3 v;
			in vec3 N;
			in vec3 lPos;
			
			
			out vec4 out_frag_color;

			void main(void)
			{
				vec3 L = normalize(lPos-v);
				float NdotL = dot(N, L);
				if ( NdotL < 0.0) // light source on the wrong side?   
					NdotL = dot(-N, L);
   				vec3 Idiff = vec3(1.0,1.0,1.0) * max(NdotL, 0.0);  
   				Idiff = clamp(Idiff, 0.0, 1.0);    
				vec4 diffTex = texture( tex, texCoord);

				out_frag_color =  vec4(diffTex.rgb * Idiff,diffTex.a) * color; 
			}";
			Compile ();
		}

		protected int   lightPosLocation;

		public Vector4 LightPos {
			set { GL.Uniform4 (lightPosLocation, value); }
		}

		protected override void BindVertexAttributes ()
		{
			base.BindVertexAttributes ();

			GL.BindAttribLocation(pgmId, 2, "in_normal");
		}
		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			lightPosLocation = GL.GetUniformLocation (pgmId, "lightPos");
		}			
	}
}

