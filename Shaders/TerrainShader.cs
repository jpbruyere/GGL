using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
    public class TerrainShader : Shader
    {
        public static List<Texture> textures = new List<Texture>();
        public static int _selectedTexIndex = 0;
        public static int selectedTexIndex
        {
            get { return _selectedTexIndex; }
            set
            {
                _selectedTexIndex = value;
                if (_selectedTexIndex < 0)
                    _selectedTexIndex = textures.Count - 1;
                else if (_selectedTexIndex > textures.Count - 1)
                    _selectedTexIndex = 0;
            }
        }


         Texture alpha;
         Texture terre;
        //public Texture grass = new Texture(directories.rootDir + @"Images\texture\terrain\grass1.jpg", true, Shader.mainProgram, TextureUnit.Texture12, "Grass");
         Texture grass;
         Texture stone;
         Texture rock;

        public TerrainShader() : base()
        {
            fragSource = @"
                varying float fogFactor; 

                uniform sampler2D Alpha;
                uniform sampler2D Terre;
                uniform sampler2D Grass;
                uniform sampler2D Stone;
                uniform sampler2D Rock;

                uniform float tile;
                
                varying float A;
                

                void main(void)
                {
                   vec4 alpha       = texture2D( Alpha, gl_TexCoord[0].st );
                   vec4 terre       = texture2D( Terre, gl_TexCoord[0].st * tile); // Tile
                   vec4 grass       = texture2D( Grass, gl_TexCoord[0].st * tile); // Tile
                   vec4 stone       = texture2D( Stone, gl_TexCoord[0].st * tile ); // Tile
                   vec4 rock        = texture2D( Rock,  gl_TexCoord[0].st * tile  ); // Tile

                   terre *= alpha.a;                                // Red channel
                   vec4 outColor = mix( terre, grass, alpha.g );    // Green channel
                   outColor = mix( outColor, stone, alpha.r );      // red channel
                   outColor = mix( outColor, rock, alpha.b );       // Blue channel


//                    const float e = 2.71828;
//                    float fogFactor = (gl_Fog.density * gl_FragCoord.z);
//                    fogFactor *= fogFactor;
//                    fogFactor = clamp(pow(e, -fogFactor), 0.0, 1.0);
                    // Blend fog color with incoming color
                    float B = 1.0;
                    gl_FragColor = outColor * B;// mix(gl_Fog.color, outColor, fogFactor);                                                        
                    
                } 
            ";

            vertSource = @"
                varying float fogFactor; 

                uniform sampler2D Alpha;
                uniform sampler2D Terre;
                uniform sampler2D Grass;
                uniform sampler2D Stone;
                uniform sampler2D Rock;

                uniform float tile;

                varying float A;
                
                void main() {
                    

                    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                    gl_TexCoord[0] = gl_MultiTexCoord0;

                    vec4 V = gl_ModelViewMatrix * gl_Vertex;

                    gl_FogFragCoord = length(V);
                    
                    const float e = 2.71828;
                    fogFactor = (gl_Fog.density * length(V));
                    fogFactor *= fogFactor;
                    fogFactor = clamp(pow(e, -fogFactor), 0.0, 1.0);
//                    gl_FrontColor = mix(gl_Fog.color, gl_Color,
//                        fogFactor);                    
                    A = 1.0;
                    
                }
                ";

            geomSource = @"
				#version 120
 				
                #extension GL_EXT_geometry_shader4 : enable
                #extension GL_EXT_gpu_shader4 : enable

				void main(void)
				{
					// variable to use in for loops
					int x,y;

					// Emit the original vertices without changing, making
					// this part exactly the same as if no geometry shader
					// was used.
                    
                    

                    if (gl_PositionIn[0].w < 10.0)
                    {
                        vec4 midPos = mix(gl_PositionIn[1],gl_PositionIn[2],0.5f);
                        vec4 midTex = mix(gl_TexCoordIn[1][0],gl_TexCoordIn[2][0],0.5f);

						gl_Position = gl_PositionIn[0];
                        gl_TexCoord[0] = gl_TexCoordIn[0][0];
						EmitVertex();

						gl_Position = gl_PositionIn[1];
                        gl_TexCoord[0] = gl_TexCoordIn[1][0];
						EmitVertex();

						gl_Position = midPos;
                        gl_TexCoord[0] = midTex;
						EmitVertex();

                        EndPrimitive();

                        gl_Position = gl_PositionIn[0];
                        gl_TexCoord[0] = gl_TexCoordIn[0][0];
						EmitVertex();

						gl_Position = midPos;
                        gl_TexCoord[0] = midTex;
						EmitVertex();

                        gl_Position = gl_PositionIn[2];
                        gl_TexCoord[0] = gl_TexCoordIn[2][0];
						EmitVertex();


                        EndPrimitive();
                    }else{
                        int i;

					    // Emit the original vertices without changing, making
					    // this part exactly the same as if no geometry shader
					    // was used.
					    for(i=0; i< gl_VerticesIn; i++)
					    {
						    gl_Position = gl_PositionIn[i];
                            gl_TexCoord[0] = gl_TexCoordIn[i][0];
						    EmitVertex();
					    }
					    // End the one primitive with the original vertices
					    EndPrimitive();
                    }
                }					
			";


            Compile();

            createGroundTextures();
                 
        }

        void createGroundTextures()
        {
            Texture alpha = new Texture(directories.rootDir + @"Images/texture/terrain/alpha1.png");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            Texture terre = new Texture(directories.rootDir + @"Images/texture/terrain/terre2.jpg");
            textures.Add(terre);
            //Texture grass = new Texture(directories.rootDir + @"Images\texture\terrain\grass1.jpg", true, Shader.mainProgram, TextureUnit.Texture12, "Grass");
            Texture grass = new Texture(directories.rootDir + @"Images/texture/test/grass_1024.jpg");
            textures.Add(grass);
            Texture stone = new Texture(directories.rootDir + @"Images/texture/terrain/stone1.jpg");
            textures.Add(stone);
            Texture rock = new Texture(directories.rootDir + @"Images/texture/terrain/rock1.jpg");
            textures.Add(rock);
            GL.Disable(EnableCap.Texture2D);                
        }
			
        public void updateTiling(float tile)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "tile"),tile);
        }
    }
}
