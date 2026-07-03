using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace recordCam
{
    public class Logger
    {
        public static void WriteDebug(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            string formattedMessage = $" [--- {memberName} - Lijn {lineNumber}] {message}";
            Debug.WriteLine(formattedMessage);
        }
        public static void WriteInfo(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            string formattedMessage = $" [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
            Debug.WriteLine(formattedMessage);
        }
        public static void WriteError(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            string formattedMessage = $" [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
            Debug.WriteLine(formattedMessage);
        }
        public static void WriteSuccess(string message,
           [CallerFilePath] string filePath = "",
           [CallerMemberName] string memberName = "",
           [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            string formattedMessage = $" [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
            Debug.WriteLine(formattedMessage);
        }
        public static void WriteWarning(string message,
          [CallerFilePath] string filePath = "",
          [CallerMemberName] string memberName = "",
          [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = System.IO.Path.GetFileName(filePath);

            string formattedMessage = $" [--- {fileName} - {memberName} - Lijn {lineNumber}] {message}";
            Debug.WriteLine(formattedMessage);
        }
    }
}
