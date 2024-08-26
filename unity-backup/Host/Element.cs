using UnityEngine;

namespace AbyssEngine
{
    internal class Element
    {
        public Element(GameObject gameObject)
        {
            GameObject = gameObject;
        }
        public GameObject GameObject { get; }
    }
}
