// See https://aka.ms/new-console-template for more information
using AbyssCLI;
using AbyssCLI.ABI;
using System.Text.Json;
class Program
{
    static void Main(string[] _)
    {
        var cout = Console.OpenStandardOutput();
        var protobuf_out = new Google.Protobuf.CodedOutputStream(cout);
        new RenderAction()
        {
            CreateElement = new RenderAction.Types.CreateElement
            {
                ParentId = 2323222,
                ElementId = 7777
            }
        }.WriteTo(protobuf_out);
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
