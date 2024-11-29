using NextChat.Core;

namespace NextChat.Default;

public class DefaultOptionCreator : IOptionCreator
{
    public void CreateBoolOption(string Title, bool value, Action<bool> Set)
    {
    }

    public void CreateIntOption(string Title, int value, Action<int> Set, params int[] Values)
    {
    }
}