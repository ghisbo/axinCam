namespace recordCam.Services
{
    /// <summary>
    /// Arguments for countdown progress event.
    /// </summary>
    public class CountdownProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Remaining seconds in the countdown.
        /// </summary>
        public int RemainingSeconds { get; set; }

        public CountdownProgressEventArgs(int remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }

    /// <summary>
    /// Arguments for recording completion event.
    /// </summary>
    public class RecordingCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to the recorded video file.
        /// </summary>
        public string VideoFilePath { get; set; }

        /// <summary>
        /// True if recording completed successfully, false if cancelled or errored.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if recording failed. Null if successful.
        /// </summary>
        public string? ErrorMessage { get; set; }

        public RecordingCompletedEventArgs(string videoFilePath, bool success, string? errorMessage = null)
        {
            VideoFilePath = videoFilePath;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Arguments for recording started event.
    /// </summary>
    public class RecordingStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Duration of recording in seconds.
        /// </summary>
        public int RecordingDurationSeconds { get; set; }

        public RecordingStartedEventArgs(int recordingDurationSeconds)
        {
            RecordingDurationSeconds = recordingDurationSeconds;
        }
    }

    /// <summary>
    /// Delegate for countdown progress events.
    /// </summary>
    public delegate void CountdownProgressEventHandler(object sender, CountdownProgressEventArgs e);

    /// <summary>
    /// Delegate for recording completion events.
    /// </summary>
    public delegate void RecordingCompletedEventHandler(object sender, RecordingCompletedEventArgs e);

    /// <summary>
    /// Delegate for recording started events.
    /// </summary>
    public delegate void RecordingStartedEventHandler(object sender, RecordingStartedEventArgs e);
}
