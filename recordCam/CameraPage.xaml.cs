using Microsoft.Maui.Controls;
using recordCam.Services;
using System;

namespace recordCam;

public partial class CameraPage : ContentPage
{
    private recordCam.Platforms.Android.CameraViewHandler _handler;
    private bool _eventsSubscribed = false;

    public CameraPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Subscribe to events when page appears
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SubscribeToRecordingEvents();
        });
    }

    private void SubscribeToRecordingEvents()
    {
#if ANDROID
        if (_eventsSubscribed) return;

        // Try to get the handler - it might not be ready yet
        _handler = cameraView.Handler as recordCam.Platforms.Android.CameraViewHandler;
        
        if (_handler != null)
        {
            Logger.WriteDebug("CameraViewHandler obtained successfully");
            _handler.CountdownProgress += Handler_CountdownProgress;
            _handler.RecordingStarted += Handler_RecordingStarted;
            _handler.RecordingCompleted += Handler_RecordingCompleted;
            _eventsSubscribed = true;
            Logger.WriteDebug("Event subscriptions completed");
        }
        else
        {
            Logger.WriteDebug("CameraViewHandler is null, will retry");
        }
#endif
    }

    private void Handler_CountdownProgress(object sender, CountdownProgressEventArgs args)
    {
        Logger.WriteDebug($"CountdownProgress event: {args.RemainingSeconds} seconds remaining");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            statusLabel.Text = $"Starting in {args.RemainingSeconds} sec";
        });
    }

    private void Handler_RecordingStarted(object sender, RecordingStartedEventArgs args)
    {
        Logger.WriteDebug("RecordingStarted event fired");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            statusLabel.Text = "Recording...";
        });
    }

    private void Handler_RecordingCompleted(object sender, RecordingCompletedEventArgs args)
    {
        Logger.WriteDebug($"RecordingCompleted event: Success={args.Success}, Path={args.VideoFilePath}");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var startButton = this.FindByName<Button>("startButton");
            var stopButton = this.FindByName<Button>("stopButton");

            if (args.Success)
            {
                statusLabel.Text = "✓ Ready";
            }
            else
            {
                statusLabel.Text = $"✗ Error: {args.ErrorMessage}";
            }

            if (startButton != null) startButton.IsEnabled = true;
            if (stopButton != null) stopButton.IsEnabled = false;
            
            Logger.WriteDebug("Button states updated");
        });
    }

    private void OnStartRecordingClicked(object sender, EventArgs e)
    {
        var startButton = (Button)sender;
        var stopButton = this.FindByName<Button>("stopButton");

        // Make sure we have the handler and events are subscribed
        if (_handler == null || !_eventsSubscribed)
        {
            SubscribeToRecordingEvents();
        }

        startButton.IsEnabled = false;
        statusLabel.Text = "Starting...";

#if ANDROID
        if (_handler != null)
        {
            Logger.WriteDebug("Starting recording sequence");
            _handler.StartRecordingSequence();
        }
        else
        {
            Logger.WriteDebug("ERROR: Handler is null, cannot start recording");
            statusLabel.Text = "Error: Camera not ready";
            startButton.IsEnabled = true;
        }
#endif

        if (stopButton != null) stopButton.IsEnabled = true;
    }

    private void OnStopRecordingClicked(object sender, EventArgs e)
    {
        var stopButton = (Button)sender;
        var startButton = this.FindByName<Button>("startButton");

        Logger.WriteDebug("Stop button clicked - cancelling recording sequence");

#if ANDROID
        // Cancel the recording sequence (either countdown or recording)
        // The handler will detect if we're recording and save the file
        _handler?.CancelRecordingSequence();
#endif

        if (stopButton != null) stopButton.IsEnabled = false;
        if (startButton != null) startButton.IsEnabled = true;
    }
}
