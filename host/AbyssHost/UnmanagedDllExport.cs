using System.Runtime.InteropServices;
using System.Text;

public class Native
{
    [UnmanagedCallersOnly(EntryPoint = "get_version")]
    public static unsafe char* GetVersion() //this has memory leak, but negligible.
    {
        var version_utf8 = Encoding.UTF8.GetBytes(AbyssHost.Host.Version());

        fixed(void* p = version_utf8)
        {
            var unmanaged_buf = NativeMemory.Alloc((nuint)(version_utf8.Length + 1));
            NativeMemory.Copy(p, unmanaged_buf, (nuint)version_utf8.Length);

            return (char*)unmanaged_buf;
        }
    }
}