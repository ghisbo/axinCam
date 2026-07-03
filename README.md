# axinCam

A .NET MAUI-based camera application for recording video with configurable recording parameters and audio feedback.

## Project Structure

```
axinCam/
├── recordCam/              # Main MAUI application
│   ├── Platforms/          # Platform-specific implementations
│   │   ├── Android/        # Android camera and recording logic
│   │   ├── iOS/
│   │   ├── MacCatalyst/
│   │   └── Windows/
│   ├── Services/           # Business logic and configuration
│   │   └── CamRecorder.cs  # Centralized camera configuration
│   ├── App.xaml(.cs)       # Application entry point
│   ├── CameraPage.xaml(.cs)# Main camera UI and recording flow
│   ├── CameraView.cs       # Camera view control
│   ├── Logger.cs           # Logging utility
│   └── MauiProgram.cs      # MAUI configuration
└── axinCam.sln             # Solution file
```

## Features

### Camera Recording
- Configurable pre-record countdown (default: 5 seconds)
- Configurable recording duration (default: 10 seconds)
- Support for front and back camera selection
- Portrait and landscape orientation support

### Audio Feedback
- Countdown beeping at configurable intervals (default: 500ms)
- Double beep 2 seconds before recording starts
- Long beep when recording begins
- Triple beep when recording stops

### Configuration
All camera and recording settings are centralized in `Services/CamRecorder.cs`:
- `PreRecordTimeS`: Seconds before recording starts
- `RecordTimeS`: Duration of recording
- `BeepRepeatTimeMs`: Interval between countdown beeps
- `Orientation`: Portrait or Landscape
- `Face`: Front or Back camera
- `VideoFileMap`: Output directory for videos
- Camera properties: resolution, frame rate, bitrate

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- Android SDK (for Android deployment)
- Visual Studio 2022 (recommended) or Visual Studio Code

### Build Android
```bash
dotnet build -f net9.0-android
```

### Run on Android Device/Emulator
```bash
dotnet run -f net9.0-android
```

## Recording Flow

1. **User clicks Start Recording** → Countdown beeping begins
2. **Pre-record duration (configurable)** → Beep every 500ms until 3 seconds remain
3. **2 seconds before recording** → Double beep plays
4. **Recording starts** → Long beep plays and video recording begins
5. **Recording duration elapsed** → Recording stops and triple beep plays

## Video Output

Videos are saved to:
- Android: `Movies/recordCam/` directory
- File naming: `swing_YYYYMMDD_HHmmss.mp4`

## Configuration Example

```csharp
var camRecorder = CamRecorder.Instance;
camRecorder.PreRecordTimeS = 3;
camRecorder.RecordTimeS = 15;
camRecorder.BeepRepeatTimeMs = 300;
camRecorder.Orientation = OrientationMode.Landscape;
camRecorder.Face = CameraFace.Front;
```

## License

Project is part of axinGolf suite.
