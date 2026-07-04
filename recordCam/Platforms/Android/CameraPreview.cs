#if ANDROID
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using recordCam.Services;
using System;

namespace recordCam.Platforms.Android
{
#if ANDROID
    public class CameraPreview : TextureView, TextureView.ISurfaceTextureListener
    {
        private CameraDevice _cameraDevice;
        private CameraCaptureSession _captureSession;
        private CaptureRequest.Builder _previewRequestBuilder;
        private MediaRecorder _mediaRecorder;
        private string? _outputFilePath;
        private bool _isRecordingSessionReady = false;

        // Beeping configuration
        private Handler _beepHandler;
        private BeepRunnable _beepRunnable;

        /// <summary>
        /// Gets the full path of the last recorded video file.
        /// Returns null if no video has been recorded yet.
        /// </summary>
        public string? LastRecordedVideoPath { get; private set; }


        public CameraPreview(Context context) : base(context)
        {
            SurfaceTextureListener = this;
        }

        public async void OnSurfaceTextureAvailable(global::Android.Graphics.SurfaceTexture surface, int width, int height)
        {
            await OpenCamera(width, height);
        }

        public bool OnSurfaceTextureDestroyed(global::Android.Graphics.SurfaceTexture surface)
        {
            CloseCamera();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(global::Android.Graphics.SurfaceTexture surface, int width, int height)
        {
        }

        public void OnSurfaceTextureUpdated(global::Android.Graphics.SurfaceTexture surface)
        {
        }

        public async void StartRecording(int recordTimeS)
        {
            var storageStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            Logger.WriteDebug($"Storage permission: {storageStatus}, Microphone permission: {micStatus}");

            if (storageStatus != PermissionStatus.Granted || micStatus != PermissionStatus.Granted)
            {
                // Handle the case where permission is denied
                return;
            }

            if (_cameraDevice == null) return;

            // Setup MediaRecorder
            _mediaRecorder = new MediaRecorder();

           

            // _mediaRecorder.SetAudioSource(AudioSource.Mic);  // DISABLED: Focus on video only
            _mediaRecorder.SetVideoSource(VideoSource.Surface);
            _mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);

            var moviesDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                global::Android.OS.Environment.DirectoryMovies);
            var camRecorder = CamRecorder.Instance;
            var recordCamDir = new Java.IO.File(moviesDir, camRecorder.VideoFileMap);
            if (!recordCamDir.Exists())
                recordCamDir.Mkdirs();

            var videoFileName = $"swing_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            var videoFile = new Java.IO.File(recordCamDir, videoFileName);
            _outputFilePath = videoFile.AbsolutePath;
            LastRecordedVideoPath = _outputFilePath;  // Store for external apps to access

            _mediaRecorder.SetOutputFile(_outputFilePath);

            // Use the same dimensions as the preview buffer for consistency
            // Validate dimensions before setting
            int videoWidth = camRecorder.PreviewBufferWidth > 0 ? camRecorder.PreviewBufferWidth : 1280;
            int videoHeight = camRecorder.PreviewBufferHeight > 0 ? camRecorder.PreviewBufferHeight : 720;

            // MediaRecorder has strict requirements - both dimensions must be even and reasonable
            if (videoWidth % 2 != 0) videoWidth--;
            if (videoHeight % 2 != 0) videoHeight--;

            // Ensure dimensions are supported by the device (cap at 1920x1080)
            videoWidth = System.Math.Min(videoWidth, 1920);
            videoHeight = System.Math.Min(videoHeight, 1080);

            Logger.WriteDebug($"StartRecording: Setting video size to {videoWidth}x{videoHeight} (buffer was {camRecorder.PreviewBufferWidth}x{camRecorder.PreviewBufferHeight})");

            _mediaRecorder.SetVideoSize(videoWidth, videoHeight);
            _mediaRecorder.SetVideoFrameRate(camRecorder.VideoFrameRate);
            _mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
            _mediaRecorder.SetVideoEncodingBitRate(camRecorder.VideoEncodingBitRate);

