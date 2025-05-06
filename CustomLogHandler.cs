using UnityEngine;

public class CustomLogHandler : ILogHandler
{
    private ILogHandler defaultLogHandler = Debug.unityLogger.logHandler;

    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        if (format.Contains("Out of memory")) return; // Belirli hatayý yut
        defaultLogHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(System.Exception exception, Object context)
    {
        if (exception.Message.Contains("Out of memory")) return;
        defaultLogHandler.LogException(exception, context);
    }
}

public class DisableMemoryLogs : MonoBehaviour
{
    void Awake()
    {
        Debug.unityLogger.logHandler = new CustomLogHandler();
    }
}