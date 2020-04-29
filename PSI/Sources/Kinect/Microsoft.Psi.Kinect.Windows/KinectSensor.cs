﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that captures and streams information (images, depth, audio, bodies, etc.) from a Kinect One (v2) sensor.
    /// </summary>
    public class KinectSensor : IKinectSensor, ISourceComponent, IDisposable
    {
        private static WaveFormat audioFormat = WaveFormat.Create16kHz1ChannelIeeeFloat();
        private readonly Pipeline pipeline;

        private Microsoft.Kinect.KinectSensor kinectSensor = null;
        private KinectSensorConfiguration configuration = null;
        private IKinectCalibration kinectCalibration = null;
        private bool calibrationPosted = false;
        private MultiSourceFrameReader multiFrameReader = null;
        private FrameSourceTypes whichFrames = FrameSourceTypes.None;

        private AudioBeamFrameReader audioBeamFrameReader = null;

        private IList<Body> bodies = null;
        private List<KinectBody> kinectBodies = null;
        private int trackedBodies = 0;
        private byte[] audioBuffer = null;
        private IList<ulong> bodyTrackingIds = null;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectSensor"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this sensor is a part of.</param>
        /// <param name="configurationFilename">Name of configuration file.</param>
        public KinectSensor(Pipeline pipeline, string configurationFilename)
        : this(pipeline)
        {
            var configurationHelper = new ConfigurationHelper<KinectSensorConfiguration>(configurationFilename);
            this.configuration = configurationHelper.Configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectSensor"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this sensor is a part of.</param>
        /// <param name="configuration">Configuration to use.</param>
        public KinectSensor(Pipeline pipeline, KinectSensorConfiguration configuration)
        : this(pipeline)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectSensor"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this sensor is a part of.</param>
        private KinectSensor(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Bodies = pipeline.CreateEmitter<List<KinectBody>>(this, nameof(this.Bodies));
            this.ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.ColorImage));
            this.RGBDImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.RGBDImage));
            this.DepthImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.DepthImage));
            this.InfraredImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.InfraredImage));
            this.LongExposureInfraredImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.LongExposureInfraredImage));
            this.Calibration = pipeline.CreateEmitter<IKinectCalibration>(this, nameof(this.Calibration));
            this.DepthFrameToCameraSpaceTable = pipeline.CreateEmitter<PointF[]>(this, nameof(this.DepthFrameToCameraSpaceTable));
            this.Audio = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Audio));
            this.AudioBeamInfo = pipeline.CreateEmitter<KinectAudioBeamInfo>(this, nameof(this.AudioBeamInfo));
            this.AudioBodyCorrelations = pipeline.CreateEmitter<IList<ulong>>(this, nameof(this.AudioBodyCorrelations)); // List of body ids which are speaking
            this.ColorToCameraMapper = pipeline.CreateEmitter<CameraSpacePoint[]>(this, nameof(this.ColorToCameraMapper));
        }

        // While kinect always seems to output 6 bodies (including untracked ones), this stream only publishes the tracked bodies

        /// <summary>
        /// Gets the list of bodies from the Kinect.
        /// </summary>
        public Emitter<List<KinectBody>> Bodies { get; private set; }

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current color+depth image.
        /// </summary>
        public Emitter<Shared<Image>> RGBDImage { get; private set; }

        /// <summary>
        /// Gets the current image from the depth camera.
        /// </summary>
        public Emitter<Shared<Image>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the current image from the infrared camera.
        /// </summary>
        public Emitter<Shared<Image>> InfraredImage { get; private set; }

        /// <summary>
        /// Gets the current long exposure image from the infrared camera.
        /// </summary>
        public Emitter<Shared<Image>> LongExposureInfraredImage { get; private set; }

        /// <summary>
        /// Gets the table of camera space points as returned by <see cref="CoordinateMapper.GetDepthFrameToCameraSpaceTable"/>.
        /// </summary>
        public Emitter<PointF[]> DepthFrameToCameraSpaceTable { get; private set; }

        /// <summary>
        /// Gets the Kinect's calibration object.
        /// </summary>
        public Emitter<IKinectCalibration> Calibration { get; private set; }

        /// <summary>
        /// Gets the emitter that returns the Kinect's audio samples.
        /// </summary>
        public Emitter<AudioBuffer> Audio { get; private set; }

        /// <summary>
        /// Gets the KinectAudioBeamInfo which returns information about the Kinect's audio beam.
        /// </summary>
        public Emitter<KinectAudioBeamInfo> AudioBeamInfo { get; private set; }

        /// <summary>
        /// Gets audio body correlations.
        /// </summary>
        public Emitter<IList<ulong>> AudioBodyCorrelations { get; private set; }

        /// <summary>
        /// Gets a emitter that maps color points to camera space points.
        /// </summary>
        public Emitter<CameraSpacePoint[]> ColorToCameraMapper { get; private set; }

        /// <summary>
        /// Gets the underlying Kinect sensor device.
        /// </summary>
        public Microsoft.Kinect.KinectSensor KinectDevice => this.kinectSensor;

        private int DisplayWidth => this.kinectSensor.ColorFrameSource.FrameDescription.Width;

        private int DisplayHeight => this.kinectSensor.ColorFrameSource.FrameDescription.Height;

        /// <summary>
        /// Called to release the sensor.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.multiFrameReader?.Dispose();
                this.audioBeamFrameReader?.Dispose();
                this.kinectSensor?.Close();
                this.disposed = true;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.StartKinect();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.kinectSensor?.Close();
            notifyCompleted();
        }

        private void StartKinect()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(KinectSensor));
            }

            this.kinectSensor = Microsoft.Kinect.KinectSensor.GetDefault();
            this.kinectSensor.CoordinateMapper.CoordinateMappingChanged += this.CoordinateMapper_CoordinateMappingChanged;

            this.whichFrames = FrameSourceTypes.None;

            if (this.configuration.OutputBodies)
            {
                this.whichFrames |= FrameSourceTypes.Body;
            }

            if (this.configuration.OutputColor)
            {
                this.whichFrames |= FrameSourceTypes.Color;
            }

            if (this.configuration.OutputDepth)
            {
                this.whichFrames |= FrameSourceTypes.Depth;
            }

            if (this.configuration.OutputInfrared)
            {
                this.whichFrames |= FrameSourceTypes.Infrared;
            }

            if (this.configuration.OutputLongExposureInfrared)
            {
                this.whichFrames |= FrameSourceTypes.LongExposureInfrared;
            }

            if (this.whichFrames != FrameSourceTypes.None)
            {
                this.multiFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(this.whichFrames);
                this.multiFrameReader.MultiSourceFrameArrived += this.MultiFrameReader_FrameArrived;
            }

            if (this.configuration.OutputAudio)
            {
                this.audioBeamFrameReader = this.kinectSensor.AudioSource.OpenReader();
                this.audioBeamFrameReader.FrameArrived += this.AudioBeamFrameReader_FrameArrived;
            }

            this.kinectSensor.Open();
        }

        private void CoordinateMapper_CoordinateMappingChanged(object sender, CoordinateMappingChangedEventArgs e)
        {
            if (this.DepthFrameToCameraSpaceTable.HasSubscribers)
            {
                this.DepthFrameToCameraSpaceTable.Post(this.kinectSensor.CoordinateMapper.GetDepthFrameToCameraSpaceTable(), this.pipeline.GetCurrentTime());
            }

            if (this.configuration.OutputCalibration)
            {
                if (!this.calibrationPosted)
                {
                    // Extract and created old style calibration
                    var kinectInternalCalibration = new KinectInternalCalibration();
                    kinectInternalCalibration.RecoverCalibrationFromSensor(this.kinectSensor);

                    Matrix<double> colorCameraMatrix = Matrix<double>.Build.Dense(3, 3);
                    Matrix<double> depthCameraMatrix = Matrix<double>.Build.Dense(3, 3);
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            colorCameraMatrix[i, j] = kinectInternalCalibration.colorCameraMatrix[i, j];
                            depthCameraMatrix[i, j] = kinectInternalCalibration.depthCameraMatrix[i, j];
                        }
                    }

                    Vector<double> colorLensDistortion = Vector<double>.Build.Dense(5);
                    Vector<double> depthLensDistortion = Vector<double>.Build.Dense(5);
                    for (int i = 0; i < 5; i++)
                    {
                        colorLensDistortion[i] = kinectInternalCalibration.colorLensDistortion[i];
                        depthLensDistortion[i] = kinectInternalCalibration.depthLensDistortion[i];
                    }

                    Matrix<double> depthToColorTransform = Matrix<double>.Build.Dense(4, 4);
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            depthToColorTransform[i, j] = kinectInternalCalibration.depthToColorTransform[i, j];
                        }
                    }

                    // Extract and create new style calibration
                    this.kinectCalibration = new KinectCalibration(
                        this.kinectSensor.ColorFrameSource.FrameDescription.Width,
                        this.kinectSensor.ColorFrameSource.FrameDescription.Height,
                        colorCameraMatrix,
                        kinectInternalCalibration.colorLensDistortion[0],
                        kinectInternalCalibration.colorLensDistortion[1],
                        0.0,
                        0.0,
                        depthToColorTransform,
                        this.kinectSensor.DepthFrameSource.FrameDescription.Width,
                        this.kinectSensor.DepthFrameSource.FrameDescription.Height,
                        depthCameraMatrix,
                        kinectInternalCalibration.depthLensDistortion[0],
                        kinectInternalCalibration.depthLensDistortion[1],
                        0.0,
                        0.0);

                    /* Warning about comments being preceded by blank line */
