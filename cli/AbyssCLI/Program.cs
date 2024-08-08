// See https://aka.ms/new-console-template for more information
using AbyssCLI;
using System.Text;
using System.Text.Json;
using System.Threading;
using static AbyssCLI.AbyssLib;
using System;

Console.WriteLine(AbyssLib.GetVersion());
var hostA = new AbyssLib.AbyssHost("mallang_host_A", "D:\\WORKS\\github\\abyss\\temp");
Console.WriteLine(hostA.LocalAddr());

var hostB = new AbyssLib.AbyssHost("mallang_host_B", "D:\\WORKS\\github\\abyss\\temp");
Console.WriteLine(hostB.LocalAddr());

var hostC = new AbyssLib.AbyssHost("mallang_host_C", "D:\\WORKS\\github\\abyss\\temp");
Console.WriteLine(hostC.LocalAddr());

var hostD = new AbyssLib.AbyssHost("mallang_host_D", "D:\\WORKS\\github\\abyss\\temp");
Console.WriteLine(hostD.LocalAddr());

var A_th = new Thread(() =>
{
    while (true)
    {
        var ev_a = hostA.WaitANDEvent();
        Console.WriteLine("A: " + JsonSerializer.Serialize(ev_a));
    }
});
new Thread(() =>
{
    while(true)
    {
        Console.WriteLine("Err[A] " + hostA.WaitError());
    }
}).Start();
A_th.Start();
var B_th = new Thread(() =>
{
    while (true)
    {
        var ev_b = hostB.WaitANDEvent();
        Console.WriteLine("B: " + JsonSerializer.Serialize(ev_b));
    }
});
new Thread(() =>
{
    while (true)
    {
        Console.WriteLine("Err[B] " + hostB.WaitError());
    }
}).Start();
B_th.Start();
var C_th = new Thread(() =>
{
    while (true)
    {
        var ev_c = hostC.WaitANDEvent();
        Console.WriteLine("C: " + JsonSerializer.Serialize(ev_c));
    }
});
new Thread(() =>
{
    while (true)
    {
        Console.WriteLine("Err[C] " + hostC.WaitError());
    }
}).Start();
C_th.Start();
var D_th = new Thread(() =>
{
    while (true)
    {
        var ev_d = hostD.WaitANDEvent();
        Console.WriteLine("D: " + JsonSerializer.Serialize(ev_d));
    }
});
new Thread(() =>
{
    while (true)
    {
        Console.WriteLine("Err[D] " + hostD.WaitError());
    }
}).Start();
D_th.Start();
Console.WriteLine("----");

hostA.OpenWorld("/", "https://www.abysseum.com/");
Thread.Sleep(1000);

hostA.RequestConnect(hostB.LocalAddr());
hostB.Join("/", hostA.LocalAddr());
Thread.Sleep(1000);

hostB.RequestConnect(hostC.LocalAddr());
hostC.Join("/", hostB.LocalAddr());
Thread.Sleep(1000);

hostC.RequestConnect(hostD.LocalAddr());
hostD.Join("/", hostC.LocalAddr());

Thread.Sleep(3000);

//var response = hostB.HttpGet("abyst://mallang_host_D/static/key.pem");
//Console.WriteLine("response(" + response.GetStatus() + ")" + Encoding.UTF8.GetString(response.GetBody()));

//TODO: fix
Console.WriteLine("host A closing world /");
hostA.CloseWorld("/");
Thread.Sleep(5000);

Console.WriteLine("host A joining C world /");
hostA.Join("/", hostC.LocalAddr());