            _mediaRecorder.SetMaxDuration(camRecorder.RecordTimeS * 1000);
            Logger.WriteDebug($"StartRecording: maxduration set to {camRecorder.RecordTimeS * 1000}ms");

            // Set orientation hint BEFORE Prepare() - works now with correct timing sequence
            _mediaRecorder.SetOrientationHint((int)camRecorder.Orientation);
            Logger.WriteDebug($"StartRecording: SetOrientationHint set to {(int)camRecorder.Orientation}");

            try
            {
                _mediaRecorder.Prepare();
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"MediaRecorder prepare failed: {ex.Message}");
                _mediaRecorder.Release();
                _mediaRecorder = null;
                return;
            }

            var surfaces = new List<Surface>();
            var texture = SurfaceTexture;
            var previewSurface = new Surface(texture);
            surfaces.Add(previewSurface);

            var recorderSurface = _mediaRecorder.Surface;
            surfaces.Add(recorderSurface);

            _previewRequestBuilder = _cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
            _previewRequestBuilder.AddTarget(previewSurface);
            _previewRequestBuilder.AddTarget(recorderSurface);

            Logger.WriteDebug($"StartRecording: Capture session being created, MediaRecorder.Start() deferred to OnConfigured");
            _cameraDevice.CreateCaptureSession(surfaces, new CameraCaptureStateCallback(this, true), null);

