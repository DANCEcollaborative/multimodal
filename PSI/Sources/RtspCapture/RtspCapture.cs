using System;

namespace RtspCapture
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media_Interop;
    using RtspClientSharp;
    using RtspClientSharp.Rtsp;

    /// <summary>
    /// Component that captures and streams video and audio from a web camera through RTSP protocol.
    /// </summary>
    public class RtspCapture : IProducer<Shared<Image>>, ISourceComponent, IDisposable, IMediaCapture
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// The video camera configuration.
        /// </summary>
        private readonly MediaCaptureConfiguration configuration;

        /// <summary>
        /// The video capture device.
        /// </summary>
        private MediaCaptureDevice camera;

        /// <summary>
        /// define audio buffer.
        /// </summary>
        private IProducer<Microsoft.Psi.Audio.AudioBuffer> audio;

        /// <summary>
        /// Defines attributes of properties exposed by MediaCaptureDevice.
        /// </summary>
        private MediaCaptureInfo deviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureRtsp"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="width">Width of output image in pixels.</param>
        /// <param name="height">Height of output image in pixels.</param>
        /// <param name="framerate">Frame rate.</param>
        /// <param name="captureAudio">Should we create an audio capture device.</param>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="persistVideoFrames">Indicates whether video frames should be persisted.</param>
        /// <param name="useInSharedMode">Indicates whether camera is shared amongst multiple applications.</param>
        public MediaCaptureRtsp(Pipeline pipeline, int width, int height, double framerate = 15, bool captureAudio = false, string deviceId = null, bool persistVideoFrames = false, bool useInSharedMode = false)
    : this(pipeline)
        {
            this.configuration = new MediaCaptureConfiguration()
            {
                UseInSharedMode = useInSharedMode,
                DeviceId = deviceId,
                Width = width,
                Height = height,
                Framerate = framerate,
                CaptureAudio = captureAudio,
            };
            if (this.configuration.CaptureAudio)
            {
                this.audio = new Audio.AudioCapture(pipeline, new Audio.AudioCaptureConfiguration() { OutputFormat = Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm() });
            }
        }

        private MediaCaptureRtsp(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the emitter for the audio stream.
        /// </summary>
        public Emitter<Audio.AudioBuffer> Audio
        {
            get { return this.audio?.Out; }
            private set { }
        }

        /// <summary>
        /// Gets the emitter for the video stream.
        /// </summary>
        public Emitter<Shared<Image>> Video => this.Out;

        /// <summary>
        /// Gets the output stream of images.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // check for null since it's possible that Start was never called
            if (this.camera != null)
            {
                this.camera.Shutdown();
                this.camera.Dispose();
                this.camera = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> rtspNotifyCompletionTime)
        {
            // notify that this is an infinite source component
            rtspNotifyCompletionTime(DateTime.MaxValue);

            // RTSP setting
            var serverUri = new Uri("rtsp://192.168.1.77:554/ucast/11");
            var credentials = new NetworkCredential("admin", "123456");

            MediaCaptureDevice.Initialize();
            CaptureFormat rtspFound = this.RtspFrameReciver(serverUri, credentials);

            // Get capture sample
            if (rtspFound != null)
            {
                this.camera.CurrentFormat = rtspFound;
                this.deviceInfo = new MediaCaptureInfo(this.camera);
                var width = this.camera.CurrentFormat.nWidth;
                var height = this.camera.CurrentFormat.nHeight;

                // get the default settings for other properties.--do not written
                /*var currentConfig = this.GetDeviceConfiguration();
                this.configuration.BacklightCompensation = currentConfig.BacklightCompensation;*/

                // this.SetDeviceConfiguration(this.configuration);
                this.camera.CaptureSample((data, length, timestamp) =>
                {
                    var time = DateTime.FromFileTimeUtc(timestamp);
                    using (var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp))
                    {
                        sharedImage.Resource.CopyFrom(data);
                        var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(timestamp);
                        this.Out.Post(sharedImage, originatingTime);
                    }
                });
            }
            else
            {
                throw new ArgumentException("RTSP camera do not found");
            }

            var connectionParameters = new ConnectionParameters(serverUri, credentials);
            var rtspClient = new RtspClient(connectionParameters);
            var cancellationTokenSource = new CancellationTokenSource();
            var rawFramesSource = new RawFramesSource(connectionParameters);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Dispose();
            MediaCaptureDevice.Uninitialize();
            notifyCompleted();
        }

        // <inheritdoc/>
        private CaptureFormat RtspFrameReciver(Uri rtspUri, NetworkCredential rtspCredential)
        {
            var rtspFrameFormat = new CaptureFormat();
            return rtspFrameFormat;
        }
    }
}
