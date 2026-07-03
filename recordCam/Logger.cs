using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace recordCam
{
    public class Logger
    {
        /// <summary>
        /// Log level enumeration for controlling output verbosity
        /// </summary>
        public enum LogLevel
        {
            /// <summary>No logging output</summary>
            None = 0,
            /// <summary>Only error messages</summary>
            Error = 1,
            /// <summary>Error and warning messages</summary>
            Warning = 2,
            /// <summary>Error, warning, and info messages</summary>
            Info = 3,
            /// <summary>All messages including debug</summary>
            All = 4
        }

        /// <summary>
        /// Current log level - control what gets logged
        /// Default is All for full visibility during development
        /// </summary>
        public static LogLevel CurrentLevel { get; set; } = LogLevel.All;

        /// <summary>
        /// Log a debug message (lowest priority, most verbose)
        /// Only shown when CurrentLevel >= All
        /// </summary>
        public static void WriteDebug(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (CurrentLevel >= LogLevel.All)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string formattedMessage = $"[DEBUG] [--- {memberName} - Lijn {lineNumber}] {message}";
                Debug.WriteLine(formattedMessage);
            }
        }

        /// <summary>
        /// Log an info message
        /// Only shown when CurrentLevel >= Info
        /// </summary>
        public static void WriteInfo(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            if (CurrentLevel >= LogLevel.Info)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string formattedMessage = $"[INFO] [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
                Debug.WriteLine(formattedMessage);
            }
        }

        /// <summary>
        /// Log a warning message
        /// Only shown when CurrentLevel >= Warning
        /// </summary>
        public static void WriteWarning(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            if (CurrentLevel >= LogLevel.Warning)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string formattedMessage = $"[WARNING] [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
                Debug.WriteLine(formattedMessage);
            }
        }

        /// <summary>
        /// Log an error message (highest priority)
        /// Only shown when CurrentLevel >= Error
        /// </summary>
        public static void WriteError(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            if (CurrentLevel >= LogLevel.Error)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string formattedMessage = $"[ERROR] [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
                Debug.WriteLine(formattedMessage);
            }
        }

        /// <summary>
        /// Log a success message
        /// Only shown when CurrentLevel >= Info
        /// </summary>
        public static void WriteSuccess(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            if (CurrentLevel >= LogLevel.Info)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string formattedMessage = $"[SUCCESS] [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
                Debug.WriteLine(formattedMessage);
            }
        }
    }
}
