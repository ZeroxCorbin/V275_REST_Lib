using NLog;
using System;
using CommunityToolkit.Mvvm.Messaging;

namespace V275_REST_lib.Logging;

public class Logger : ILogger
{
    private static IMessenger Messenger => WeakReferenceMessenger.Default;
    private static NLog.Logger GetLogger(Type sourceType) => LogManager.GetLogger(sourceType.FullName);

    public void LogInfo(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Info(message);
        _ = Messenger.Send(new Messages.LoggerMessage(message, Messages.LoggerMessageTypes.Info));
    }

    public void LogDebug(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Debug(message);
        _ = Messenger.Send(new Messages.LoggerMessage(message, Messages.LoggerMessageTypes.Debug));
    }

    public void LogWarning(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Warn(message);
        _ = Messenger.Send(new Messages.LoggerMessage(message, Messages.LoggerMessageTypes.Warning));
    }

    public void LogError(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Error(message);
        _ = Messenger.Send(new Messages.LoggerMessage(message, Messages.LoggerMessageTypes.Error));
    }

    public void LogError(Type sourceType, Exception ex)
    {
        var logger = GetLogger(sourceType);
        logger.Error(ex);
        _ = Messenger.Send(new Messages.LoggerMessage(ex));
    }

    public void LogError(Type sourceType, string message, Exception ex)
    {
        var logger = GetLogger(sourceType);
        logger.Error(ex, message);
        _ = Messenger.Send(new Messages.LoggerMessage(message, ex));
    }
}
