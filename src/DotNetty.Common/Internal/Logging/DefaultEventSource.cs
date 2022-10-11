// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal.Logging
{
    using System;
    using System.Diagnostics.Tracing;

    [EventSource(
        Name = "DotNetty-Default",
        Guid = "d079e771-0495-4124-bd2f-ab63c2b50525")]
    public sealed class DefaultEventSource : EventSource
    {
        const int TraceEventId = 1;
        const int DebugEventId = 2;
        const int InfoEventId = 3;
        const int WarningEventId = 4;
        const int ErrorEventId = 5;

        public class Keywords
        {
            public const EventKeywords TraceEventKeyword = (EventKeywords)1;
            public const EventKeywords DebugEventKeyword = (EventKeywords)(1 << 1);
        }

        public static readonly DefaultEventSource Log = new DefaultEventSource();

        DefaultEventSource()
        {
        }

        public bool IsTraceEnabled => true;

        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;

        public bool IsWarningEnabled => true;

        public bool IsErrorEnabled => true;

        [NonEvent]
        public void Trace(string source, string message) => this.Trace(source, message, string.Empty);

        [NonEvent]
        public void Trace(string source, string message, Exception exception)
        {
            if (this.IsTraceEnabled)
            {
                this.Trace(source, message, exception?.ToString() ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");
            }
        }

        [Event(TraceEventId, Level = EventLevel.Verbose, Keywords = Keywords.TraceEventKeyword)]
        public void Trace(string source, string message, string info)
        {
            if (this.IsTraceEnabled)
            {
                this.WriteEvent(TraceEventId, source, message, info);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [NonEvent]
        public void Debug(string source, string message) => this.Debug(source, message, string.Empty);

        [NonEvent]
        public void Debug(string source, string message, Exception exception)
        {
            if (this.IsDebugEnabled)
            {
                this.Debug(source, message, exception?.ToString() ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [Event(DebugEventId, Level = EventLevel.Verbose, Keywords = Keywords.DebugEventKeyword)]
        public void Debug(string source, string message, string info)
        {
            if (this.IsDebugEnabled)
            {
                this.WriteEvent(DebugEventId, source, message, info);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");
            }
        }

        [NonEvent]
        public void Info(string source, string message) => this.Info(source, message, string.Empty);

        [NonEvent]
        public void Info(string source, string message, Exception exception)
        {
            if (this.IsInfoEnabled)
            {
                this.Info(source, message, exception?.ToString() ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [Event(InfoEventId, Level = EventLevel.Informational)]
        public void Info(string source, string message, string info)
        {
            if (this.IsInfoEnabled)
            {
                this.WriteEvent(InfoEventId, source, message, info);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [NonEvent]
        public void Warning(string source, string message) => this.Warning(source, message, string.Empty);

        [NonEvent]
        public void Warning(string source, string message, Exception exception)
        {
            if (this.IsWarningEnabled)
            {
                this.Warning(source, message, exception?.ToString() ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [Event(WarningEventId, Level = EventLevel.Warning)]
        public void Warning(string source, string message, string exception)
        {
            if (this.IsWarningEnabled)
            {
                this.WriteEvent(WarningEventId, source, message, exception);
            }
        }

        [NonEvent]
        public void Error(string source, string message) => this.Error(source, message, string.Empty);

        [NonEvent]
        public void Error(string source, string message, Exception exception)
        {
            if (this.IsErrorEnabled)
            {
                this.Error(source, message, exception?.ToString() ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");

            }
        }

        [Event(ErrorEventId, Level = EventLevel.Error)]
        public void Error(string source, string message, string exception)
        {
            if (this.IsErrorEnabled)
            {
                this.WriteEvent(ErrorEventId, source, message, exception);
                System.Diagnostics.Debug.WriteLine($"бя[{DateTime.Now.ToString()}]: {message}");
            }
        }
    }
}