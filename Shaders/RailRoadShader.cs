using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OTKGL;

namespace OTKGL.Shaders
{
    public class RailRoadShader : Shader
    {

        public RailRoadShader()
            : base()
        {
            vertSource = @"

                uniform float time;


                //
                // entry point.
                //
                void main( void )
                {
	                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
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

            CompileAndLink();

            Texture waterTex = new Texture(directories.rootDir + @"Images\texture\terrain\Water02.png", true, Shader.mainProgram, TextureUnit.Texture30, "waterTex");

            GL.Uniform1(GL.GetUniformLocation(Shader.mainProgram, "time"), World.CurrentWorld.elapsedSeconds);
        }
    }
}
