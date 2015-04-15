using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace GGL
{
    public class ExternalShader : Shader
    {
        public static string shadersDirectory = directories.rootDir + @"Developpements/OpenGL/Shaders";

        string _vertSourcePath;
        string _fragSourcePath;
        string _geomSourcePath;

        public override string vertSource
        {
            get
            {
                try
                {
                    string tmp = "";

                    string path = _vertSourcePath;
                    if (!File.Exists(path))
                        path = shadersDirectory + System.IO.Path.DirectorySeparatorChar + path; 
                    
                    using (StreamReader reader = new StreamReader(path))
                    {
                        Debug.WriteLine("Loading " + _vertSourcePath + "...");
                        tmp = reader.ReadToEnd();    
                    }

                    return tmp;
                }
                catch (Exception)
                {
                    return base.vertSource;    
                }                
            }
        }
        public override string fragSource
        {
            get
            {
                try
                {
                    string tmp = "";

                    string path = _fragSourcePath;
                    if (!File.Exists(path))
                        path = shadersDirectory + System.IO.Path.DirectorySeparatorChar + path;


                    using (StreamReader reader = new StreamReader(path))
                    {
                        Debug.WriteLine("Loading " + _fragSourcePath + "...");
                        tmp = reader.ReadToEnd();
                    }

                    return tmp;
                }
                catch (Exception)
                {
                    return base.fragSource;
                }
            }
        }
        public override string geomSource
        {
            get
            {
                try
                {
                    string tmp = "";
                    
                    string path = _geomSourcePath;
                    if (!File.Exists(path))
                        path = shadersDirectory + System.IO.Path.DirectorySeparatorChar + path;

                    using (StreamReader reader = new StreamReader(path))
                    {
                        Debug.WriteLine("Loading " + _geomSourcePath + "...");
                        tmp = reader.ReadToEnd();
                    }

                    return tmp;
                }
                catch (Exception)
                {
                    return base.geomSource;
                }
            }
        }

        public ExternalShader(string _vsPath = "", string _fsPath = "", string _gsPath = "")
            : base()
        {
            if (!_vsPath.EndsWith(".vert", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_vsPath))
                _vertSourcePath = _vsPath + ".vert";
            else
                _vertSourcePath = _vsPath;

            if (!_fsPath.EndsWith(".frag", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_fsPath))
                _fragSourcePath = _fsPath + ".frag";
            else 
                _fragSourcePath = _fsPath;

            if (!_gsPath.EndsWith(".geom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_gsPath))
                _geomSourcePath = _gsPath + ".geom";
            else 
                _geomSourcePath = _gsPath;

            Compile();
        }

        public ExternalShader(string _shaderName)
            : base()
        {
            _vertSourcePath = shadersDirectory + System.IO.Path.DirectorySeparatorChar + _shaderName + ".vert";
            _fragSourcePath = shadersDirectory + System.IO.Path.DirectorySeparatorChar + _shaderName + ".frag";
            _geomSourcePath = shadersDirectory + System.IO.Path.DirectorySeparatorChar + _shaderName + ".geom";

            
            Compile();
        }

        public void reload()
        {
            Init();
            Compile();
        }

    }
}