#pragma warning disable SA1515
                    // KinectCalibrationOld's original ToColorSpace() method flips the Y axis (height-Y). To account
                    // for this we adjust our Transform.
                    //                        Matrix<double> flipY = Matrix<double>.Build.DenseIdentity(3, 3);
                    //                        flipY[1, 1] = -1.0;
                    //                        flipY[1, 2] = this.kinectSensor.ColorFrameSource.FrameDescription.Height;
                    //                        this.kinectCalibration.ColorIntrinsics.Transform = flipY * this.kinectCalibration.ColorIntrinsics.Transform;
#pragma warning restore SA1515

                    this.Calibration.Post(this.kinectCalibration, this.pipeline.GetCurrentTime());
                    this.calibrationPosted = true;
                }
            }
        }

        private void LongExposureInfraredFrameReader_FrameArrived(LongExposureInfraredFrameReference longExposureInfraredFrameReference)
        {
            using (LongExposureInfraredFrame longExposureInfraredFrame = longExposureInfraredFrameReference.AcquireFrame())
            {
                if (longExposureInfraredFrame != null)
                {
                    FrameDescription longExposureInfraredFrameDescription = longExposureInfraredFrame.FrameDescription;
                    using (KinectBuffer longExposureInfraredBuffer = longExposureInfraredFrame.LockImageBuffer())
                    {
                        using (var dest = ImagePool.GetOrCreate(longExposureInfraredFrameDescription.Width, longExposureInfraredFrameDescription.Height, Imaging.PixelFormat.Gray_16bpp))
                        {
                            longExposureInfraredFrame.CopyFrameDataToIntPtr(dest.Resource.ImageData, (uint)(longExposureInfraredFrameDescription.Width * longExposureInfraredFrameDescription.Height * 2));
                            var time = this.pipeline.GetCurrentTimeFromElapsedTicks(longExposureInfraredFrameReference.RelativeTime.Ticks);
                            this.LongExposureInfraredImage.Post(dest, time);
                        }
                    }
                }
            }
        }

        private void InfraredFrameReader_FrameArrived(InfraredFrameReference infraredFrameReference)
        {
            using (InfraredFrame infraredFrame = infraredFrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;
                    using (KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        using (var dest = ImagePool.GetOrCreate(infraredFrameDescription.Width, infraredFrameDescription.Height, Imaging.PixelFormat.Gray_16bpp))
                        {
                            infraredFrame.CopyFrameDataToIntPtr(dest.Resource.ImageData, (uint)(infraredFrameDescription.Width * infraredFrameDescription.Height * 2));
                            var time = this.pipeline.GetCurrentTimeFromElapsedTicks(infraredFrameReference.RelativeTime.Ticks);
                            this.InfraredImage.Post(dest, time);
                        }
                    }
                }
            }
        }

        private void DepthFrameReader_FrameArrived(DepthFrame depthFrame)
        {
            if (depthFrame != null)
            {
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    using (var dest = ImagePool.GetOrCreate(depthFrameDescription.Width, depthFrameDescription.Height, Imaging.PixelFormat.Gray_16bpp))
                    {
                        depthFrame.CopyFrameDataToIntPtr(dest.Resource.ImageData, (uint)(depthFrameDescription.Width * depthFrameDescription.Height * 2));
                        var time = this.pipeline.GetCurrentTimeFromElapsedTicks(depthFrame.RelativeTime.Ticks);
                        this.DepthImage.Post(dest, time);
                    }
                }
            }
        }

        private void ColorFrameReader_FrameArrived(ColorFrameReference colorFrameReference, out Shared<Image> colorImage)
        {
            colorImage = null;
            using (ColorFrame colorFrame = colorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        using (var sharedImage = ImagePool.GetOrCreate(colorFrameDescription.Width, colorFrameDescription.Height, Imaging.PixelFormat.BGRX_32bpp))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(sharedImage.Resource.ImageData, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4), ColorImageFormat.Bgra);
                            colorImage = sharedImage.AddRef();
                            var time = this.pipeline.GetCurrentTimeFromElapsedTicks(colorFrame.RelativeTime.Ticks);
                            this.ColorImage.Post(sharedImage, time);
                        }
                    }
                }
            }
        }

        private void MapColorToDepth(DepthFrame depthFrame, Shared<Image> colorImage)
        {
            const int colorImageWidth = 1920;
            const int colorImageHeight = 1080;

            if (!this.configuration.OutputColorToCameraMapping && !this.configuration.OutputRGBD)
            {
                return;
            }

            ushort[] depthData = new ushort[depthFrame.FrameDescription.LengthInPixels];
            depthFrame.CopyFrameDataToArray(depthData);

            if (this.configuration.OutputColorToCameraMapping)
            {
                // Writing out a mapping from color space to camera space
                CameraSpacePoint[] colorToCameraMapping = new CameraSpacePoint[colorImageWidth * colorImageHeight];
                this.kinectSensor.CoordinateMapper.MapColorFrameToCameraSpace(depthData, colorToCameraMapping);
                var time = this.pipeline.GetCurrentTimeFromElapsedTicks(depthFrame.RelativeTime.Ticks);
                this.ColorToCameraMapper.Post(colorToCameraMapping, time);
            }

            if (this.configuration.OutputRGBD)
            {
                unsafe
                {
                    DepthSpacePoint[] depthSpacePoints = new DepthSpacePoint[colorImageWidth * colorImageHeight];
                    this.kinectSensor.CoordinateMapper.MapColorFrameToDepthSpace(depthData, depthSpacePoints);
                    using (var rgbd = ImagePool.GetOrCreate(colorImageWidth, colorImageHeight, Imaging.PixelFormat.RGBA_64bpp))
                    {
                        byte* srcRow = (byte*)colorImage.Resource.ImageData.ToPointer();
                        byte* dstRow = (byte*)rgbd.Resource.ImageData.ToPointer();
                        int depthWidth = depthFrame.FrameDescription.Width;
                        int depthHeight = depthFrame.FrameDescription.Height;
                        for (int y = 0; y < colorImage.Resource.Height; y++)
                        {
                            byte* srcCol = srcRow;
                            ushort* dstCol = (ushort*)dstRow;
                            int offset = y * colorImageWidth;
                            for (int x = 0; x < colorImage.Resource.Width; x++)
                            {
                                dstCol[0] = (ushort)(srcCol[2] << 8);
                                dstCol[1] = (ushort)(srcCol[1] << 8);
                                dstCol[2] = (ushort)(srcCol[0] << 8);
                                DepthSpacePoint pt = depthSpacePoints[offset];
                                if (pt.X >= 0 && pt.X < depthWidth && pt.Y >= 0 && pt.Y < depthHeight)
                                {
                                    dstCol[3] = depthData[((int)pt.Y * depthWidth) + (int)pt.X];
                                }
                                else
                                {
                                    dstCol[3] = 0;
                                }

                                dstCol += 4;
                                srcCol += colorImage.Resource.BitsPerPixel / 8;
                                offset++;
                            }

                            srcRow += colorImage.Resource.Stride;
                            dstRow += rgbd.Resource.Stride;
                        }

                        var time = this.pipeline.GetCurrentTimeFromElapsedTicks(depthFrame.RelativeTime.Ticks);
                        this.RGBDImage.Post(rgbd, time);
                    }
                }
            }
        }

        private Dictionary<TKey, TVal> CloneDictionary<TKey, TVal>(IReadOnlyDictionary<TKey, TVal> dictionaryIn)
        {
            Dictionary<TKey, TVal> dictionary = new Dictionary<TKey, TVal>();
            foreach (var key in dictionaryIn.Keys)
            {
                dictionary[key] = dictionaryIn[key];
            }

            return dictionary;
        }

        private void BodyFrameReader_FrameArrived(BodyFrameReference bodyFrameReference)
        {
            using (BodyFrame bodyFrame = bodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null || this.bodies.Count != bodyFrame.BodyCount)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // compute the number of tracked bodies
                    this.trackedBodies = this.bodies.Count(b => b.IsTracked);

                    if (this.kinectBodies == null || this.kinectBodies.Count != this.trackedBodies)
                    {
                        this.kinectBodies = new List<KinectBody>(this.trackedBodies);
                        for (int i = 0; i < this.trackedBodies; i++)
                        {
                            this.kinectBodies.Add(new KinectBody());
                        }
                    }

                    // construct the output
                    int ti = 0;
                    for (int i = 0; i < bodyFrame.BodyCount; i++)
                    {
                        if (this.bodies[i].IsTracked)
                        {
                            this.kinectBodies[ti].FloorClipPlane = bodyFrame.FloorClipPlane;
                            this.kinectBodies[ti].ClippedEdges = this.bodies[i].ClippedEdges;
                            this.kinectBodies[ti].HandLeftConfidence = this.bodies[i].HandLeftConfidence;
                            this.kinectBodies[ti].HandLeftState = this.bodies[i].HandLeftState;
                            this.kinectBodies[ti].HandRightConfidence = this.bodies[i].HandRightConfidence;
                            this.kinectBodies[ti].HandRightState = this.bodies[i].HandRightState;
                            this.kinectBodies[ti].IsRestricted = this.bodies[i].IsRestricted;
                            this.kinectBodies[ti].IsRestricted = this.bodies[i].IsRestricted;
                            this.kinectBodies[ti].IsTracked = this.bodies[i].IsTracked;
                            this.kinectBodies[ti].JointOrientations = this.CloneDictionary(this.bodies[i].JointOrientations);
                            this.kinectBodies[ti].Joints = this.CloneDictionary(this.bodies[i].Joints);
                            this.kinectBodies[ti].Lean = this.bodies[i].Lean;
                            this.kinectBodies[ti].LeanTrackingState = this.bodies[i].LeanTrackingState;
                            this.kinectBodies[ti].TrackingId = this.bodies[i].TrackingId;
                            ti++;
                        }
                    }

                    var time = this.pipeline.GetCurrentTimeFromElapsedTicks(bodyFrameReference.RelativeTime.Ticks);
                    this.Bodies.Post(this.kinectBodies, time);
                }
            }
        }

        private void MultiFrameReader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();

            if ((this.whichFrames & FrameSourceTypes.Body) == FrameSourceTypes.Body)
            {
                this.BodyFrameReader_FrameArrived(frame.BodyFrameReference);
            }

            Shared<Image> colorImage = null;
            if ((this.whichFrames & FrameSourceTypes.Color) == FrameSourceTypes.Color)
            {
                this.ColorFrameReader_FrameArrived(frame.ColorFrameReference, out colorImage);
            }

            DepthFrame depthFrame = null;
            if ((this.whichFrames & FrameSourceTypes.Depth) == FrameSourceTypes.Depth)
            {
                depthFrame = frame.DepthFrameReference.AcquireFrame();
                this.DepthFrameReader_FrameArrived(depthFrame);
            }

            if (depthFrame != null && colorImage != null)
            {
                this.MapColorToDepth(depthFrame, colorImage);
            }

            colorImage?.Dispose();
            depthFrame?.Dispose();

            if ((this.whichFrames & FrameSourceTypes.Infrared) == FrameSourceTypes.Infrared)
            {
                this.InfraredFrameReader_FrameArrived(frame.InfraredFrameReference);
            }

            if ((this.whichFrames & FrameSourceTypes.LongExposureInfrared) == FrameSourceTypes.LongExposureInfrared)
            {
                this.LongExposureInfraredFrameReader_FrameArrived(frame.LongExposureInfraredFrameReference);
            }
        }

        private void AudioBeamFrameReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            using (AudioBeamFrameList frameList = e.FrameReference.AcquireBeamFrames())
            {
                if (frameList != null)
                {
                    // NOTE - the old pattern of passing the AudioBeamFrameList to a downstream
                    // KinectAudio component had issues that were exposed in async mode. The
                    // AudioBeamFrameList is not disposed immediately in the event handler as
                    // it needs to be kept around until the async receiver processes it. However,
                    // Kinect suppresses all further audio events until the AudioBeamFrameList is
                    // disposed, so the receiver in KinectAudio has no way of recycling the old
                    // AudioBeamFrameList once it is done processing it (since the receiver never
                    // gets called again and this is the way objects are passed back upstream for
                    // recycling in the current cooperative buffering scheme). To resolve this, I
                    // moved the audio processing into this handler inside a using clause which
                    // ensures that the AudioBeamFrameList is disposed of immediately.
                    AudioBeamFrame audioBeamFrame = frameList[0];

                    foreach (var subFrame in audioBeamFrame.SubFrames)
                    {
                        // Check if we need to reallocate the audio buffer - if for instance the downstream component
                        // that we posted-by-ref to modifies the reference to audioBuffer to null or an array with
                        // a different size.
                        if ((this.audioBuffer == null) || (this.audioBuffer.Length != subFrame.FrameLengthInBytes))
                        {
                            this.audioBuffer = new byte[subFrame.FrameLengthInBytes];
                        }

                        // Get the raw audio bytes from the frame.
                        subFrame.CopyFrameDataToArray(this.audioBuffer);

                        // Compute originating time from the relative time reported by Kinect.
                        var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks((subFrame.RelativeTime + subFrame.Duration).Ticks);

                        // Post the audio buffer by reference.
                        this.Audio.Post(new AudioBuffer(this.audioBuffer, KinectSensor.audioFormat), originatingTime);

                        // Post the audio beam angle information by value (not using co-operative buffering).
                        this.AudioBeamInfo.Post(new KinectAudioBeamInfo(subFrame.BeamAngle, subFrame.BeamAngleConfidence), originatingTime);

                        if ((subFrame.AudioBodyCorrelations != null) && (subFrame.AudioBodyCorrelations.Count > 0))
                        {
                            // Get BodyTrackingIds from AudioBodyCorrelations list (seems like this is the only
                            // bit of useful information).
                            var bodyIds = subFrame.AudioBodyCorrelations.Select(abc => abc.BodyTrackingId);

                            // Since we are posting bodyTrackingIds by-ref, we need to do a null check each
                            // time and allocate if necessary. Otherwise clear and re-use the existing list.
                            if (this.bodyTrackingIds == null)
                            {
                                // Allocate a new list
                                this.bodyTrackingIds = new List<ulong>(bodyIds);
                            }
                            else
                            {
                                // Re-use the existing list
                                this.bodyTrackingIds.Clear();
                                foreach (ulong id in bodyIds)
                                {
                                    this.bodyTrackingIds.Add(id);
                                }
                            }

                            // Post the audio body correlations by reference.
                            this.AudioBodyCorrelations.Post(this.bodyTrackingIds, originatingTime);
                        }
                    }
                }
            }
        }
    }
}
