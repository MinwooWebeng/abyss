using AbyssCLI.ABI;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;

public class Executor : MonoBehaviour
{
    [SerializeField]
    private string local_hash = "mallang";
    [SerializeField]
    private string h3_root_dir;
    [SerializeField]
    private GameObject objHolder;
    [SerializeField]
    private CommonShaderLoader commonShaderLoader;
    [SerializeField]
    private int stepLimit;
    [SerializeField]
    private int currentStep;
    [SerializeField]
    private bool executeActions;

    public void MoveWorld(string url)
    {
        _abyss_host.CallFunc.MoveWorld(url);
    }

    void OnEnable()
    {
        _abyss_host = new AbyssEngine.Host(local_hash, h3_root_dir);

        _game_objects = new();
        _components = new();

        //root and nil-root object
        var nil_root = new GameObject("-1");
        var root = new GameObject("0");
        _game_objects[-1] = nil_root;
        _game_objects[0] = root;

        nil_root.SetActive(false);
    }
    void OnDisable()
    {
        GameObject.Destroy(_game_objects[0]);
        GameObject.Destroy(_game_objects[-1]);

        foreach (var comp in _components)
        {
            comp.Value.Dispose();
        }

        _components = null;
        _game_objects = null;

        _abyss_host.Close();
        _abyss_host = null;

        //field reset
        currentStep = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!executeActions) return;

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        int i = 0;
        while (currentStep < stepLimit && _abyss_host.TryPopRenderAction(out RenderAction render_action))
        {
            i++;
            currentStep++;

            try
            {
                switch (render_action.InnerCase)
                {
                    case RenderAction.InnerOneofCase.CreateElement:
                        CreateElement(render_action.CreateElement);
                        break;
                    case RenderAction.InnerOneofCase.MoveElement:
                        MoveElement(render_action.MoveElement);
                        break;
                    case RenderAction.InnerOneofCase.DeleteElement:
                        DeleteElement(render_action.DeleteElement);
                        break;
                    case RenderAction.InnerOneofCase.ElemSetPos:
                        ElemSetPos(render_action.ElemSetPos);
                        break;
                    case RenderAction.InnerOneofCase.CreateImage:
                        CreateImage(render_action.CreateImage);
                        break;
                    case RenderAction.InnerOneofCase.DeleteImage:
                        DeleteImage(render_action.DeleteImage);
                        break;
                    case RenderAction.InnerOneofCase.CreateMaterialV:
                        CreateMaterialV(render_action.CreateMaterialV);
                        break;
                    case RenderAction.InnerOneofCase.CreateMaterialF:
                        CreateMaterialF(render_action.CreateMaterialF);
                        break;
                    case RenderAction.InnerOneofCase.MaterialSetParamV:
                        MaterialSetParamV(render_action.MaterialSetParamV);
                        break;
                    case RenderAction.InnerOneofCase.MaterialSetParamC:
                        MaterialSetParamC(render_action.MaterialSetParamC);
                        break;
                    case RenderAction.InnerOneofCase.DeleteMaterial:
                        DeleteMaterial(render_action.DeleteMaterial);
                        break;
                    case RenderAction.InnerOneofCase.CreateStaticMesh:
                        CreateStaticMesh(render_action.CreateStaticMesh);
                        break;
                    case RenderAction.InnerOneofCase.StaticMeshSetMaterial:
                        StaticMeshSetMaterial(render_action.StaticMeshSetMaterial);
                        break;
                    case RenderAction.InnerOneofCase.ElemAttachStaticMesh:
                        ElemAttachStaticMesh(render_action.ElemAttachStaticMesh);
                        break;
                    case RenderAction.InnerOneofCase.DeleteStaticMesh:
                        DeleteStaticMesh(render_action.DeleteStaticMesh);
                        break;
                    case RenderAction.InnerOneofCase.CreateAnimation:
                        CreateAnimation(render_action.CreateAnimation);
                        break;
                    case RenderAction.InnerOneofCase.DeleteAnimation:
                        DeleteAnimation(render_action.DeleteAnimation);
                        break;
                    default:
                        Debug.LogError("Executor: invalid RenderAction");
                        return;
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                executeActions = false;
                break;
            }

            var execution_time_mS = stopwatch.Elapsed.TotalMilliseconds;
            if (execution_time_mS > 5)
            {
                break;
            }
        }
        if (i != 0)
            Debug.Log("executed " + i + " calls, " + _abyss_host.GetLeftoverRenderActionCount() + " remaining");
    }
    void FixedUpdate()
    {
        if (_abyss_host.TryPopException(out Exception e))
        {
            Debug.Log(e.Message + "\nstacktrace: " + e.StackTrace);
        }
    }

