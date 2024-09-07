using System.Collections.Generic;

namespace AbyssEngine.Component
{
    internal class Material : IComponent
    {
        public Material(UnityEngine.Material baseMaterial, Dictionary<string, int> paramIdMap)
        {
            UnityMaterial = new UnityEngine.Material(baseMaterial);
            _param_id_map = paramIdMap;
        }
        public void SetTexture(string paramName, Image image)
        {
            //UnityEngine.Debug.Log("nameID: " + _param_id_map[paramName]);
            UnityMaterial.SetTexture(_param_id_map[paramName], image.UnityTexture2D);
        }

        public void Dispose()
        {
            UnityMaterial = null;
        }

        public UnityEngine.Material UnityMaterial { get; private set; }
        private readonly Dictionary<string, int> _param_id_map;
    }
}
