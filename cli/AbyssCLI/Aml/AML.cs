//using System.Reflection;
//using System.Xml;

//namespace AbyssCLI.Aml
//{
//    internal static class AMLParser
//    {
//        public static AML Parse(string aml)
//        {
//            XmlDocument doc = new();
//            doc.LoadXml(aml);

//            // Check for the DOCTYPE
//            if (doc.DocumentType == null || doc.DocumentType.Name != "AML")
//            {
//                throw new Exception("DOCTYPE mismatch");
//            }

//            // Check for one and only one <aml> element
//            XmlNode amlNode = doc.SelectSingleNode("/aml");
//            if (amlNode == null || amlNode.ParentNode != doc)
//            {
//                throw new Exception("<aml> not found");
//            }

//            AML amlResult = new();

//            // Process <head>
//            XmlNode headNode = amlNode.SelectSingleNode("head");
//            if (headNode != null)
//            {
//                foreach (XmlNode child in headNode.ChildNodes)
//                {
//                    if (child.Name is "script" or "meta")
//                    {
//                        amlResult.HeadElements.Add(new AMLHeadElement { Tag = child.Name, Content = child.InnerText });
//                    }
//                    else
//                    {
//                        throw new Exception("Invalid tag in <head>");
//                    }
//                }
//            }

//            // Process <body>
//            amlResult.Body = ParseO(amlNode.SelectSingleNode("body"));
//            return amlResult;
//        }

//        private static AMLMesh ParseMesh(XmlNode node)
//        {
//            AMLMesh mesh = new()
//            {
//                Id = node.Attributes["id"]?.Value,
//                Src = node.Attributes["src"]?.Value,
//                Type = node.Attributes["type"]?.Value,
//                Pos = node.Attributes["pos"]?.Value,
//                Rot = node.Attributes["rot"]?.Value,
//                Mats = [],
//                Colliders = []
//            };

//            foreach (XmlNode child in node.ChildNodes)
//            {
//                if (child.Name == "mat")
//                {
//                    mesh.Mats.Add(ParseMat(child));
//                }
//                else if (child.Name == "collider")
//                {
//                    mesh.Colliders.Add(ParseCollider(child));
//                }
//                else
//                {
//                    throw new Exception("Invalid child in <mesh>");
//                }
//            }

//            return mesh;
//        }

//        private static AMLMat ParseMat(XmlNode node)
//        {
//            AMLMat mat = new()
//            {
//                Id = node.Attributes["id"]?.Value,
//                Src = node.Attributes["src"]?.Value,
//                Shader = node.Attributes["shader"]?.Value,
//                Images = []
//            };

//            foreach (XmlNode child in node.ChildNodes)
//            {
//                if (child.Name == "img")
//                {
//                    mat.Images.Add(ParseImage(child));
//                }
//                else
//                {
//                    throw new Exception("Invalid child in <mat>");
//                }
//            }

//            return mat;
//        }

//        private static AMLImage ParseImage(XmlNode node)
//        {
//            return new AMLImage
//            {
//                Id = node.Attributes["id"]?.Value,
//                Role = node.Attributes["role"]?.Value,
//                Src = node.Attributes["src"]?.Value
//            };
//        }

//        private static AMLCollider ParseCollider(XmlNode node)
//        {
//            return new AMLCollider
//            {
//                Id = node.Attributes["id"]?.Value,
//                Pos = node.Attributes["pos"]?.Value,
//                Rot = node.Attributes["rot"]?.Value,
//                Shape = node.Attributes["shape"]?.Value,
//                ShapeR = node.Attributes["shape_r"]?.Value
//            };
//        }

//        private static AMLO ParseO(XmlNode node)
//        {
//            AMLO o = new()
//            {
//                Id = node.Attributes["id"]?.Value,
//                Pos = node.Attributes["pos"]?.Value,
//                Rot = node.Attributes["rot"]?.Value,
//                Type = node.Attributes["type"]?.Value,
//                BodyElements = []
//            };

//            foreach (XmlNode child in node.ChildNodes)
//            {
//                if (child.Name == "mesh")
//                {
//                    o.BodyElements.Add(ParseMesh(child));
//                }
//                else if (child.Name == "collider")
//                {
//                    o.BodyElements.Add(ParseCollider(child));
//                }
//                else
//                {
//                    throw new Exception("Invalid child in <o>");
//                }
//            }

//            return o;
//        }
//    }
//    internal class AML
//    {
//        public List<AMLHeadElement> HeadElements { get; set; } = [];
//        public AMLO Body { get; set; }
//    }

//    internal class AMLHeadElement
//    {
//        public string Tag { get; set; }
//        public string Content { get; set; }
//    }
//    internal class Selector
//    {
//        public string Id { get; set; }
//    }
//    internal class Locationer : Selector
//    {
//        public string Pos { get; set; }
//        public string Rot { get; set; }
//    }

//    internal class AMLMesh : Locationer
//    {
//        public string Src { get; set; }
//        public string Type { get; set; }
//        public List<AMLMat> Mats { get; set; }
//        public List<AMLCollider> Colliders { get; set; }
//    }

//    internal class AMLMat : Selector
//    {
//        public string Src { get; set; }
//        public string Shader { get; set; }
//        public List<AMLImage> Images { get; set; }
//    }

//    internal class AMLImage : Selector
//    {
//        public string Role { get; set; }
//        public string Src { get; set; }
//    }

//    internal class AMLCollider : Locationer
//    {
//        public string Shape { get; set; }
//        public string ShapeR { get; set; }
//    }

//    internal class AMLO : Locationer
//    {
//        public string Type { get; set; }
//        public List<object> BodyElements { get; set; }
//    }
//}
