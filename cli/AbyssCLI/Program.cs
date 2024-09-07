// See https://aka.ms/new-console-template for more information
using AbyssCLI.Client;
class Program
{
    public static async Task Main()
    {
        Client client = new();
        await client.RunAsync();
    }

    //private static UIAction ReadProtoMessage(BinaryReader reader)
    //{
    //    int length = reader.ReadInt32();
    //    if (length <= 0)
    //    {
    //        throw new Exception("invalid length message");
    //    }
    //    byte[] data = reader.ReadBytes(length);
    //    if (data.Length != length)
    //    {
    //        throw new Exception("invalid length message");
    //    }
    //    return UIAction.Parser.ParseFrom(data);
    //}
    //static async void Main()
    //{
    //    var cerr_writer = new StreamWriter(Console.OpenStandardError())
    //    {
    //        AutoFlush = true
    //    };
    //    try
    //    {
    //        var cin_reader = new BinaryReader(Console.OpenStandardInput());
    //        var cout_writer = new RenderActionWriter(
    //            Stream.Synchronized(Console.OpenStandardOutput())
    //        );

    //        //Host Initialization
    //        var init_msg = ReadProtoMessage(cin_reader);
    //        if (init_msg.InnerCase != UIAction.InnerOneofCase.Init)
    //        {
    //            throw new Exception("fatal: host not initialized");
    //        }

    //        var this_host = new AbyssLib.AbyssHost(init_msg.Init.LocalHash, init_msg.Init.Http3RootDir);
    //        _ = Task.Run(() => AndHandleFunc(this_host, cerr_writer));
    //        _ = Task.Run(() => SomHandleFunc(this_host, cerr_writer));
    //        _ = Task.Run(() => ErrorHandleFunc(this_host, cerr_writer));

    //        await Task.Run(() => MainHandleFunc(this_host, cin_reader, cerr_writer, cout_writer));
    //    }
    //    catch (Exception ex)
    //    {
    //        cerr_writer.WriteLine(ex);
    //    }
    //    cerr_writer.Close();
    //}
    //private static void AndHandleFunc(AbyssLib.AbyssHost host, StreamWriter cerr_writer)
    //{
    //    while (true)
    //    {
    //        var ev_a = host.AndWaitEvent();
    //        cerr_writer.Write($"AND: {JsonSerializer.Serialize(ev_a)}");
    //    }
    //}
    //private static void SomHandleFunc(AbyssLib.AbyssHost host, StreamWriter cerr_writer)
    //{
    //    while (true)
    //    {
    //        cerr_writer.Write($"SOM: {host.SomWaitEvent()}");
    //    }
    //}
    //private static void ErrorHandleFunc(AbyssLib.AbyssHost host, StreamWriter cerr_writer)
    //{
    //    while (true)
    //    {
    //        cerr_writer.Write($"Error: {host.WaitError()}");
    //    }
    //}
    //private static async void MainHandleFunc(AbyssLib.AbyssHost host, BinaryReader cin_reader, StreamWriter cerr_writer, RenderActionWriter cout_writer)
    //{
    //    while (true)
    //    {
    //        var message = ReadProtoMessage(cin_reader);
    //        switch (message.InnerCase)
    //        {
    //            case UIAction.InnerOneofCase.Kill:
    //                return; //graceful shutdown
    //            case UIAction.InnerOneofCase.ShareContent:
    //                var world_uuid = message.ShareContent.WorldUuid;
    //                var url = message.ShareContent.Url;
    //                if (url.StartsWith("http"))
    //                {
    //                }
    //                else if (url.StartsWith("abyst"))
    //                {
    //                    Task.Run(() => host.HttpGet(url));
    //                }
    //                else
    //                {
    //                    var aml = await LoadAmlFile(host, url);
    //                }
    //                break;
    //            default:
    //                throw new Exception("fatal: received invalid UI Action");
    //        }
    //    }
    //}
    //private static Task<AmlContent> LoadAmlFile(AbyssLib.AbyssHost host, string url)
    //{
    //    throw new NotImplementedException();
    //}

    //static void Main4(string[] _)
    //{
    //    var cerr_writer = new StreamWriter(Console.OpenStandardError());
    //    cerr_writer.AutoFlush = true;

    //    Console.SetOut(cerr_writer);
    //    if (!AMLParser.TryParse(System.IO.File.ReadAllText("carrot.aml"), out var AML))
    //    {
    //        Console.WriteLine("failed to parse");
    //        return;
    //    }
    //    Console.WriteLine(AML.HeadElements[0].Tag);
    //    Console.WriteLine(AML.HeadElements[0].Content);
    //    Console.Out.Flush();
    //}
    //static void Main3(string[] _)
    //{
    //    var cout_writer = new RenderActionWriter(
    //        Console.OpenStandardOutput()
    //    );
    //    cout_writer.CreateElement(0, 1);    //1

