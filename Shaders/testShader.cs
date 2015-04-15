using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace GGL
{

    class testShader : Shader
    {
        public testShader()
        {
            vertSource = @"
                #version 120

                uniform float texSize;

                void main(void)
                {                    
                    vec4 vertex = gl_Vertex;
                    
                    vertex.z += 0.5;
                    vec4 outPos = gl_ModelViewProjectionMatrix * vertex;
                    
                    // calculate point size based on distance from eye
                    vec4 V = gl_ModelViewMatrix * gl_Vertex;
                    float ptSize = length(V);
                    ptSize *= ptSize;
                    gl_PointSize = texSize / outPos.w;
                    gl_Position = outPos;                     
                }"
                ;
            fragSource = @"
                #version 120

                uniform sampler2D sprite_texture;

                void main(void)
                {
                    vec2 tc = gl_PointCoord.st;
                    //tc.y = tc.y / 2.0 + 0.5
                    vec4 c = texture2D( sprite_texture, tc );  
                    if (c.a == 0.0)
                        discard;                          
                    gl_FragColor = c ;
                }            
            ";

//            geomSource = @"
//                #version 120 
//				//#extension GL_EXT_geometry_shader4 : enable
//				void main(void)
//				{
//				    gl_Position = vec3(0,0,0);
//    		    }
//			";
            Compile();
        }

    }
}