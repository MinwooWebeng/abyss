// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

string AbyssGetVersion()
{
    unsafe
    {
        [DllImport("../../../abyssnet.dll")]
        static extern int GetVersion(byte* buf, int buflen);

        fixed (byte* pBytes = new byte[16])
        {
            int len = GetVersion(pBytes, 16);
            if (len < 0)
            {
                return "fail";
            }
            return System.Text.Encoding.UTF8.GetString(pBytes, len);
        }
    }
}

Console.WriteLine(AbyssGetVersion());