using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
    class Shader1 : Shader
    {        
        public Shader1()
        {
            fragSource = @"
                #version 150
                uniform sampler2D tex0;
                uniform sampler2D tex1;
                uniform sampler2D tex2;
                uniform sampler2D tex3;
 
                void main(void)
                {
                 if (gl_TexCoord[0].s < 0.25){
                  gl_FragColor = texture2D( tex0, gl_TexCoord[0].st );  
                  gl_FragColor[1] = gl_FragColor[1] * 0.90;
                 }
                else if (gl_TexCoord[0].s < 0.5) {
                  gl_FragColor = texture2D( tex1, gl_TexCoord[0].st );  
                  gl_FragColor[0] = gl_FragColor[0] * 0.90;
                 }
                else if (gl_TexCoord[0].s < 0.75) {
                  gl_FragColor = texture2D( tex2, gl_TexCoord[0].st );  
                  gl_FragColor[2] = gl_FragColor[2] * 0.90;
                 }
                else {
                  gl_FragColor = texture2D( tex3, gl_TexCoord[0].st );  
                 }
                }
			";

            Init();

            Texture tex0 = new Texture("area03.JPG");
            Texture tex1 = new Texture("concrete.jpg");
            Texture tex2 = new Texture("area03.JPG");
            Texture tex3 = new Texture("area03.JPG");        
        }
    }
}
