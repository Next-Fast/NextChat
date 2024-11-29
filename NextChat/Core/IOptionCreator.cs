namespace NextChat.Core;

public interface IOptionCreator
{
    public void CreateBoolOption(string Title, bool DefaultValue, Action<bool> Set);
    
    public void CreateIntOption(string Title, int DefaultValue, Action<int> Set, params int[] Values);
}