namespace MassTransit.Transports
{
    using System;
    using System.Runtime.CompilerServices;
    using Context;
    using Metadata;
    using Microsoft.Extensions.Logging;
    using Util;


    public static class ReceiveEndpointLoggingExtensions
    {
        static readonly LogMessage<Uri, Guid?, string, string, TimeSpan> _logConsumed = LogContext.Define<Uri, Guid?, string, string, TimeSpan>(LogLevel.Debug,
            "RECEIVE {InputAddress} {MessageId} {MessageType} {ConsumerType}({Duration})");

        static readonly LogMessage<Uri, Guid?, string, string, TimeSpan> _logConsumeFault = LogContext.Define<Uri, Guid?, string, string, TimeSpan>(
            LogLevel.Error,
            "R-FAULT {InputAddress} {MessageId} {MessageType} {ConsumerType}({Duration})");

        static readonly LogMessage<Uri, Guid?, string, string> _logMoved = LogContext.Define<Uri, Guid?, string, string>(LogLevel.Information,
            "MOVE {InputAddress} {MessageId} {DestinationAddress} {Reason}");

        static readonly LogMessage<Uri, Guid?, TimeSpan> _logReceiveFault = LogContext.Define<Uri, Guid?, TimeSpan>(LogLevel.Error,
            "R-FAULT {InputAddress} {MessageId} {Duration}");

        static readonly LogMessage<Uri, Guid?, string> _logSent = LogContext.Define<Uri, Guid?, string>(LogLevel.Debug,
            "SEND {DestinationAddress} {MessageId} {MessageType}");

        static readonly LogMessage<Uri, Guid?> _logSkipped = LogContext.Define<Uri, Guid?>(LogLevel.Debug,
            "SKIP {InputAddress} {MessageId}");

        static LogMessage<Uri, Guid?> _logRetry = LogContext.Define<Uri, Guid?>(LogLevel.Warning,
            "R-RETRY {InputAddress} {MessageId}");

        /// <summary>
        /// Log a skipped message that was moved to the dead-letter queue
        /// </summary>
        /// <param name="context"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogSkipped(this ReceiveContext context)
        {
            _logSkipped(context.InputAddress, GetMessageId(context));
        }

        /// <summary>
        /// Log a moved message from one endpoint to the destination endpoint address
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destination"></param>
        /// <param name="reason"> </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogMoved(this ReceiveContext context, string destination, string reason)
        {
            _logMoved(context.InputAddress, GetMessageId(context), destination, reason);
        }

        /// <summary>
        /// Log a consumed message
        /// </summary>
        /// <param name="context"></param>
        /// <param name="duration"></param>
        /// <param name="consumerType"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogConsumed<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            _logConsumed(context.ReceiveContext.InputAddress, context.MessageId, TypeMetadataCache<T>.ShortName, consumerType, duration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFaulted<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class
        {
            _logConsumeFault(context.ReceiveContext.InputAddress, context.MessageId, TypeMetadataCache<T>.ShortName, consumerType, duration, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFaulted(this ReceiveContext context, Exception exception)
        {
            _logReceiveFault(context.InputAddress, GetMessageId(context), context.ElapsedTime, exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogRetry(this ConsumeContext context, Exception exception)
        {
            _logRetry(context.ReceiveContext.InputAddress, context.MessageId, exception);
        }

        public static void LogRetry<T>(this ConsumeContext<T> context, Exception exception)
            where T : class
        {
            LogContext.Current?.Messages.Warning?.Log("R-RETRY {InputAddress} {MessageId} {MessageType} {Exception}", context.ReceiveContext.InputAddress,
                context.MessageId, TypeMetadataCache<T>.ShortName, GetFaultMessage(exception));
        }

        public static void LogFaulted<T>(this SendContext<T> context, Exception exception)
            where T : class
        {
            LogContext.Current?.Messages.Error?.Log("S-FAULT {DestinationAddress} {MessageId} {MessageType} {Exception}", context.DestinationAddress,
                context.MessageId, TypeMetadataCache<T>.ShortName, GetFaultMessage(exception));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogSent<T>(this SendContext<T> context)
            where T : class
        {
            _logSent(context.DestinationAddress, context.MessageId, TypeMetadataCache<T>.ShortName);
        }

        public static void LogScheduled<T>(this SendContext<T> context, DateTime deliveryTime)
            where T : class
        {
            LogContext.Current?.Messages.Debug?.Log("SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}", context.DestinationAddress,
                context.MessageId, TypeMetadataCache<T>.ShortName, deliveryTime, context.ScheduledMessageId?.ToString("D"));
        }

        static Guid? GetMessageId(ReceiveContext context)
        {
            return context.TransportHeaders.Get<Guid>("MessageId");
        }

        static string GetFaultMessage(Exception exception)
        {
            var baseException = exception.GetBaseException() ?? exception;

            return ExceptionUtil.GetMessage(baseException);
        }
    }
}
