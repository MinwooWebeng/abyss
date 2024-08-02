// See https://aka.ms/new-console-template for more information
using AbyssCLI;
using System.Text.Json;
using System.Threading;

Console.WriteLine(AbyssLib.GetVersion());
var hostA = new AbyssLib.AbyssHost("mallang_host_A");
Console.WriteLine(hostA.LocalAddr());

var hostB = new AbyssLib.AbyssHost("mallang_host_B");
Console.WriteLine(hostB.LocalAddr());

var hostC = new AbyssLib.AbyssHost("mallang_host_C");
Console.WriteLine(hostC.LocalAddr());

var hostD = new AbyssLib.AbyssHost("mallang_host_D");
Console.WriteLine(hostD.LocalAddr());

var A_th = new Thread(() =>
{
    while (true)
    {
        var ev_a = hostA.WaitANDEvent();
        Console.WriteLine("A: " + JsonSerializer.Serialize(ev_a));
    }
});
A_th.Start();
var B_th = new Thread(() =>
{
    while (true)
    {
        var ev_b = hostB.WaitANDEvent();
        Console.WriteLine("B: " + JsonSerializer.Serialize(ev_b));
    }
});
B_th.Start();
var C_th = new Thread(() =>
{
    while (true)
    {
        var ev_c = hostC.WaitANDEvent();
        Console.WriteLine("C: " + JsonSerializer.Serialize(ev_c));
    }
});
C_th.Start();
var D_th = new Thread(() =>
{
    while (true)
    {
        var ev_d = hostD.WaitANDEvent();
        Console.WriteLine("D: " + JsonSerializer.Serialize(ev_d));
    }
});
D_th.Start();
Console.WriteLine("----");

hostA.OpenWorld("/", "https://www.abysseum.com/");
Thread.Sleep(1000);

hostA.RequestConnect(hostB.LocalAddr());
hostB.Join("/", hostA.LocalAddr());
Thread.Sleep(100);

hostB.RequestConnect(hostC.LocalAddr());
hostC.Join("/", hostB.LocalAddr());
Thread.Sleep(100);

hostC.RequestConnect(hostD.LocalAddr());
hostD.Join("/", hostC.LocalAddr());

Thread.Sleep(2000);

hostA.CloseWorld("/");

Thread.Sleep(20000);