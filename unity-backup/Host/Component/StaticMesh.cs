using Dummiesman;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using UnityEngine;

namespace AbyssEngine.Component
{
    internal class StaticMesh : IComponent
    {
        public StaticMesh(AbyssCLI.ABI.File arg, GameObject holder_parent, string editorName)
        {
            _mmap_file = MemoryMappedFile.OpenExisting(
                arg.MmapName,
                MemoryMappedFileRights.Read
            );
            var file_stream = _mmap_file.CreateViewStream(arg.Off, arg.Len);

            //External dependenty: Dummiesman's runtime obj importer
            UnityGameObject = new OBJLoader().Load(file_stream);
            UnityGameObject.name = editorName;
            UnityGameObject.transform.SetParent(holder_parent.transform);

            _mat_slots = new();
            void iterator(GameObject GO)
            {
                //head-first.
                var mesh_renderers = GO.GetComponents<MeshRenderer>();
                foreach (var renderer in mesh_renderers)
                {
                    for (int i = 0; i<renderer.materials.Length; i++)
                    {
                        _mat_slots.Add(new Tuple<MeshRenderer, int>(renderer, i));
                    }
                }

                for (int i = 0; i < GO.transform.childCount; i++)
                {
                    iterator(GO.transform.GetChild(i).gameObject);
                }
            }
            iterator(UnityGameObject);
            
            _instances = new();
        }
        public void InstantiateTracked(GameObject parent)
        {
            var instance = GameObject.Instantiate(UnityGameObject, parent.transform);
            _instances.Add(instance);
        }
        public void SetMaterial(int slot, Material material)
        {
            var target = _mat_slots[slot];
            var prev_materials = target.Item1.materials;
            prev_materials[target.Item2] = material.UnityMaterial;
            target.Item1.SetMaterials(prev_materials.ToList());
        }
        public void Dispose()
        {
            _instances.Clear();
            _mat_slots.Clear();
            GameObject.Destroy(UnityGameObject);
            _mmap_file.Dispose();
        }

        public GameObject UnityGameObject { get; private set; }
        private readonly MemoryMappedFile _mmap_file;
        private List<Tuple<MeshRenderer, int>> _mat_slots;
        private HashSet<GameObject> _instances;
    }
}
