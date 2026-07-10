using Microsoft.Extensions.Logging;

namespace LichessNET;

internal class Constants
{
    public const string BaseUrl = "https://lichess.org/";

#if DEBUG
    public static LogLevel MinimumLogLevel = LogLevel.Trace;
#else
        public static LogLevel MinimumLogLevel = LogLevel.Information;
#endif
}