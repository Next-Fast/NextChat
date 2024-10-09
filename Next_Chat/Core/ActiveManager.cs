namespace Next_Chat.Core;

public class ActiveManager : InstanceClass<ActiveManager>
{
    private readonly List<IActive> _activeList = [];

    public int MaxActive { get; set; } = 10;

    public void OnUpdate()
    {
        if (_activeList.Count < MaxActive) return;
        foreach (var active in _activeList.Where(active => !active._active).ToList())
        {
            active.Dispose();
            _activeList.Remove(active);
        }
    }

    public T? GetActive<T>(int id) where T : class, IActive
    {
        return _activeList.FirstOrDefault(n => n is T && n._Id == id) as T;
    }
}