    //    byte[] fileBytes = System.IO.File.ReadAllBytes("carrot.png");
    //    MemoryMappedFile mmf = MemoryMappedFile.CreateNew("abysscli/static/carrot", fileBytes.Length);
    //    var accessor = mmf.CreateViewAccessor();
    //    accessor.WriteArray(0, fileBytes, 0, fileBytes.Length);
    //    accessor.Flush();
    //    cout_writer.CreateImage(0, new AbyssCLI.ABI.File()  //2
    //    {
    //        Mime = MIME.ImagePng,
    //        MmapName = "abysscli/static/carrot",
    //        Off = 0,
    //        Len = (uint)fileBytes.Length,
    //    });

    //    cout_writer.CreateMaterialV(1, "diffuse");  //3
    //    cout_writer.MaterialSetParamC(1, "albedo", 0);  //4

    //    byte[] fileBytes2 = System.IO.File.ReadAllBytes("carrot.obj");
    //    MemoryMappedFile mmf2 = MemoryMappedFile.CreateNew("abysscli/static/carrot_mesh", fileBytes2.Length);
    //    var accessor2 = mmf2.CreateViewAccessor();
    //    accessor2.WriteArray(0, fileBytes2, 0, fileBytes2.Length);
    //    accessor2.Flush();
    //    cout_writer.CreateStaticMesh(2, new AbyssCLI.ABI.File() //5
    //    {
    //        Mime = MIME.ModelObj,
    //        MmapName = "abysscli/static/carrot_mesh",
    //        Off = 0,
    //        Len = (uint)fileBytes2.Length,
    //    });

    //    cout_writer.StaticMeshSetMaterial(2, 0, 1); //6
    //    cout_writer.ElemAttachStaticMesh(1, 2); //7

    //    Thread.Sleep(5000);
    //    mmf.Dispose();
    //    mmf2.Dispose();
    //    cout_writer.Flush();
    //}
    //static void Main2(string[] args)
    //{
    //    Dictionary<string, string> parameters = new();

    //    // Loop through the command-line arguments
    //    foreach (var arg in args)
    //    {
    //        if (arg.StartsWith("--"))
    //        {
    //            int splitIndex = arg.IndexOf('=');
    //            if (splitIndex > 0)
    //            {
    //                string key = arg[2..splitIndex];
    //                string value = arg[(splitIndex + 1)..];
    //                parameters[key] = value;
    //            }
    //        }
    //    }

    //    if (!parameters.TryGetValue("id", out string host_id) || 
    //        !parameters.TryGetValue("root", out string root_path))
    //    {
    //        Console.WriteLine("id: ");
    //        host_id = Console.ReadLine();

    //        //D:\WORKS\github\abyss\temp
    //        Console.WriteLine("root: ");
    //        root_path = Console.ReadLine();
    //    }

    //    if (host_id == null || root_path == null)
    //    {
    //        throw new Exception("invalid arguments");
    //    }

    //    var this_host = new AbyssLib.AbyssHost(host_id, root_path);
    //    new Thread(() =>
    //    {
    //        while (true)
    //        {
    //            var ev_a = this_host.AndWaitEvent();
    //            Console.WriteLine($"AND: {JsonSerializer.Serialize(ev_a)}");
    //        }
    //    }).Start();
    //    new Thread(() =>
    //    {
    //        while (true)
    //        {
    //            Console.WriteLine($"Error: {this_host.WaitError()}");
    //        }
    //    }).Start();
    //    new Thread(() =>
    //    {
    //        while (true)
    //        {
    //            Console.WriteLine($"SOM: {this_host.SomWaitEvent()}");
    //        }
    //    }).Start();

    //    var pre_command = parameters["commands"];
    //    if (pre_command != null)
    //    {
    //        foreach (var s in pre_command.Split(">> "))
    //        {
    //            if (s == null || s == "exit")
    //            {
    //                return;
    //            }
    //            this_host.ParseAndInvoke(s);
    //        }
    //    }

    //    while (true)
    //    {
    //        Console.Write(">> ");
    //        var call = Console.ReadLine();
    //        if (call == null || call == "exit")
    //        {
    //            return;
    //        }
    //        this_host.ParseAndInvoke(call);
    //    }
    //}
}
