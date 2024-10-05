namespace Next_Chat.Core;

public interface INetworkEndpoint
{
    public ConnectionMode Mode { get; init; }
    public void Start();
    public void Send();
    public void Receive();
    public void Stop();
}