            PlayBeep(300, 1);
        }

        public async Task StopRecordingAsync()
        {
            try
            {
                _mediaRecorder?.Stop();
                Logger.WriteDebug("MediaRecorder.Stop() called - waiting for file flush");
                
                // Give the file time to be fully written to disk (critical!)
                await Task.Delay(500);
                
                _mediaRecorder?.Release();
                _mediaRecorder = null;
                _isRecordingSessionReady = false;
                
                Logger.WriteDebug("MediaRecorder released successfully");
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"StopRecording error: {ex.Message}");
                try
                {
                    _mediaRecorder?.Release();
                    _mediaRecorder = null;
                }
                catch { }
            }
            
            PlayBeep(100, 3);

            // Log file size with filename
            if (!string.IsNullOrEmpty(_outputFilePath))
            {
                try
                {
                    var file = new Java.IO.File(_outputFilePath);
                    long fileSizeBytes = file.Length();
                    long fileSizeKB = fileSizeBytes / 1024;
                    long fileSizeMB = fileSizeKB / 1024;

                    string sizeString = fileSizeMB > 0
                        ? $"{fileSizeMB} MB ({fileSizeKB} KB)"
                        : $"{fileSizeKB} KB";

                    Logger.WriteDebug($"Recording stopped. Video saved to: {_outputFilePath} | Size: {sizeString}");
                }
                catch (Exception ex)
                {
                    Logger.WriteDebug($"Recording stopped. Video saved to: {_outputFilePath} | Error getting file size: {ex.Message}");
                }
            }
            else
            {
                Logger.WriteDebug("Recording stopped. No output file path.");
            }

            CreateCameraPreviewSession();
        }

        public void StopRecording()
        {
            _mediaRecorder?.Stop();
            _mediaRecorder?.Release();
            _mediaRecorder = null;
            _isRecordingSessionReady = false;
            
            PlayBeep(100, 3);

            // Log file size with filename
            if (!string.IsNullOrEmpty(_outputFilePath))
            {
                try
                {
                    var file = new Java.IO.File(_outputFilePath);
                    long fileSizeBytes = file.Length();
                    long fileSizeKB = fileSizeBytes / 1024;
                    long fileSizeMB = fileSizeKB / 1024;

                    string sizeString = fileSizeMB > 0
                        ? $"{fileSizeMB} MB ({fileSizeKB} KB)"
                        : $"{fileSizeKB} KB";

                    Logger.WriteDebug($"Recording stopped. Video saved to: {_outputFilePath} | Size: {sizeString}");
                }
                catch (Exception ex)
                {
                    Logger.WriteDebug($"Recording stopped. Video saved to: {_outputFilePath} | Error getting file size: {ex.Message}");
                }
            }
            else
            {
                Logger.WriteDebug("Recording stopped. No output file path.");
            }

            CreateCameraPreviewSession();
        }

        private async Task OpenCamera(int width, int height)
        {
            var cameraManager = (CameraManager)Context.GetSystemService(Context.CameraService);

            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                // Handle the case where permission is denied
                return;
            }

            string cameraId = cameraManager.GetCameraIdList()[0];
            var characteristics = cameraManager.GetCameraCharacteristics(cameraId);
            var map = (global::Android.Hardware.Camera2.Params.StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var previewSize = map.GetOutputSizes(Java.Lang.Class.FromType(typeof(global::Android.Graphics.SurfaceTexture)))[0];

            cameraManager.OpenCamera(cameraId, new CameraStateCallback(this), null);
            
        }

        private void CloseCamera()
        {
            _captureSession?.Close();
            _captureSession = null;
            _cameraDevice?.Close();
            _cameraDevice = null;
            _mediaRecorder?.Release();
            _mediaRecorder = null;
        }

        private class CameraStateCallback : CameraDevice.StateCallback
        {
            private readonly CameraPreview _owner;

            public CameraStateCallback(CameraPreview owner)
            {
                _owner = owner;
            }

            public override void OnOpened(CameraDevice camera)
            {
                _owner._cameraDevice = camera;
                _owner.CreateCameraPreviewSession();
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                camera.Close();
                _owner._cameraDevice = null;
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close();
                _owner._cameraDevice = null;
            }
        }

        public void PlayBeep(int durationMs = 100, int repeatCount = 1)
        {
            var camRecorder = CamRecorder.Instance;
            
            // If BeepRepeatTimeMs is 0, disable all beeping
            if (camRecorder.BeepRepeatTimeMs == 0)
                return;

            try
            {
                var toneGen = new global::Android.Media.ToneGenerator(global::Android.Media.Stream.Music, 100);
                // Use PropBeep with the requested duration. If Android caps it, so be it.
                // But keep the ToneGenerator alive for the full duration so it doesn't get GC'd early
                toneGen.StartTone(global::Android.Media.Tone.DtmfS, durationMs);
                var handler = new Handler(Looper.MainLooper);
                for (int i = 1; i < repeatCount; i++)
                {
                    handler.PostDelayed(() =>
                    {
                        toneGen.StartTone(global::Android.Media.Tone.DtmfS, durationMs);
                    }, i * (durationMs + 100)); // Increment delay so tones play sequentially
                }
            }
            catch (System.Exception ex)
            {
                Logger.WriteDebug($"Long beep error: {ex.Message}");
            }
        }

        public void StartCountdownBeeping()
        {
            var camRecorder = CamRecorder.Instance;
            
            // If BeepRepeatTimeMs is 0, don't play any beeps
            if (camRecorder.BeepRepeatTimeMs == 0)
            {
                Logger.WriteDebug("Beeping disabled (BeepRepeatTimeMs = 0)");
                return;
            }

            if (_beepHandler == null)
                _beepHandler = new Handler(Looper.MainLooper);

            // Calculate when to stop beeping: preRecordTimeS - 3 seconds
            long stopTimeMs = (camRecorder.PreRecordTimeS - 3) * 1000;
            long startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _beepRunnable = new BeepRunnable(this, startTimeMs, stopTimeMs);
            _beepHandler.Post(_beepRunnable);
        }

        public void StopCountdownBeeping()
        {
            if (_beepHandler != null && _beepRunnable != null)
            {
                _beepHandler.RemoveCallbacks(_beepRunnable);
                Logger.WriteDebug("Countdown beeping stopped.");
            }
        }

        private class BeepRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private readonly CameraPreview _preview;
            private readonly long _startTimeMs;
            private readonly long _stopTimeMs;

            public BeepRunnable(CameraPreview preview, long startTimeMs, long stopTimeMs)
            {
                _preview = preview;
                _startTimeMs = startTimeMs;
                _stopTimeMs = stopTimeMs;
            }

            public void Run()
            {
                long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long elapsedTimeMs = currentTimeMs - _startTimeMs;

                if (elapsedTimeMs < _stopTimeMs)
                {
                    // Play beep
                    _preview.PlayBeep(100, 1);
                    Logger.WriteDebug($"Beep at {elapsedTimeMs / 1000.0:F1} seconds into countdown.");

                    // Schedule next beep
                    if (_preview._beepHandler != null)
                    {
                        var camRecorder = CamRecorder.Instance;
                        _preview._beepHandler.PostDelayed(this, camRecorder.BeepRepeatTimeMs);
                    }
                }
                else
                {
                    _preview.StopCountdownBeeping();
                }

            }
        }

        private void CreateCameraPreviewSession()
        {
            var texture = SurfaceTexture;
            var surface = new Surface(texture);

            _previewRequestBuilder = _cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            _previewRequestBuilder.AddTarget(surface);

            _cameraDevice.CreateCaptureSession(new[] { surface }, new CameraCaptureStateCallback(this, false), null);
        }

        private class CameraCaptureStateCallback : CameraCaptureSession.StateCallback
        {
            private readonly CameraPreview _owner;
            private readonly bool _isRecording;

            public CameraCaptureStateCallback(CameraPreview owner, bool isRecording)
            {
                _owner = owner;
                _isRecording = isRecording;
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                _owner._captureSession = session;
                try
                {
                    if (!_isRecording)
                    {
                        _owner._previewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                        _owner._captureSession.SetRepeatingRequest(_owner._previewRequestBuilder.Build(), null, null);
                        Logger.WriteDebug("OnConfigured: Preview session, SetRepeatingRequest called");
                    }
                    else
                    {
                        // For recording: schedule delayed execution to let MediaRecorder initialize
                        Logger.WriteDebug("OnConfigured: Recording session configured, scheduling SetRepeatingRequest + MediaRecorder.Start() with 500ms delay");
                        var handler = new Handler(Looper.MainLooper);
                        handler.PostDelayed(new StartRecordingRunnable(_owner), 500);
                    }
                }
                catch (CameraAccessException ex)
                {
                    Logger.WriteDebug($"CameraAccessException in OnConfigured: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Logger.WriteDebug($"Exception in OnConfigured: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                Logger.WriteDebug("OnConfigureFailed: Configuration failed.");
                System.Diagnostics.Debug.WriteLine("Configuration failed.");
            }

            private class StartRecordingRunnable : Java.Lang.Object, Java.Lang.IRunnable
            {
                private readonly CameraPreview _owner;

                public StartRecordingRunnable(CameraPreview owner)
                {
                    _owner = owner;
                }

                public void Run()
                {
                    try
                    {
                        Logger.WriteDebug("StartRecordingRunnable.Run: Executing SetRepeatingRequest + MediaRecorder.Start()");
                        
                        if (_owner._captureSession == null)
                        {
                            Logger.WriteDebug("ERROR: _captureSession is null");
                            return;
                        }
                        
                        if (_owner._previewRequestBuilder == null)
                        {
                            Logger.WriteDebug("ERROR: _previewRequestBuilder is null");
                            return;
                        }
                        
                        if (_owner._mediaRecorder == null)
                        {
                            Logger.WriteDebug("ERROR: _mediaRecorder is null");
                            return;
                        }

                        // Set repeating request to deliver frames to recorder surface
                        _owner._captureSession.SetRepeatingRequest(_owner._previewRequestBuilder.Build(), null, null);
                        Logger.WriteDebug("StartRecordingRunnable: SetRepeatingRequest completed");

                        // Start recording - NOW frames will flow
                        _owner._mediaRecorder.Start();
                        _owner._isRecordingSessionReady = true;
                        Logger.WriteDebug("StartRecordingRunnable: MediaRecorder.Start() completed - frames now flowing");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteDebug($"StartRecordingRunnable.Run ERROR: {ex.Message}");
                    }
                }
            }
        }
    }
#else
    public class CameraPreview : Microsoft.Maui.Controls.View
    {
        public CameraPreview(object context) { }
        public void StartRecording(int recordTimeS) { }
        public void StopRecording() { }
    }
#endif
}
#endif
