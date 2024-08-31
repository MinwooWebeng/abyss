using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
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
                var mat = field.GetValue(this) as UnityEngine.Material;
                _rumtime_map[field.Name] = mat;
            }
            else if (field.FieldType == typeof(string[]))
            {
                var shader_name = field.Name[..^6];
                var mat = _rumtime_map[shader_name];
                var param_names = field.GetValue(this) as string[];

                var id_map = new Dictionary<string, int>();
                var propertyCount = ShaderUtil.GetPropertyCount(mat.shader);
                for (int i = 0; i < propertyCount && i < param_names.Length; i++)
                {
                    string propertyName = ShaderUtil.GetPropertyName(mat.shader, i);
                    int propertyID = Shader.PropertyToID(propertyName);
                    //Debug.Log($"Property ID: {propertyID} - Name: {propertyName}");

                    id_map[param_names[i]] = propertyID;
                }

                _parameter_id_maps[shader_name] = id_map;
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
