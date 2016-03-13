using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;
using GameLib;

namespace Tetra
{
	public class Mat4InstancedShader : Shader
	{
		public Mat4InstancedShader ()
		{
			vertSource = @"
			#version 330
			precision highp float;

			layout (location = 0) in vec3 in_position;
			layout (location = 1) in vec2 in_tex;
			layout (location = 2) in vec3 in_normal;
			layout (location = 4) in mat4 in_model;

			layout (std140, index = 0) uniform block_data{
				mat4 Projection;
				mat4 ModelView;
				mat4 Normal;
				vec4 lightPos;
				vec4 Color;
			};

			out vec2 texCoord;			
			out vec3 n;			
			out vec4 vEyeSpacePos;
			

			void main(void)
			{				
				texCoord = in_tex;
				n = vec3(Normal * in_model * vec4(in_normal, 0));

				vec3 pos = in_position.xyz;

				vEyeSpacePos = ModelView * in_model * vec4(pos, 1);
				
				gl_Position = Projection * ModelView * in_model * vec4(pos, 1);
			}";

			fragSource = @"
			#version 330			

			precision highp float;


			layout (std140, index = 10) uniform fogData
			{ 
				vec4 fogColor;
				float fStart; // This is only for linear fog
				float fEnd; // This is only for linear fog
				float fDensity; // For exp and exp2 equation   
				int iEquation; // 0 = linear, 1 = exp, 2 = exp2
			};


			uniform sampler2D tex;			

			layout (std140, index = 0) uniform block_data{
				mat4 Projection;
				mat4 ModelView;
				mat4 Normal;
				vec4 lightPos;
				vec4 Color;
			};

			in vec2 texCoord;			
			in vec4 vEyeSpacePos;
			in vec3 n;			
			
			out vec4 out_frag_color;

			float getFogFactor(float fFogCoord)
			{
			   float fResult = 0.0;
			   if(iEquation == 0)
			      fResult = (fEnd-fFogCoord)/(fEnd-fStart);
			   else if(iEquation == 1)
			      fResult = exp(-fDensity*fFogCoord);
			   else if(iEquation == 2)
			      fResult = exp(-pow(fDensity*fFogCoord, 2.0));
			      
			   fResult = 1.0-clamp(fResult, 0.0, 1.0);
			   
			   return fResult;
			}

			void main(void)
			{
				vec4 diffTex = texture( tex, texCoord) * Color;
				if (diffTex.a < 0.5)
					discard;

				vec3 l;
				if (lightPos.w == 0.0)
					l = normalize(-lightPos.xyz);
				else
					l = normalize(lightPos.xyz - vEyeSpacePos.xyz);				

				float Idiff = clamp(max(dot(n,l), 0.0),0.5,1.0);

			   	float fFogCoord = abs(vEyeSpacePos.z/vEyeSpacePos.w);
/*
				out_frag_color = vec4( 
					mix(diffTex.rgb * Idiff , fogColor.rgb, getFogFactor(fFogCoord)),diffTex.a);
*/
				out_frag_color = vec4( 
					mix(diffTex.rgb, fogColor.rgb, getFogFactor(fFogCoord)),diffTex.a);
			}";
			Compile ();
		}

		public int DiffuseTexture;

		protected override void BindVertexAttributes ()
		{
			base.BindVertexAttributes ();

			GL.BindAttribLocation(pgmId, 2, "in_normal");
			GL.BindAttribLocation(pgmId, 4, "in_model");
		}
		protected override void GetUniformLocations ()
		{	
			GL.UniformBlockBinding(pgmId, GL.GetUniformBlockIndex(pgmId, "block_data"), 0);
			GL.UniformBlockBinding(pgmId, GL.GetUniformBlockIndex(pgmId, "fogData"), 10);
		}	
		public override void Enable ()
		{
			GL.UseProgram (pgmId);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, DiffuseTexture);
		}
	}
}

