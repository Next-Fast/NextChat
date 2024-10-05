global using static Next_Chat.Core.LogHelper;
using System;

namespace Next_Chat.Core;

public static class LogHelper
{
    public static void LogError(string message) => Main.LogSource.LogError(message);
    
    public static void LogInfo(string message) => Main.LogSource.LogInfo(message);
    
    public static void LogWarning(string message) => Main.LogSource.LogWarning(message);
    
    public static void LogDebug(string message) => Main.LogSource.LogDebug(message);
    
    public static void LogFatal(string message) => Main.LogSource.LogFatal(message);
    
    public static void LogMessage(string message) => Main.LogSource.LogMessage(message);
    
    public static void LogException(Exception ex) => Main.LogSource.LogError("Exception Error:\n" + ex);
}