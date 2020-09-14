// https://github.com/lewa-j/Unity-Source-Tools/blob/master/Assets/Code/Read/VMTLoader.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace VMTLoader
{
    public class VMTLoader
    {
        public class VMTFile
        {
            public string shader;

            public string basetexture;
            public string basetexture2;
            public string bumpmap;
            public string surfaceprop;
            public string detil;
            public string dudvmap;
            public float detailscale;

            public bool alphatest;
            public bool translucent;
            public bool selfillum;
            public bool additive;

            public string envmap;
            public float basealphaenvmapmask;
            public float envmapcontrast;
            public float envmapsaturation;
            public Vector3 envmaptint;
        }

        public static VMTFile ParseVMTFile(string path)
        {
            VMTFile material = new VMTFile();

            string[] file = File.ReadAllLines(path);
            string line = null;
            int depth = 0;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            string block = null;
            for (int i = 0; i < file.Length; i++)
            {
                line = file[i].Trim().Trim('\t');
                line = line.Replace("\"\"", "\" \"");

                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith("{"))
                {
                    depth++;
                    continue;
                }

                if (depth == 0)
                {
                    material.shader = line.Trim('"').ToLower();
                }
                else if (depth == 1)
                {
                    if (line.StartsWith("}"))
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (line.Split(new char[] { ' ', '\t' }).Length < 2)
                        {
                            block = line.Trim('"').ToLower();
                        }
                        else
                        {
                            string[] temp = line.Trim().Split(new char[] { ' ', '\t' }, 2);
                            if (temp.Length < 2)
                            {
                                Debug.Log(path + " " + line);
                            }

                            parameters.Add(temp[0].Trim('"').ToLower(), temp[1].Trim().Trim('"'));
                        }
                    }
                }
                else if (depth == 2)
                {
                    if (line.StartsWith("}"))
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (line.Split(new char[] { ' ', '\t' }).Length < 2)
                        {
                            //Debug.Log ("Line is short "+line);
                        }
                        else
                        {
                            if (block == "insert")
                            {
                                string[] temp = line.Trim().Split(new char[] { ' ', '\t' }, 2);
                                if (temp.Length < 2)
                                {
                                    Debug.Log(path + " " + line);
                                }

                                if (!parameters.ContainsKey(temp[0].Trim('"').ToLower()))
                                {
                                    parameters.Add(temp[0].Trim('"').ToLower(), temp[1].Trim().Trim('"'));
                                }
                                else
                                {
                                    parameters[temp[0].Trim('"').ToLower()] = temp[1].Trim().Trim('"');
                                }
                            }
                        }
                    }
                }

                if (line.StartsWith("}"))
                {
                    depth--;
                    if (depth == 0)
                    {
                        break;
                    }
                }
            }

            if (material.shader == "patch")
            {
                Debug.Log("Patch!");
                if (parameters.ContainsKey("include"))
                {
                    string basePath = path.Substring(0, path.LastIndexOf('/'));
                    string patchPath = basePath + parameters["include"].ToLower();
                    material = ParseVMTFile(patchPath);

                    if (material == null)
                    {
                        Debug.LogError($"Include \"{patchPath}\" from material \"{path}\" missing");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning("Shader is patch but has no parameter include " + path);
                    for (int i = 0; i < parameters.Keys.ToArray().Length; i++)
                    {
                        Debug.Log(parameters.Keys.ToArray()[i]);
                    }

                    return null;
                }
            }

            if (parameters.ContainsKey("$basetexture"))
            {
                material.basetexture = parameters["$basetexture"];
            }
            else
            {
                if (parameters.ContainsKey("%tooltexture"))
                {
                    material.basetexture = parameters["%tooltexture"];
                }
                else if (parameters.ContainsKey("$iris"))
                {
                    material.basetexture = parameters["$iris"];
                }
            }

            if (parameters.ContainsKey("$basetexture2"))
            {
                material.basetexture2 = parameters["$basetexture2"];
            }

            if (parameters.ContainsKey("$bumpmap"))
            {
                material.bumpmap = parameters["$bumpmap"];
            }

            if (parameters.ContainsKey("$surfaceprop"))
            {
                material.surfaceprop = parameters["$surfaceprop"];
            }

            if (parameters.ContainsKey("alphatest"))
            {
                if (parameters["alphatest"] == "1")
                {
                    material.alphatest = true;
                }
            }

            if (parameters.ContainsKey("$alphatest"))
            {
                if (parameters["$alphatest"] == "1")
                {
                    material.alphatest = true;
                }
            }

            if (parameters.ContainsKey("$selfillum"))
            {
                if (parameters["$selfillum"] == "1")
                {
                    material.selfillum = true;
                }
            }

            if (parameters.ContainsKey("$translucent"))
            {
                if (parameters["$translucent"] == "1")
                {
                    material.translucent = true;
                }
            }

            if (parameters.ContainsKey("$additive"))
            {
                if (parameters["$additive"] == "1")
                {
                    material.additive = true;
                }
            }

            if (parameters.ContainsKey("$dudvmap"))
            {
                material.dudvmap = parameters["$dudvmap"];
            }

            if (parameters.ContainsKey("$normalmap"))
            {
                material.dudvmap = parameters["$normalmap"];
            }

            return material;
        }
    }
}
