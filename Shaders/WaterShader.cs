using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using GGL;

namespace GGL
{
    public class WaterShader : Shader
    {
        
        public WaterShader():base()
        {
            vertSource = @"

uniform float time;


//
// entry point.
//
void main( void )
{
	float scale = 2.0;
	float x = gl_Vertex.x;
	float y = gl_Vertex.y;

	// calculate a scale factor.
	float s = sin( (time+3.0*y)*scale );
	float c = cos( (time+5.0*x)*scale );
	float z = 0.05 * s * c;

	// offset the position along the normal.
	vec3 v = gl_Vertex.xyz + gl_Normal * z;

	// transform the attributes.
	gl_Position = gl_ModelViewProjectionMatrix * vec4( v, 1.0 );
   
	gl_FrontColor = gl_Color;
	gl_TexCoord[0] = gl_MultiTexCoord0;
}
";

            fragSource = @"
// simple fragment shader

uniform sampler2D waterTex;

void main()
{
    vec4 w       = texture2D( waterTex, gl_TexCoord[0].st );
    gl_FragColor = w;
}";

//            geomSource = @"
//				#version 120 				
//                #extension GL_EXT_geometry_shader4 : enable
//                #extension GL_EXT_gpu_shader4 : enable
//				void main(void)
//				{
//                        int i;
//
//					    // Emit the original vertices without changing, making
//					    // this part exactly the same as if no geometry shader
//					    // was used.
//					    for(i=0; i< gl_VerticesIn; i++)
//					    {
//						    gl_Position = gl_PositionIn[i];
//                            gl_TexCoord[0] = gl_TexCoordIn[i][0];
//						    EmitVertex();
//					    }
//					    // End the one primitive with the original vertices
//					    EndPrimitive(); 
//                }";

//            geomSource = @"
//				#version 120 				
//                #extension GL_EXT_geometry_shader4 : enable
//                #extension GL_EXT_gpu_shader4 : enable
//				void main(void)
//				{
//					// variable to use in for loops
//					int x,y;
//
//					// Emit the original vertices without changing, making
//					// this part exactly the same as if no geometry shader
//					// was used.
//                    
//                    float width = (gl_PositionIn[4].x - gl_PositionIn[0].x) / 10.0;
//                    float height = (gl_PositionIn[1].y - gl_PositionIn[0].y) / 10.0;
//
//					vec4 pos = gl_PositionIn[0];
////
////                    for(y=0; y< 10; y++)
////					{
////                        for(x=0; x< 10; x++)
////					    {                                
////                            gl_Position = pos;
////                            gl_TexCoord[0] = gl_TexCoordIn[0][0];
////						    EmitVertex();
////                            pos += vec4(0.0,height,0.0,0.0);
////                            gl_Position = pos;
////                            gl_TexCoord[0] = gl_TexCoordIn[0][1];
////						    EmitVertex();
////                            pos += vec4(width,0.0,0.0,0.0);
////                            gl_Position = pos;
////                            gl_TexCoord[0] = gl_TexCoordIn[0][2];
////						    EmitVertex();
////                            pos -= vec4(0.0,height,0.0,0.0);
////                            gl_Position = pos;
////                            gl_TexCoord[0] = gl_TexCoordIn[0][3];
////						    EmitVertex();
////                            EndPrimitive();                            
////					    }
////                        pos.x = gl_PositionIn[0].x;
////                        pos.y += height; 
////                    }
////                    for(y=0; y< 10; y++)
////					{
////                        for(x=0; x< 10; x++)
////					    {                                
//					        int i;
//
//					        // Emit the original vertices without changing, making
//					        // this part exactly the same as if no geometry shader
//					        // was used.
//					        for(i=0; i< gl_VerticesIn; i++)
//					        {
//						        gl_Position = gl_PositionIn[i];
//                                gl_TexCoord[0] = gl_TexCoordIn[0][i];
//						        EmitVertex();
//					        }
//					        // End the one primitive with the original vertices
//					        EndPrimitive();                           
////					    }
////                        pos.x = gl_PositionIn[0].x;
////                        pos.y += height; 
////                    }
//				}
//
//			";

            Compile();

            Texture waterTex = new Texture(directories.rootDir + @"Images\texture\terrain\Water02.png");

			//GL.Uniform1(GL.GetUniformLocation(Shader.mainProgram, "time"), World.CurrentWorld.elapsedSeconds);
        }
    }
}
