namespace recordCam.Services
{
    public class CamRecorder
    {
        private static CamRecorder _instance;
        private static readonly object _lockObject = new object();

        // Recording timing
        public int PreRecordTimeS { get; set; } = 10; // seconds before recording starts
        public int RecordTimeS { get; set; } = 10; // seconds to record

        // Beeping configuration
        public int BeepRepeatTimeMs { get; set; } = 2000; // milliseconds between beeps

        // Camera orientation
        public OrientationMode Orientation { get; set; } = OrientationMode.Portrait;

        // Camera face selection
        public CameraFace Face { get; set; } = CameraFace.Back;

        // Video file storage
        public string VideoFileMap { get; set; } = "recordCam";

        // Camera properties
        public int CameraImageWidth { get; set; } = 640;
        public int CameraImageHeight { get; set; } = 480;
        public int PreviewBufferWidth { get; set; } = 1280;
        public int PreviewBufferHeight { get; set; } = 720;
        public int VideoFrameRate { get; set; } = 30;
        public int VideoEncodingBitRate { get; set; } = 5000000;

        private CamRecorder()
        {
        }

        public static CamRecorder Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new CamRecorder();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Reset()
        {
            PreRecordTimeS = 10;
            RecordTimeS = 10;
            BeepRepeatTimeMs = 2000;
            Orientation = OrientationMode.Portrait;
            Face = CameraFace.Back;
            VideoFileMap = "recordCam";
            CameraImageWidth = 640;
            CameraImageHeight = 480;
            PreviewBufferWidth = 1280;
            PreviewBufferHeight = 720;
            VideoFrameRate = 30;
            VideoEncodingBitRate = 5000000;
        }
    }

    public enum OrientationMode
    {
        Portrait = 90,
        Landscape = 0
    }

    public enum CameraFace
    {
        Back = 0,
        Front = 1
    }
}
