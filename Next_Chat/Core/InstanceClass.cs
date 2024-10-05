namespace Next_Chat.Core;

public class InstanceClass<T> where T : InstanceClass<T>, new()
{
    protected static T? _Instance;

    public static T Instance => _Instance ??= new T();
}