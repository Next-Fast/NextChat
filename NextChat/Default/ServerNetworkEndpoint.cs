using Hazel.Udp;
using NextChat.Core;

namespace NextChat.Default;

public class ServerNetworkEndpoint : INetworkEndpoint
{
    public ConnectionMode Mode { get; init; }
    public string address { get; set; } = string.Empty;
    public int port { get; set; } = 0;
    private HttpClient? _client { get; set; }
    private UnityUdpClientConnection? _udpClient { get; set; }
    private string udpAddress { get; set; } = string.Empty;
    private int udpPort { get; set; } = 0;
    
    private bool started;
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