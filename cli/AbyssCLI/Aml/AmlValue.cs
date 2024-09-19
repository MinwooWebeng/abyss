#pragma warning disable IDE1006 //naming convention
namespace AbyssCLI.Aml
{
    public struct vec3
    {
        public float x, y, z;
        public vec3()
        {
            x = 0;
            y = 0;
            z = 0;
        }
        public vec3(int v1, int v2, int v3)
        {
            x = v1;
            y = v2;
            z = v3;
        }

        public static vec3 add(vec3 lhs, vec3 rhs)
        {
            return new vec3()
            {
                x = lhs.x + rhs.x,
                y = lhs.y + rhs.y,
                z = lhs.z + rhs.z,
            };
        }
        public static vec3 zero { get; } = new vec3();
    }
    public struct vec4
    {
        public float w, x, y, z;
        public static vec4 zero { get; } = new vec4();
    }

    internal static class AmlValueParser
    {
        public static vec3 ParseVec3(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new vec3();

            // Split the string by commas
            string[] components = input.Split(',', 3);

            // Check if we have exactly 3 components
            if (components.Length != 3)
            {
                return new vec3();
            }

            try
            {
                // Parse each component into a float
                float x = float.Parse(components[0].Trim());
                float y = float.Parse(components[1].Trim());
                float z = float.Parse(components[2].Trim());

                return new vec3() { x = x, y = y, z = z };
            }
            catch
            {
                return new vec3();
            }
        }
        public static vec4 ParseVec4(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new vec4();

            // Split the string by commas
            string[] components = input.Split(',', 4);

            // Check if we have exactly 3 components
            if (components.Length != 4)
            {
                return new vec4();
            }

            try
            {
                // Parse each component into a float
                float w = float.Parse(components[0].Trim());
                float x = float.Parse(components[1].Trim());
                float y = float.Parse(components[2].Trim());
                float z = float.Parse(components[3].Trim());

                return new vec4() { w = w, x = x, y = y, z = z };
            }
            catch
            {
                return new vec4();
            }
        }
    }
}
#pragma warning restore IDE1006