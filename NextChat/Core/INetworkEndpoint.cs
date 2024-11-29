namespace NextChat.Core;

public interface INetworkEndpoint
{
    public ConnectionMode Mode { get; init; }
    public void Start();
    public void Send(NextAudioData data);
    public void Stop();
}