namespace ResourceSourceGenerator;

public abstract class BaseGenerator(string fileDir)
{
    public string FileDir { get; } = fileDir;
}