namespace NextChat.Core;

public interface IActive : IDisposable
{
    public bool _active { get; set; }
    public int _Id { get; set; }
}