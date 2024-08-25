using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace V275_REST_lib.Messages;

public enum LoggerMessageTypes
{
    Debug,
    Info,
    Warning,
    Error,
}
public class LoggerMessage : ValueChangedMessage<LoggerMessageTypes>
{
    public DateTime TimeStamp { get; } = DateTime.Now;

    private readonly string message;
    public string Message => Exception != null ? Exception.Message : message;

    public Exception? Exception { get; private set; } = null;
    public bool HasException => Exception != null;

    public LoggerMessage(string message, LoggerMessageTypes type) : base(type) => this.message = message;
    public LoggerMessage(string message) : base(LoggerMessageTypes.Info) => this.message = message;
    public LoggerMessage(Exception exception) : base(LoggerMessageTypes.Error) { Exception = exception; this.message = exception.Message; }
    public LoggerMessage(string message, Exception exception) : base(LoggerMessageTypes.Error) { Exception = exception; this.message = message; }
}