    private void CreateElement(RenderAction.Types.CreateElement args)
    {
        GameObject newGO = new(args.ElementId.ToString());
        newGO.transform.SetParent(_game_objects[args.ParentId].transform, false);
        _game_objects[args.ElementId] = newGO;
    }
    private void MoveElement(RenderAction.Types.MoveElement args)
    {
        _game_objects[args.ElementId].transform.SetParent(_game_objects[args.NewParentId].transform, true);
    }
    private void DeleteElement(RenderAction.Types.DeleteElement args)
    {
        GameObject.Destroy(_game_objects[args.ElementId]);
        _game_objects.Remove(args.ElementId);
    }
    private void ElemSetPos(RenderAction.Types.ElemSetPos args)
    {
        _game_objects[args.ElementId].transform.position = new Vector3((float)args.Pos.X, (float)args.Pos.Y, (float)args.Pos.Z);
    }
    private void CreateImage(RenderAction.Types.CreateImage args)
    {
        _components[args.ImageId] = new AbyssEngine.Component.Image(args.File);
    }
    private void DeleteImage(RenderAction.Types.DeleteImage args)
    {
        DeleteComponent(args.ImageId);
    }
    private void CreateMaterialV(RenderAction.Types.CreateMaterialV args)
    {
        _components[args.MaterialId] = new AbyssEngine.Component.Material(
            commonShaderLoader.Get(args.ShaderName),
            commonShaderLoader.GetParameterIDMap(args.ShaderName)
        );
    }
    private void CreateMaterialF(RenderAction.Types.CreateMaterialF args)
    {
        throw new NotImplementedException();
    }
    private void MaterialSetParamV(RenderAction.Types.MaterialSetParamV args)
    {
        var mat = _components[args.MaterialId] as AbyssEngine.Component.Material;
        switch (args.Param.ValCase)
        {
            case AnyVal.ValOneofCase.Int:
                mat.UnityMaterial.SetInteger(args.ParamName, args.Param.Int);
                break;
            case AnyVal.ValOneofCase.Double:
                mat.UnityMaterial.SetFloat(args.ParamName, (float)args.Param.Double);
                break;
            case AnyVal.ValOneofCase.Vec2:
                mat.UnityMaterial.SetVector(args.ParamName, new UnityEngine.Vector2((float)args.Param.Vec2.X, (float)args.Param.Vec2.Y));
                break;
            case AnyVal.ValOneofCase.Vec3:
                mat.UnityMaterial.SetVector(args.ParamName, new UnityEngine.Vector3((float)args.Param.Vec3.X, (float)args.Param.Vec3.Y, (float)args.Param.Vec3.Z));
                break;
            case AnyVal.ValOneofCase.Quat:
                mat.UnityMaterial.SetVector(args.ParamName, new UnityEngine.Vector4((float)args.Param.Quat.A, (float)args.Param.Quat.B, (float)args.Param.Quat.C, (float)args.Param.Quat.D));
                break;
            default:
                throw new NotImplementedException();
        }
    }
    private void MaterialSetParamC(RenderAction.Types.MaterialSetParamC args)
    {
        var mat = _components[args.MaterialId] as AbyssEngine.Component.Material;
        var comp = _components[args.ComponentId];
        switch (comp)
        {
            case AbyssEngine.Component.Image image:
                mat.SetTexture(args.ParamName, image);
                break;
            default:
                throw new NotImplementedException();
        }
    }
    private void DeleteMaterial(RenderAction.Types.DeleteMaterial args)
    {
        DeleteComponent(args.MaterialId);
    }
    private void CreateStaticMesh(RenderAction.Types.CreateStaticMesh args)
    {
        _components[args.MeshId] = new AbyssEngine.Component.StaticMesh(args.File, objHolder, "C" + args.MeshId.ToString());
    }
    private void StaticMeshSetMaterial(RenderAction.Types.StaticMeshSetMaterial args) 
    {
        var mesh = _components[args.MeshId] as AbyssEngine.Component.StaticMesh;
        var mat = _components[args.MaterialId] as AbyssEngine.Component.Material;
        mesh.SetMaterial(args.MaterialSlot, mat);
    }
    private void ElemAttachStaticMesh(RenderAction.Types.ElemAttachStaticMesh args)
    {
        var parent = _game_objects[args.ElementId];
        var mesh = _components[args.MeshId] as AbyssEngine.Component.StaticMesh;
        mesh.InstantiateTracked(parent);
    }
    private void DeleteStaticMesh(RenderAction.Types.DeleteStaticMesh args) {
        DeleteComponent(args.MeshId);
    }
    private void CreateAnimation(RenderAction.Types.CreateAnimation args) { }
    private void DeleteAnimation(RenderAction.Types.DeleteAnimation args) { }


    //others
    private void DeleteComponent(int component_id)
    {
        _components[component_id].Dispose();
        _components.Remove(component_id);
    }

    private AbyssEngine.Host _abyss_host;
    private Dictionary<int, GameObject> _game_objects;
    private Dictionary<int, AbyssEngine.Component.IComponent> _components;
}