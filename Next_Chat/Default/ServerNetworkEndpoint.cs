using System.Net.Sockets;
using Hazel;
using Hazel.Udp;
using Il2CppSystem.Net;
using InnerNet;
using Next_Chat.Core;

namespace Next_Chat.Default;

public class ServerNetworkEndpoint : INetworkEndpoint
{
    public ConnectionMode Mode { get; init; }
    public string address { get; set; } = string.Empty;
    public int port { get; set; } = 0;
    private HttpClient? _client { get; set; }
    private UnityUdpClientConnection? _udpClient { get; set; }
    private string udpAddress { get; set; } = string.Empty;
    private int udpPort { get; set; } = 0;
    
    private bool started = false;
    public void Start()
    {
        if (address == string.Empty || port == 0) return;
        _client ??= new HttpClient();
        started = true;
    }

    public void Send(NextAudioData data)
    {
        if (!started) 
            return;
    }

    public void Stop()
    {
        _client?.Dispose();
        _client = null;
        started = false;
    }
}