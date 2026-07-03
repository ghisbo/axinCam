using Microsoft.Maui.Handlers;
#if ANDROID
using Android.Content;
using recordCam.Platforms.Android;
using recordCam.Services;

namespace recordCam.Platforms.Android
{
    public partial class CameraViewHandler : ViewHandler<CameraView, CameraPreview>
    {
        public static IPropertyMapper<CameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<CameraView, CameraViewHandler>(ViewHandler.ViewMapper);

        private CancellationTokenSource? _recordingCts;

        /// <summary>
        /// Event fired every countdown interval to report remaining seconds.
        /// </summary>
        public event CountdownProgressEventHandler? CountdownProgress;

        /// <summary>
        /// Event fired when recording has completed successfully with the video file ready.
        /// </summary>
        public event RecordingCompletedEventHandler? RecordingCompleted;

        /// <summary>
        /// Event fired when recording starts (after countdown).
        /// </summary>
        public event RecordingStartedEventHandler? RecordingStarted;

        public CameraViewHandler() : base(PropertyMapper)
        {
        }

        protected override CameraPreview CreatePlatformView()
        {
            return new CameraPreview(Context);
        }

        public void StartRecording(int recordTimeS)
        {
            PlatformView?.StartRecording(recordTimeS);
        }

        public void StopRecording()
        {
            PlatformView?.StopRecording();
        }

        public void StartCountdownBeeping()
        {
            PlatformView?.StartCountdownBeeping();
        }

        public void StopCountdownBeeping()
        {
            PlatformView?.StopCountdownBeeping();
        }

        public void PlayBeep(int durationMs, int repeatCount)
        {
            PlatformView?.PlayBeep(durationMs, repeatCount);
        }

        /// <summary>
        /// Gets the full path of the last recorded video file.
        /// Can be called by external apps after recording completes.
        /// Returns null if no video has been recorded yet.
        /// </summary>
        public string? GetLastRecordedVideoPath()
        {
            return PlatformView?.LastRecordedVideoPath;
        }

        /// <summary>
        /// Starts the complete recording sequence (countdown beeping, recording, and post-recording beep).
        /// Can be called from any external app button/event.
        /// Fires CountdownProgress, RecordingStarted, and RecordingCompleted events.
        /// </summary>
        private bool _isRecording = false;

        public async void StartRecordingSequence()
        {
            _recordingCts = new CancellationTokenSource();
            var camRecorder = CamRecorder.Instance;
            _isRecording = false;

            try
            {
                // Start countdown beeping
                StartCountdownBeeping();

                // Countdown with progress updates every BeepRepeatTimeMs
                int totalCountdownMs = (camRecorder.PreRecordTimeS - 2) * 1000;
                int intervalMs = camRecorder.BeepRepeatTimeMs > 0 ? camRecorder.BeepRepeatTimeMs : 1000; // Default 1 second if beeping disabled
                
                for (int elapsedMs = 0; elapsedMs < totalCountdownMs; elapsedMs += intervalMs)
                {
                    // Calculate remaining seconds
                    int remainingMs = totalCountdownMs - elapsedMs;
                    int remainingSeconds = (remainingMs + 999) / 1000; // Round up
                    
                    // Fire countdown progress event
                    OnCountdownProgress(new CountdownProgressEventArgs(remainingSeconds));

                    // Wait for next interval
                    int delayMs = System.Math.Min(intervalMs, totalCountdownMs - elapsedMs);
                    await Task.Delay(delayMs, _recordingCts.Token);
                }

                // Play double beep before recording starts
                PlayBeep(100, 2);

                // Small delay for the beep to complete
                await Task.Delay(300, _recordingCts.Token);

                // Start recording
                StartRecording(camRecorder.RecordTimeS);
                _isRecording = true;

                // Fire RecordingStarted event
                OnRecordingStarted(new RecordingStartedEventArgs(camRecorder.RecordTimeS));

                // Wait for recording to complete (or be stopped early)
                await Task.Delay(camRecorder.RecordTimeS * 1000, _recordingCts.Token);

                // Stop recording (which plays triple beep)
                StopRecording();
                _isRecording = false;

                // Fire RecordingCompleted event with success
                var videoPath = GetLastRecordedVideoPath();
                OnRecordingCompleted(new RecordingCompletedEventArgs(
                    videoPath ?? "Unknown",
                    success: true,
                    errorMessage: null
                ));
            }
            catch (OperationCanceledException)
            {
                // If already recording, user clicked Stop button - save the file
                if (_isRecording)
                {
                    Logger.WriteDebug("Recording stopped early by user");
                    StopRecording();
                    _isRecording = false;
                    var videoPath = GetLastRecordedVideoPath();
                    OnRecordingCompleted(new RecordingCompletedEventArgs(
                        videoPath ?? "Unknown",
                        success: true,
                        errorMessage: null
                    ));
                }
                else
                {
                    // Cancelled during countdown
                    Logger.WriteDebug("Recording cancelled during countdown");
                    StopRecording();
                    _isRecording = false;
                    OnRecordingCompleted(new RecordingCompletedEventArgs(
                        GetLastRecordedVideoPath() ?? "Unknown",
                        success: false,
                        errorMessage: "Recording was cancelled"
                    ));
                }
            }
            catch (System.Exception ex)
            {
                Logger.WriteDebug($"Error in StartRecordingSequence: {ex.Message}");
                StopRecording();
                _isRecording = false;
                OnRecordingCompleted(new RecordingCompletedEventArgs(
                    GetLastRecordedVideoPath() ?? "Unknown",
                    success: false,
                    errorMessage: ex.Message
                ));
            }
        }

        /// <summary>
        /// Cancels the currently running recording sequence.
        /// </summary>
        public void CancelRecordingSequence()
        {
            Logger.WriteDebug("CancelRecordingSequence called - stopping countdown beeping");
            StopCountdownBeeping();
            _recordingCts?.Cancel();
        }

        /// <summary>
        /// Raises the CountdownProgress event.
        /// </summary>
        protected virtual void OnCountdownProgress(CountdownProgressEventArgs e)
        {
            Logger.WriteDebug($"OnCountdownProgress: {e.RemainingSeconds} sec, Subscribers={CountdownProgress?.GetInvocationList().Length ?? 0}");
            CountdownProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the RecordingStarted event.
        /// </summary>
        protected virtual void OnRecordingStarted(RecordingStartedEventArgs e)
        {
            Logger.WriteDebug($"OnRecordingStarted: Duration={e.RecordingDurationSeconds}s, Subscribers={RecordingStarted?.GetInvocationList().Length ?? 0}");
            RecordingStarted?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the RecordingCompleted event.
        /// </summary>
        protected virtual void OnRecordingCompleted(RecordingCompletedEventArgs e)
        {
            Logger.WriteDebug($"OnRecordingCompleted: Success={e.Success}, Subscribers={RecordingCompleted?.GetInvocationList().Length ?? 0}");
            RecordingCompleted?.Invoke(this, e);
        }
    }
}
#endif