global using static NextChat.Core.LogHelper;

namespace NextChat.Core;

public static class LogHelper
{
    public static void LogError(string message) => NextChat.Main.LogSource.LogError(message);
    
    public static void LogInfo(string message) => NextChat.Main.LogSource.LogInfo(message);
    
    public static void LogWarning(string message) => NextChat.Main.LogSource.LogWarning(message);
    
    public static void LogDebug(string message) => NextChat.Main.LogSource.LogDebug(message);
    
    public static void LogFatal(string message) => NextChat.Main.LogSource.LogFatal(message);
    
    public static void LogMessage(string message) => NextChat.Main.LogSource.LogMessage(message);
    
    public static void LogException(Exception ex) => NextChat.Main.LogSource.LogError("Exception Error:\n" + ex);
}