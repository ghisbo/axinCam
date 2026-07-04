#if ANDROID
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
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
    /// <summary>
    /// Simplified camera preview and recording using MediaRecorder directly (no Camera2 complexity)
    /// </summary>
    public class CameraPreview : TextureView, TextureView.ISurfaceTextureListener
    {
        private Camera _camera;
        private MediaRecorder _mediaRecorder;
        private string _outputFilePath;
        private bool _isRecording = false;
        private bool _isPreviewing = false;

        // Beeping
        private Handler _beepHandler;
        private BeepRunnable _beepRunnable;

        public string LastRecordedVideoPath { get; private set; }

        public CameraPreview(Context context) : base(context)
        {
            SurfaceTextureListener = this;
            _beepHandler = new Handler(Looper.MainLooper);
        }

        public void OnSurfaceTextureAvailable(global::Android.Graphics.SurfaceTexture surface, int width, int height)
        {
            try
            {
                Logger.WriteDebug($"OnSurfaceTextureAvailable: {width}x{height}");
                StartPreview(surface);
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"OnSurfaceTextureAvailable error: {ex.Message}");
            }
        }

        public bool OnSurfaceTextureDestroyed(global::Android.Graphics.SurfaceTexture surface)
        {
            Logger.WriteDebug("OnSurfaceTextureDestroyed");
            StopPreview();
            return false;
        }

        public void OnSurfaceTextureSizeChanged(global::Android.Graphics.SurfaceTexture surface, int width, int height)
        {
            Logger.WriteDebug($"OnSurfaceTextureSizeChanged: {width}x{height}");
        }

        public void OnSurfaceTextureFrameAvailable(global::Android.Graphics.SurfaceTexture surface)
        {
        }

        public void OnSurfaceTextureUpdated(global::Android.Graphics.SurfaceTexture surface)
        {
        }

        private void StartPreview(global::Android.Graphics.SurfaceTexture surfaceTexture)
        {
            if (_isPreviewing) return;

            try
            {
                // Request camera permission
                var status = Permissions.RequestAsync<Permissions.Camera>().Result;
                if (status != PermissionStatus.Granted)
                {
                    Logger.WriteDebug("Camera permission denied");
                    return;
                }

                // Open back camera (device index 0)
                _camera = Camera.Open(0);
                if (_camera == null)
                {
                    Logger.WriteDebug("Failed to open camera");
                    return;
                }

                // Configure camera
                var parameters = _camera.GetParameters();
                var previewSizes = parameters.SupportedPreviewSizes;
                if (previewSizes != null && previewSizes.Count > 0)
                {
                    var previewSize = previewSizes[0];
                    parameters.SetPreviewSize(previewSize.Width, previewSize.Height);
                    Logger.WriteDebug($"Preview size set to {previewSize.Width}x{previewSize.Height}");
                }

                _camera.SetParameters(parameters);

                // Set camera display orientation to 90 degrees for portrait mode
                _camera.SetDisplayOrientation(90);

                // Set preview surface
                _camera.SetPreviewTexture(surfaceTexture);
                _camera.StartPreview();
                _isPreviewing = true;

                Logger.WriteDebug("Camera preview started");
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"StartPreview error: {ex.Message}");
            }
        }

        private void StopPreview()
        {
            try
            {
                if (_camera != null && _isPreviewing)
                {
                    _camera.StopPreview();
                    _camera.Release();
                    _camera = null;
                    _isPreviewing = false;
                    Logger.WriteDebug("Camera preview stopped");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"StopPreview error: {ex.Message}");
            }
        }

        public void StartRecording(int recordTimeS)
        {
            if (_isRecording || _camera == null) return;

            try
            {
                Logger.WriteDebug($"StartRecording: Preparing MediaRecorder for {recordTimeS}s");

                // Stop preview temporarily
                _camera.StopPreview();

                // CRITICAL: Unlock camera before MediaRecorder can use it
                _camera.Unlock();

                // Create MediaRecorder
                _mediaRecorder = new MediaRecorder();
                // Set camera FIRST after unlock
                _mediaRecorder.SetCamera(_camera);
                // _mediaRecorder.SetAudioSource(AudioSource.Mic);  // Audio disabled - focus on video
                _mediaRecorder.SetVideoSource(VideoSource.Camera);
                _mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
                // _mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);  // No audio encoder - audio disabled
                _mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
                _mediaRecorder.SetVideoSize(1280, 720);
                _mediaRecorder.SetVideoFrameRate(30);
                // _mediaRecorder.SetAudioSamplingRate(44100);  // No audio - disabled
                _mediaRecorder.SetVideoEncodingBitRate(1024*1024*2); // 2Mbps
                var camRecorder = CamRecorder.Instance;
                _mediaRecorder.SetMaxDuration(camRecorder.RecordTimeS * 1000);
                _mediaRecorder.SetOrientationHint((int)camRecorder.Orientation);

                // Output file
                var moviesDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                    global::Android.OS.Environment.DirectoryMovies);
                var recordCamDir = new Java.IO.File(moviesDir, camRecorder.VideoFileMap);
                if (!recordCamDir.Exists())
                    recordCamDir.Mkdirs();

                var videoFileName = $"swing_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                var videoFile = new Java.IO.File(recordCamDir, videoFileName);
                _outputFilePath = videoFile.AbsolutePath;
                LastRecordedVideoPath = _outputFilePath;

                _mediaRecorder.SetOutputFile(_outputFilePath);

                Logger.WriteDebug($"StartRecording: Preparing MediaRecorder with output: {_outputFilePath}");
                _mediaRecorder.Prepare();

                Logger.WriteDebug("StartRecording: Starting MediaRecorder");
                _mediaRecorder.Start();
                _isRecording = true;

                Logger.WriteDebug("StartRecording: MediaRecorder started successfully");
                PlayBeep(300, 1);
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"StartRecording error: {ex.Message}");
                CleanupMediaRecorder();
                try
                {
                    if (_camera != null)
                        _camera.StartPreview();
                }
                catch { }
            }
        }

        public async Task StopRecordingAsync()
        {
            if (!_isRecording) return;

            try
            {
                Logger.WriteDebug("StopRecording: Stopping MediaRecorder");
                _mediaRecorder?.Stop();
                
                // Wait for file to be flushed
                await Task.Delay(500);

                CleanupMediaRecorder();
                _isRecording = false;

                // Re-lock camera after MediaRecorder releases it
                if (_camera != null)
                {
                    _camera.Lock();
                    Logger.WriteDebug("Camera re-locked after recording");
                    
                    _camera.StartPreview();
                    Logger.WriteDebug("Preview restarted");
                }

                // Log file size
                if (!string.IsNullOrEmpty(_outputFilePath))
                {
                    try
                    {
                        var file = new Java.IO.File(_outputFilePath);
                        long fileSizeBytes = file.Length();
                        long fileSizeMB = fileSizeBytes / (1024 * 1024);
                        long fileSizeKB = (fileSizeBytes % (1024 * 1024)) / 1024;

                        string sizeString = fileSizeMB > 0
                            ? $"{fileSizeMB} MB ({fileSizeKB} KB)"
                            : $"{fileSizeBytes / 1024} KB";

                        Logger.WriteDebug($"Recording stopped. Video saved to: {_outputFilePath} | Size: {sizeString}");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteDebug($"Error getting file size: {ex.Message}");
                    }
                }

                PlayBeep(100, 3);
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"StopRecording error: {ex.Message}");
                CleanupMediaRecorder();
            }
        }

        public void StopRecording()
        {
            StopRecordingAsync().Wait();
        }

        private void CleanupMediaRecorder()
        {
            try
            {
                _mediaRecorder?.Release();
                _mediaRecorder = null;
            }
            catch (Exception ex)
            {
                Logger.WriteDebug($"MediaRecorder cleanup error: {ex.Message}");
            }
        }

        public void StartCountdownBeeping()
        {
            var camRecorder = CamRecorder.Instance;
            if (camRecorder.BeepRepeatTimeMs <= 0) return;

            _beepRunnable = new BeepRunnable(this, camRecorder.PreRecordTimeS * 1000);
            _beepHandler?.PostDelayed(_beepRunnable, camRecorder.BeepRepeatTimeMs);
        }

        public void StopCountdownBeeping()
        {
            if (_beepRunnable != null)
            {
                _beepHandler?.RemoveCallbacks(_beepRunnable);
                _beepRunnable = null;
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
                toneGen.StartTone(global::Android.Media.Tone.DtmfS, durationMs);
                var handler = new Handler(Looper.MainLooper);
                for (int i = 1; i < repeatCount; i++)
                {
                    handler.PostDelayed(() =>
                    {
                        toneGen.StartTone(global::Android.Media.Tone.DtmfS, durationMs);
                    }, i * (durationMs + 100));
                }
            }
            catch (System.Exception ex)
            {
                Logger.WriteDebug($"PlayBeep error: {ex.Message}");
            }
        }

        private class BeepRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private CameraPreview _preview;
            private long _startTimeMs;
            private long _stopTimeMs;

            public BeepRunnable(CameraPreview preview, long durationMs)
            {
                _preview = preview;
                _startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _stopTimeMs = durationMs;
            }

            public void Run()
            {
                long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long elapsedTimeMs = currentTimeMs - _startTimeMs;

                if (elapsedTimeMs < _stopTimeMs)
                {
                    _preview.PlayBeep(100, 1);
                    var camRecorder = CamRecorder.Instance;
                    _preview._beepHandler?.PostDelayed(this, camRecorder.BeepRepeatTimeMs);
                }
            }
        }
    }
}
#endif
