using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CommonShaderLoader : MonoBehaviour //actually, material
{
    public UnityEngine.Material none;
    public UnityEngine.Material diffuse;
    public string[] diffuse_param;
    //TODO
    //public Shader specular;
    //public Shader bsdf;
    //public Shader transparent;
    //public Shader translucent;

    void OnEnable()
    {
        _rumtime_map = new();
        _parameter_id_maps = new();

        FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(UnityEngine.Material))
            {
                _rumtime_map[field.Name] = field.GetValue(this) as UnityEngine.Material;
            } 
            else if (field.FieldType == typeof(string[]))
            {
                var names = field.GetValue(this) as string[];
                //for (int i = 0; i < names.Length; i++)
                //{
                //    _parameter_id_maps[field.Name + ":" + names[i]] = i;
                //}
            }
        }
    }
    void OnDisable()
    {
        _parameter_id_maps = null;
        _rumtime_map = null;
    }

    public UnityEngine.Material Get(string name)
    {
        if(_rumtime_map.TryGetValue(name, out Material mat))
        {
            return mat;
        }
        return none;
    }
    public Dictionary<string, int> GetParameterIDMap(string name)
    {
        return _parameter_id_maps[name];
    }

    Dictionary<string, UnityEngine.Material> _rumtime_map;
    Dictionary<string, Dictionary<string, int>> _parameter_id_maps;
}
