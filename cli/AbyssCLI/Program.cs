// See https://aka.ms/new-console-template for more information
using AbyssCLI;
using AbyssCLI.ABI;
using System.Text.Json;
using System.Text;
using Google.Protobuf;
using System.IO.MemoryMappedFiles;
class Program
{
    static void WriteRenderAction()
    {

    }
    static void Main(string[] _)
    {
        var cout_writer = new RenderActionWriter(
            Console.OpenStandardOutput()
        );
        cout_writer.CreateElement(0, 1);    //1

        byte[] fileBytes = System.IO.File.ReadAllBytes("carrot.png");
        MemoryMappedFile mmf = MemoryMappedFile.CreateNew("abysscli/static/carrot", fileBytes.Length);
        var accessor = mmf.CreateViewAccessor();
        accessor.WriteArray(0, fileBytes, 0, fileBytes.Length);
        accessor.Flush();
        cout_writer.CreateImage(0, new AbyssCLI.ABI.File()  //2
        {
            Mime = MIME.ImagePng,
            MmapName = "abysscli/static/carrot",
            Off = 0,
            Len = (uint)fileBytes.Length,
        });

        cout_writer.CreateMaterialV(1, "diffuse");  //3
        cout_writer.MaterialSetParamC(1, "albedo", 0);  //4

        byte[] fileBytes2 = System.IO.File.ReadAllBytes("carrot.obj");
        MemoryMappedFile mmf2 = MemoryMappedFile.CreateNew("abysscli/static/carrot_mesh", fileBytes2.Length);
        var accessor2 = mmf2.CreateViewAccessor();
        accessor2.WriteArray(0, fileBytes2, 0, fileBytes2.Length);
        accessor2.Flush();
        cout_writer.CreateStaticMesh(2, new AbyssCLI.ABI.File() //5
        {
            Mime = MIME.ModelObj,
            MmapName = "abysscli/static/carrot_mesh",
            Off = 0,
            Len = (uint)fileBytes2.Length,
        });

        cout_writer.StaticMeshSetMaterial(2, 0, 1); //6
        cout_writer.ElemAttachStaticMesh(1, 2); //7

        Thread.Sleep(5000);
        mmf.Dispose();
        cout_writer.Flush();
    }
    static void Main2(string[] args)
    {
        Dictionary<string, string> parameters = new();

        // Loop through the command-line arguments
        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                int splitIndex = arg.IndexOf('=');
                if (splitIndex > 0)
                {
                    string key = arg[2..splitIndex];
                    string value = arg[(splitIndex + 1)..];
                    parameters[key] = value;
                }
            }
        }

        if (!parameters.TryGetValue("id", out string host_id) || 
            !parameters.TryGetValue("root", out string root_path))
        {
            Console.WriteLine("id: ");
            host_id = Console.ReadLine();

            //D:\WORKS\github\abyss\temp
            Console.WriteLine("root: ");
            root_path = Console.ReadLine();
        }

        if (host_id == null || root_path == null)
        {
            throw new Exception("invalid arguments");
        }

        var this_host = new AbyssLib.AbyssHost(host_id, root_path);
        new Thread(() =>
        {
            while (true)
            {
                var ev_a = this_host.AndWaitEvent();
                Console.WriteLine($"AND: {JsonSerializer.Serialize(ev_a)}");
            }
        }).Start();
        new Thread(() =>
        {
            while (true)
            {
                Console.WriteLine($"Error: {this_host.WaitError()}");
            }
        }).Start();
        new Thread(() =>
        {
            while (true)
            {
                Console.WriteLine($"SOM: {this_host.SomWaitEvent()}");
            }
        }).Start();

        var pre_command = parameters["commands"];
        if (pre_command != null)
        {
            foreach (var s in pre_command.Split(">> "))
            {
                if (s == null || s == "exit")
                {
                    return;
                }
                this_host.ParseAndInvoke(s);
            }
        }

        while (true)
        {
            Console.Write(">> ");
            var call = Console.ReadLine();
            if (call == null || call == "exit")
            {
                return;
            }
            this_host.ParseAndInvoke(call);
        }
    }
}
