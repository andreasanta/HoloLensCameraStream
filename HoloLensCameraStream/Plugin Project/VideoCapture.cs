//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Effects;
using Microsoft.Media.RTMP;
using Windows.Perception.Spatial;
using Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.Media;

using UnityEngine;
using Windows.Media.Devices;
using System.Reflection;

namespace HoloLensCameraStream
{
    /// <summary>
    /// Called when a VideoCapture resource has been created.
    /// If the instance failed to be created, the instance returned will be null.
    /// </summary>
    /// <param name="captureObject">The VideoCapture instance.</param>
    public delegate void OnVideoCaptureResourceCreatedCallback(VideoCapture captureObject);

    /// <summary>
    /// Called when the web camera begins streaming video.
    /// </summary>
    /// <param name="result">Indicates whether or not video recording started successfully.</param>
    public delegate void OnVideoModeStartedCallback(VideoCaptureResult result);

    /// <summary>
    /// This is called every time there is a new frame sample available.
    /// See VideoCapture.FrameSampleAcquired and the VideoCaptureSample class for more information.
    /// </summary>
    /// <param name="videoCaptureSample">The recently captured frame sample.
    /// It contains methods for accessing the bitmap, as well as supporting information
    /// such as transform and projection matrices.</param>
    public delegate void FrameSampleAcquiredCallback(VideoCaptureSample videoCaptureSample);

    /// <summary>
    /// Called when video mode has been stopped.
    /// </summary>
    /// <param name="result">Indicates whether or not video mode was successfully deactivated.</param>
    public delegate void OnVideoModeStoppedCallback(VideoCaptureResult result);

    public delegate void OnSessionPublishFailed(string reason);

    public delegate void OnSessionClosedHandler(string reason);

    /// <summary>
    /// Streams video from the camera and makes the buffer available for reading.
    /// </summary>
    public sealed class VideoCapture
    {
        /// <summary>
        /// Note: This function is not yet implemented. Help us out on GitHub!
        /// There is an instance method on VideoCapture called GetSupportedResolutions().
        /// Please use that until we can get this method working.
        /// </summary>
        public static IEnumerable<Resolution> SupportedResolutions
        {
            get
            {
                throw new NotImplementedException("Please use the instance method VideoCapture.GetSupportedResolutions() for now.");
            }
        }

        /// <summary>
        /// Returns the supported frame rates at which a video can be recorded given a resolution.
        /// Use VideoCapture.SupportedResolutions to get the supported web camera recording resolutions.
        /// </summary>
        /// <param name="resolution">A recording resolution.</param>
        /// <returns>The frame rates at which the video can be recorded.</returns>
        public static IEnumerable<float> SupportedFrameRatesForResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is called every time there is a new frame sample available.
        /// You must properly initialize the VideoCapture object, including calling StartVideoModeAsync()
        /// before this event will begin firing.
        /// 
        /// You should not subscribe to FrameSampleAcquired if you do not need access to most
        /// of the video frame samples for your application (for instance, if you are doing image detection once per second),
        /// because there is significant memory management overhead to processing every frame.
        /// Instead, you can call RequestNextFrameSample() which will respond with the next available sample only.
        /// 
        /// See the VideoFrameSample class for more information about dealing with the memory
        /// complications of the BitmapBuffer.
        /// </summary>
        public event FrameSampleAcquiredCallback FrameSampleAcquired;

        /// <summary>
        /// Indicates whether or not the VideoCapture instance is currently streaming video.
        /// This becomes true when the OnVideoModeStartedCallback is called, and ends 
        /// when the OnVideoModeStoppedCallback is called.
        /// 
        /// "VideoMode", as I have interpreted means that the frame reader begins delivering
        /// the bitmap buffer, making it available to be consumed.
        /// </summary>
        public bool IsStreaming
        {
            get
            {
                return _frameReader != null || IsStreamingRTMP;
            }
        }

        internal SpatialCoordinateSystem worldOrigin { get; private set; }
        public IntPtr WorldOriginPtr
        {
            set
            {
                worldOrigin = (SpatialCoordinateSystem)Marshal.GetObjectForIUnknown(value);
            }
        }

        static readonly Guid ROTATION_KEY = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        static readonly MediaStreamType STREAM_TYPE = MediaStreamType.VideoRecord;

        MediaFrameSourceGroup _frameSourceGroup;
        MediaFrameSourceInfo _frameSourceInfo;
        DeviceInformation _deviceInfo;
        DeviceInformation _audioDeviceInfo;
        MediaCapture _mediaCapture;
        MediaFrameReader _frameReader;

        // For RTMP streaming
        RTMPPublishSession _rtmpPublishSession = null;
        PublishProfile _pubProfile;
        LowLagMediaRecording _lowlagCapture;
        IMediaExtension _sink = null;
        bool IsStreamingRTMP = false;

        VideoCapture(MediaFrameSourceGroup frameSourceGroup, MediaFrameSourceInfo frameSourceInfo, DeviceInformation deviceInfo, DeviceInformation audioDevice = null)
        {
            _frameSourceGroup   = frameSourceGroup;
            _frameSourceInfo    = frameSourceInfo;
            _deviceInfo         = deviceInfo;
            _audioDeviceInfo    = audioDevice;
        }

        public async Task logAllSupportedDevicesAndModes()
        {
            
            var allFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
            foreach (MediaFrameSourceGroup g in allFrameSourceGroups)
            {
                Debug.LogFormat("==> MediaFrameSourceGroup");
                Debug.LogFormat("ID: {0}", g.Id);
                Debug.LogFormat("NAME: {0}", g.DisplayName);

                
                foreach (MediaFrameSourceInfo i in g.SourceInfos)
                {
                    Debug.LogFormat("===> MediaFrameSourceInfo");
                    Debug.LogFormat("ID: {0}", i.Id);
                    Debug.LogFormat("PROFILE ID: {0}", i.ProfileId);
                    Debug.LogFormat("MEDIA STREAM TYPE: {0}", i.MediaStreamType.ToString());
                    Debug.LogFormat("SOURCE KIND: {0}", i.SourceKind.ToString());
                    
                    var colourGroup = await MediaFrameSourceGroup.FindAllAsync();
                    foreach (var group in colourGroup)
                    {
                        Debug.LogFormat("====> MediaFrameSourceGroup {0}", group.DisplayName);
                        foreach (var source in group.SourceInfos)
                        {
                            Debug.LogFormat("\tStream Source {0}, {1}, {2}", source.MediaStreamType, source.SourceKind, source.Id);

                            foreach (var profile in source.VideoProfileMediaDescription)
                            {
                                Debug.LogFormat("Stream Profile {0}x{1} @ {2}", profile.Width, profile.Height, profile.FrameRate);
                            }
                        }
                    }

                    Debug.LogFormat("PROPERTIES");
                    foreach (KeyValuePair<Guid, object> pair in i.Properties)
                        Debug.LogFormat("{0} => {1}", pair.Key.ToString(), pair.Value.ToString());
                }
            }

            Debug.LogFormat("");
            Debug.LogFormat("");
            Debug.LogFormat("==> VIDEO Devices");

            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach (DeviceInformation d in videoDevices)
            {
                Debug.LogFormat("");
                Debug.LogFormat("ID: {0}", d.Id);
                Debug.LogFormat("NAME: {0}", d.Name);
                Debug.LogFormat("IS DEFAULT: {0}", d.IsDefault.ToString());
                Debug.LogFormat("IS ENABLED: {0}", d.IsEnabled.ToString());
                Debug.LogFormat("KIND: {0}", d.Kind.ToString());

                Debug.LogFormat("PROPERTIES");
                foreach (KeyValuePair<string, object> pair in d.Properties)
                    if (String.IsNullOrEmpty(pair.Key))
                        Debug.LogFormat("{0} => {1}", pair.Key.ToString(), pair.Value != null ? pair.Value.ToString() : "NULL");
                
                Debug.LogFormat("");
/*
                Debug.LogFormat("==> Stream Properties");
                MediaCapture _cap = new MediaCapture();
                await _cap.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    VideoDeviceId = d.Id,
                    MemoryPreference = MediaCaptureMemoryPreference.Auto
                });

                Debug.LogFormat("==> Initialized Media Capture");

                var allPropertySets = _cap.VideoDeviceController.GetAvailableMediaStreamProperties(STREAM_TYPE).Select((x) =>
                {

                    Debug.LogFormat("===> First video profile found");

                    if (x == null)
                        return x;

                    VideoEncodingProperties p = x as VideoEncodingProperties;

                    Debug.LogFormat("PROFILE ID: {0}", p.ProfileId.ToString());
                    Debug.LogFormat("BITRATE: {0}", p.Bitrate.ToString());
                    Debug.LogFormat("FPS: {0}", (p.FrameRate.Numerator / p.FrameRate.Denominator).ToString());
                    Debug.LogFormat("RESOLUTION: {0} x {1}", p.Height.ToString(), p.Width.ToString());
                    Debug.LogFormat("ASPECT RATIO: {0}:", p.PixelAspectRatio.Numerator.ToString(), p.PixelAspectRatio.Denominator.ToString());
                    Debug.LogFormat("PROPERTIES: {0}", p.Properties.ToString());
                    Debug.LogFormat("");

                    return x;

                }); //Returns IEnumerable<VideoEncodingProperties>

                _cap.Dispose();

                Debug.LogFormat("==> Listed Video Encoding Properties");
*/

            }

            Debug.LogFormat("");
            Debug.LogFormat("");
            Debug.LogFormat("==> AUDIO Devices");
            
            var audioDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (DeviceInformation d in audioDevices)
            {

                Debug.LogFormat("ID: {0}", d.Id);
                Debug.LogFormat("NAME: {0}", d.Name);
                Debug.LogFormat("IS DEFAULT: {0}", d.IsDefault.ToString());
                Debug.LogFormat("IS ENABLED: {0}", d.IsEnabled.ToString());
                Debug.LogFormat("KIND: {0}", d.Kind.ToString());


                Debug.LogFormat("PROPERTIES");
                foreach (KeyValuePair<string, object> pair in d.Properties)
                    if (String.IsNullOrEmpty(pair.Key))
                        Debug.LogFormat("{0} => {1}", pair.Key.ToString(), pair.Value != null ? pair.Value.ToString() : "NULL");

                /*
                Debug.LogFormat("==> Stream Properties");
                MediaCapture _cap = new MediaCapture();
                await _cap.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    AudioDeviceId = d.Id,
                    MemoryPreference = MediaCaptureMemoryPreference.Auto
                });


                var allPropertySets = _cap.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Audio).Select((x) =>
                {

                    Debug.LogFormat("===> First audio profile found");

                    if (x == null)
                        return x;

                    AudioEncodingProperties p = x as AudioEncodingProperties;

                    Debug.LogFormat("BITRATE: {0}", p.Bitrate.ToString());
                    Debug.LogFormat("SAMPLERATE: {0}", p.SampleRate.ToString());
                    Debug.LogFormat("BITSPERSAMPLE: {0}", p.BitsPerSample.ToString());
                    Debug.LogFormat("CHANNELCOUNT: {0}", p.ChannelCount.ToString());
                    Debug.LogFormat("IS SPATIAL: {0}:", p.IsSpatial.ToString());
                    Debug.LogFormat("PROPERTIES: {0}", p.Properties.ToString());
                    Debug.LogFormat("");

                    return x;
                });

                _cap.Dispose();

                Debug.LogFormat("==> Listed Audio Encoding Properties");
                */
            }
        }

        /// <summary>
        /// Asynchronously creates an instance of a VideoCapture object that can be used to stream video frames from the camera to memory.
        /// If the instance failed to be created, the instance returned will be null. Also, holograms will not appear in the video.
        /// </summary>
        /// <param name="onCreatedCallback">This callback will be invoked when the VideoCapture instance is created and ready to be used.</param>
        public static async void CreateAsync(OnVideoCaptureResourceCreatedCallback onCreatedCallback)
        {
            var allFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();                                              //Returns IReadOnlyList<MediaFrameSourceGroup>
            var candidateFrameSourceGroups = allFrameSourceGroups.Where(group => group.SourceInfos.Any((x) => IsColorVideo(x, STREAM_TYPE)));          //Returns IEnumerable<MediaFrameSourceGroup>
            var selectedFrameSourceGroup = candidateFrameSourceGroups.FirstOrDefault();                                         //Returns a single MediaFrameSourceGroup
            
            if (selectedFrameSourceGroup == null)
            {
                onCreatedCallback?.Invoke(null);
                return;
            }

            var selectedFrameSourceInfo = selectedFrameSourceGroup.SourceInfos.FirstOrDefault(); //Returns a MediaFrameSourceInfo
            
            if (selectedFrameSourceInfo == null)
            {
                onCreatedCallback?.Invoke(null);
                return;
            }
            
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);   //Returns DeviceCollection
            var videoDeviceInformation = videoDevices.FirstOrDefault();                               //Returns a single DeviceInformation
            
            if (videoDeviceInformation == null)
            {
                onCreatedCallback(null);
                return;
            }

            // We want to capture audio too
            var audioDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            var audioDeviceInformation = audioDevices.FirstOrDefault();

            if (audioDeviceInformation == null)
            {
                onCreatedCallback(null);
                return;
            }

            var videoCapture = new VideoCapture(selectedFrameSourceGroup, selectedFrameSourceInfo, videoDeviceInformation, audioDeviceInformation);
            await videoCapture.CreateMediaCaptureAsync();
            onCreatedCallback?.Invoke(videoCapture);
        }

        public IEnumerable<Resolution> GetSupportedResolutions(MediaStreamType streamType = MediaStreamType.VideoPreview)
        {
            List<Resolution> resolutions = new List<Resolution>();

            var allPropertySets = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => x as VideoEncodingProperties); //Returns IEnumerable<VideoEncodingProperties>
            foreach (var propertySet in allPropertySets)
            {
                resolutions.Add(new Resolution((int)propertySet.Width, (int)propertySet.Height));
            }

            return resolutions.AsReadOnly();
        }

        public IEnumerable<float> GetSupportedFrameRatesForResolution(Resolution resolution, MediaStreamType streamType = MediaStreamType.VideoPreview)
        {
            //Get all property sets that match the supported resolution
            var allPropertySets = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select((x) => x as VideoEncodingProperties)
                .Where((x) =>
            {
                return x != null &&
                x.Width == (uint)resolution.width &&
                x.Height == (uint)resolution.height;
            }); //Returns IEnumerable<VideoEncodingProperties>

            //Get all resolutions without duplicates.
            var frameRatesDict = new Dictionary<float, bool>();
            foreach (var propertySet in allPropertySets)
            {
                if (propertySet.FrameRate.Denominator != 0)
                {
                    float frameRate = (float)propertySet.FrameRate.Numerator / (float)propertySet.FrameRate.Denominator;
                    frameRatesDict.Add(frameRate, true);
                }
            }

            //Format resolutions as a list.
            var frameRates = new List<float>();
            foreach (KeyValuePair<float, bool> kvp in frameRatesDict)
            {
                frameRates.Add(kvp.Key);
            }

            return frameRates.AsReadOnly();
        }

        public async Task startRTMPStreamingAsync(
            string url,
            string streamName = null,
            CameraParameters cameraParams = null,
            AudioParameters audioParams = null,
            OnSessionPublishFailed onFailedCallback = null,
            OnSessionClosedHandler onClosedCallback = null
        )
        {
            cameraParams = cameraParams ?? new CameraParameters();
            audioParams = audioParams ?? new AudioParameters();

            
            VideoEncodingProperties srcvideoencodingprops = GetVideoEncodingPropertiesForCameraParams(cameraParams, STREAM_TYPE, MediaEncodingSubtypes.Nv12);
            AudioEncodingProperties srcaudioencodingprops = GetAudioEncodingPropertiesForAudioParams(audioParams, MediaStreamType.Audio);

/*            VideoEncodingProperties srcvideoencodingprops = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(STREAM_TYPE) as VideoEncodingProperties;
            AudioEncodingProperties srcaudioencodingprops = _mediaCapture.AudioDeviceController.GetMediaStreamProperties(STREAM_TYPE) as AudioEncodingProperties; */

            Debug.LogFormat("Video subtype from source {0}", srcvideoencodingprops.Subtype);
            Debug.LogFormat("Video resolution from source {0}x{1} @ {2} - {3} Kbits",
                    srcvideoencodingprops.Width, srcvideoencodingprops.Width, srcvideoencodingprops.FrameRate.Numerator, srcvideoencodingprops.Bitrate / 1024);
            Debug.LogFormat("Audio type from source {0} Hz, {1} bits, {2} channels", srcaudioencodingprops.Bitrate, srcaudioencodingprops.BitsPerSample, srcaudioencodingprops.ChannelCount);

            _pubProfile = new PublishProfile(RTMPServerType.Generic, url);

            // Destination and lag
            _pubProfile.StreamName = streamName;
            _pubProfile.EnableLowLatency = true;

            // H264 Encoding
            _pubProfile.TargetEncodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
            _pubProfile.TargetEncodingProfile.Container = null;
            _pubProfile.TargetEncodingProfile.Video.ProfileId = H264ProfileIds.Baseline;

            // Video size and fine tuning
            _pubProfile.TargetEncodingProfile.Video.Width = srcvideoencodingprops.Width;
            _pubProfile.TargetEncodingProfile.Video.Height = srcvideoencodingprops.Height;
            _pubProfile.TargetEncodingProfile.Video.Bitrate = srcvideoencodingprops.Bitrate;
            _pubProfile.KeyFrameInterval = (uint) cameraParams.frameRate * 2; // One keyframe every 2 seconds
            _pubProfile.ClientChunkSize = 128;
            _pubProfile.TargetEncodingProfile.Video.FrameRate.Numerator = srcvideoencodingprops.FrameRate.Numerator;
            _pubProfile.TargetEncodingProfile.Video.FrameRate.Denominator = srcvideoencodingprops.FrameRate.Denominator;
            _pubProfile.TargetEncodingProfile.Video.PixelAspectRatio.Numerator = srcvideoencodingprops.PixelAspectRatio.Numerator;
            _pubProfile.TargetEncodingProfile.Video.PixelAspectRatio.Denominator = srcvideoencodingprops.PixelAspectRatio.Denominator;

            // Audio size and fine tuning
            _pubProfile.TargetEncodingProfile.Audio.BitsPerSample = srcaudioencodingprops.BitsPerSample;
            _pubProfile.TargetEncodingProfile.Audio.Bitrate = srcaudioencodingprops.Bitrate;
            _pubProfile.TargetEncodingProfile.Audio.ChannelCount = srcaudioencodingprops.ChannelCount;
            _pubProfile.TargetEncodingProfile.Audio.SampleRate = 44100;

            _rtmpPublishSession = new RTMPPublishSession(new List<PublishProfile> { _pubProfile });

            // Set delegates for handling errors
            /*if (onClosedCallback != null)
                _rtmpPublishSession.SessionClosed += onClosedCallback;

            if (onFailedCallback != null)
                _rtmpPublishSession.PublishFailed += onFailedCallback;*/

            // Retrieve sink
            _sink = await _rtmpPublishSession.GetCaptureSinkAsync();

            //_lowlagCapture = await _mediaCapture.PrepareLowLagRecordToCustomSinkAsync(_pubProfile.TargetEncodingProfile, _sink);
            //await _lowlagCapture.StartAsync();

            VideoEncodingProperties srcvideoencodingpropsh264 = srcvideoencodingprops;
            srcvideoencodingpropsh264.Subtype = MediaEncodingSubtypes.H264;

            await _mediaCapture.SetEncodingPropertiesAsync(STREAM_TYPE, srcvideoencodingpropsh264, _pubProfile.TargetEncodingProfile.Video.Properties);

            // Now we're sure we get the proper media format
            await _mediaCapture.StartRecordToCustomSinkAsync(_pubProfile.TargetEncodingProfile, _sink);

            await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(STREAM_TYPE, srcvideoencodingprops);
            await _mediaCapture.AudioDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Audio, srcaudioencodingprops);

            //	gr: taken from here https://forums.hololens.com/discussion/2009/mixedrealitycapture
            // Historical context: https://github.com/VulcanTechnologies/HoloLensCameraStream/issues/6
            /*if (cameraParams.rotateImage180Degrees)
            {
                srcvideoencodingprops.Properties.Add(ROTATION_KEY, 180);
            }*/

            IVideoEffectDefinition ved = new VideoMRCSettings(cameraParams.enableHolograms, cameraParams.enableVideoStabilization, cameraParams.videoStabilizationBufferSize, cameraParams.hologramOpacity);
            await _mediaCapture.AddVideoEffectAsync(ved, STREAM_TYPE);

            // And audio effect to capture too
            IAudioEffectDefinition aed = new AudioMRCSettings();
            await _mediaCapture.AddAudioEffectAsync(aed);

            

            this.IsStreamingRTMP = true;
        }

        async public Task stopRTMPStreamingAsync()
        {
            if (_lowlagCapture != null)
            {
                await _lowlagCapture.StopAsync();
                _lowlagCapture = null;
            }

            if (this.IsStreamingRTMP)
                await _mediaCapture.StopPreviewAsync();
        }

        async public Task pauseRTMPStreamingAsync()
        {
            if (_lowlagCapture != null)
                await _lowlagCapture.PauseAsync(MediaCapturePauseBehavior.RetainHardwareResources);

            if (this.IsStreamingRTMP)
                await _mediaCapture.StopPreviewAsync();

            this.IsStreamingRTMP = false;
        }

        async public Task resumeRTMPStreamingAsync()
        {
            if (_lowlagCapture != null)
                await _lowlagCapture.ResumeAsync();

            if (this.IsStreamingRTMP)
                await _mediaCapture.StartPreviewToCustomSinkAsync(_pubProfile.TargetEncodingProfile, _sink);

            this.IsStreamingRTMP = true;
        }

        /// <summary>
        /// Asynchronously starts video mode.
        /// 
        /// Activates the web camera with the various settings specified in CameraParameters.
        /// Only one VideoCapture instance can start the video mode at any given time.
        /// After starting the video mode, you listen for new video frame samples via the VideoCapture.FrameSampleAcquired event, 
        /// or by calling VideoCapture.RequestNextFrameSample() when will return the next available sample.
        /// While in video mode, more power will be consumed so make sure that you call VideoCapture.StopVideoModeAsync qhen you can afford the start/stop video mode overhead.
        /// </summary>
        /// <param name="setupParams">Parameters that change how video mode is used.</param>
        /// <param name="onVideoModeStartedCallback">This callback will be invoked once video mode has been activated.</param>
        public async void StartVideoModeAsync(CameraParameters setupParams, OnVideoModeStartedCallback onVideoModeStartedCallback)
        {
            var mediaFrameSource = _mediaCapture.FrameSources[_frameSourceInfo.Id]; //Returns a MediaFrameSource
            
            if (mediaFrameSource == null)
            {
                onVideoModeStartedCallback?.Invoke(new VideoCaptureResult(1, ResultType.UnknownError, false));
                return;
            }

            var pixelFormat = ConvertCapturePixelFormatToMediaEncodingSubtype(setupParams.pixelFormat);
            _frameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSource, pixelFormat);
            _frameReader.FrameArrived += HandleFrameArrived;
            await _frameReader.StartAsync();
            VideoEncodingProperties properties = GetVideoEncodingPropertiesForCameraParams(setupParams);

            // Historical context: https://github.com/VulcanTechnologies/HoloLensCameraStream/issues/6
            if (setupParams.rotateImage180Degrees)
            {
                properties.Properties.Add(ROTATION_KEY, 180);
            }
			
			//	gr: taken from here https://forums.hololens.com/discussion/2009/mixedrealitycapture
			IVideoEffectDefinition ved = new VideoMRCSettings( setupParams.enableHolograms, setupParams.enableVideoStabilization, setupParams.videoStabilizationBufferSize, setupParams.hologramOpacity );
			await _mediaCapture.AddVideoEffectAsync(ved, MediaStreamType.VideoPreview);
        
            await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, properties);

            onVideoModeStartedCallback?.Invoke(new VideoCaptureResult(0, ResultType.Success, true));
        }

        /// <summary>
        /// Returns a new VideoFrameSample as soon as the next one is available.
        /// This method is preferable to listening to the FrameSampleAcquired event
        /// in circumstances where most or all frames are not needed. For instance, if
        /// you were planning on sending frames to a remote image recognition service twice per second,
        /// you may consider using this method rather than ignoring most of the event dispatches from FrameSampleAcquired.
        /// This will avoid the overhead of acquiring and disposing of unused frames.
        /// 
        /// If, for whatever reason, a frame reference cannot be obtained, it is possible that the callback will return a null sample.
        /// </summary>
        /// <param name="onFrameSampleAcquired"></param>
        public void RequestNextFrameSample(FrameSampleAcquiredCallback onFrameSampleAcquired)
        {
            if (onFrameSampleAcquired == null)
            {
                throw new ArgumentNullException("onFrameSampleAcquired");
            }

            if (IsStreaming == false)
            {
                throw new Exception("You cannot request a frame sample until the video mode is started.");
            }

            TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> handler = null;
            handler = (MediaFrameReader sender, MediaFrameArrivedEventArgs args) =>
            {
                using (var frameReference = _frameReader.TryAcquireLatestFrame()) //frame: MediaFrameReference
                {
                    if (frameReference != null)
                    {
                        onFrameSampleAcquired.Invoke(new VideoCaptureSample(frameReference, worldOrigin));
                    }
                    else
                    {
                        onFrameSampleAcquired.Invoke(null);
                    }
                }
                _frameReader.FrameArrived -= handler;
            };
            _frameReader.FrameArrived += handler;
        }

        /// <summary>
        /// Asynchronously stops video mode.
        /// </summary>
        /// <param name="onVideoModeStoppedCallback">This callback will be invoked once video mode has been deactivated.</param>
        public async void StopVideoModeAsync(OnVideoModeStoppedCallback onVideoModeStoppedCallback)
        {
            if (IsStreaming == false)
            {
                onVideoModeStoppedCallback?.Invoke(new VideoCaptureResult(1, ResultType.InappropriateState, false));
                return;
            }

            _frameReader.FrameArrived -= HandleFrameArrived;
            await _frameReader.StopAsync();
            _frameReader.Dispose();
            _frameReader = null;

            onVideoModeStoppedCallback?.Invoke(new VideoCaptureResult(0, ResultType.Success, true));
        }

        /// <summary>
        /// Dispose must be called to shutdown the PhotoCapture instance.
        /// 
        /// If your VideoCapture instance successfully called VideoCapture.StartVideoModeAsync,
        /// you must make sure that you call VideoCapture.StopVideoModeAsync before disposing your VideoCapture instance.
        /// </summary>
        public void Dispose()
        {
            if (IsStreaming)
            {
                throw new Exception("Please make sure StopVideoModeAsync() is called before displosing the VideoCapture object.");
            }
            
            _mediaCapture?.Dispose();
        }

        async Task CreateMediaCaptureAsync()
        {
            await this.logAllSupportedDevicesAndModes();


            if (_mediaCapture != null)
            {
                throw new Exception("The MediaCapture object has already been created.");
            }

            _mediaCapture = new MediaCapture();

            // List capture profile, and select the best one for communication
            IReadOnlyList<MediaCaptureVideoProfile> profiles = MediaCapture.FindKnownVideoProfiles(_deviceInfo.Id, KnownVideoProfile.VideoConferencing);
            MediaCaptureVideoProfile selectedProfile = null;
            if (profiles.Count > 0)
                selectedProfile = profiles[0];
           
            if (selectedProfile == null)
            {
                throw new Exception("Unable to select VideoConferencing video profile");
            }

            // Handle failed event
            _mediaCapture.Failed += _mediaCapture_Failed;
            
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = _deviceInfo.Id,
                AudioDeviceId = _audioDeviceInfo.Id,
                SourceGroup = _frameSourceGroup,
                MemoryPreference = MediaCaptureMemoryPreference.Auto,
                StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                VideoProfile = selectedProfile,
                MediaCategory = MediaCategory.Communications,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly
            });

            //_mediaCapture.VideoDeviceController.Focus.TrySetAuto(true);

        }

        private void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.LogFormat("Media Capture Failed for reason: {0} => {1}", errorEventArgs.Code, errorEventArgs.Message);
        }

        void HandleFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (FrameSampleAcquired == null)
            {
                return;
            }

            using (var frameReference = _frameReader.TryAcquireLatestFrame()) //frameReference is a MediaFrameReference
            {
                if (frameReference != null)
                {
                    var sample = new VideoCaptureSample(frameReference, worldOrigin);
                    FrameSampleAcquired?.Invoke(sample);
                }
            }
        }

        VideoEncodingProperties GetVideoEncodingPropertiesForCameraParams(
            CameraParameters cameraParams,
            MediaStreamType streamType = MediaStreamType.VideoPreview,
            string subtype = null
         )
        {

            Debug.LogFormat("Requesting capture format {0}x{1} @ {2}", cameraParams.cameraResolutionWidth, cameraParams.cameraResolutionHeight, cameraParams.frameRate);

            var allPropertySets = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select((x) => x as VideoEncodingProperties)
                .Where((x) =>
            {
                if (x == null) return false;
                if (x.FrameRate.Denominator == 0) return false;

                double calculatedFrameRate = (double)x.FrameRate.Numerator / (double)x.FrameRate.Denominator;

                Debug.LogFormat("Found Video Stream Profile {0}x{1} @ {2} - {3}:{4} - {5} KBits - Subtype {6}", x.Width, x.Height,
                                    calculatedFrameRate, x.PixelAspectRatio.Numerator, x.PixelAspectRatio.Denominator, x.Bitrate, x.Subtype);

                if (!String.IsNullOrEmpty(subtype) && x.Subtype.ToUpper() != subtype.ToUpper())
                {
                    Debug.LogFormat("Subtype mismatch {0} <> {1}", x.Subtype, subtype);
                    return false;
                }


                return x.Width == (uint)cameraParams.cameraResolutionWidth &&
                        x.Height == (uint)cameraParams.cameraResolutionHeight &&
                        (int)Math.Round(calculatedFrameRate) == cameraParams.frameRate;
            }); //Returns IEnumerable<VideoEncodingProperties>

            if (allPropertySets.Count() == 0)
            {
                throw new Exception("Could not find a video encoding property set that matches the given camera parameters.");
            }
            
            var chosenPropertySet = allPropertySets.FirstOrDefault();
            return chosenPropertySet;
        }

        AudioEncodingProperties GetAudioEncodingPropertiesForAudioParams(AudioParameters audioParams, MediaStreamType streamType = MediaStreamType.Audio)
        {

            Debug.LogFormat("Requesting audio format {0}Hz {1}", audioParams.SampleRate, audioParams.ChannelCount == 1U ? "Mono" : "Stereo");

            var allPropertySets = _mediaCapture.AudioDeviceController.GetAvailableMediaStreamProperties(streamType).Select((x) => x as AudioEncodingProperties)
                .Where((x) =>
                {
                    if (x == null) return false;

                    Debug.LogFormat("Found Audio Stream Profile {0}Hz @ {1} bits - {2} chan - - Subtype {3}", x.SampleRate, x.Bitrate, x.ChannelCount, x.Subtype);

                    return
                        x.SampleRate == (uint)audioParams.SampleRate &&
                        x.ChannelCount == (uint)audioParams.ChannelCount;
                }); //Returns IEnumerable<AudioEncodingProperties>

            if (allPropertySets.Count() == 0)
            {
                throw new Exception("Could not find an audio encoding property set that matches the given camera parameters.");
            }

            var chosenPropertySet = allPropertySets.FirstOrDefault();
            return chosenPropertySet;
        }

        static bool IsColorVideo(MediaFrameSourceInfo sourceInfo, MediaStreamType streamType = MediaStreamType.VideoPreview)
        {
            //TODO: Determine whether 'VideoPreview' or 'VideoRecord' is the appropriate type. What's the difference?
            return (sourceInfo.MediaStreamType == streamType &&
                sourceInfo.SourceKind == MediaFrameSourceKind.Color);
        }

        static string ConvertCapturePixelFormatToMediaEncodingSubtype(CapturePixelFormat format)
        {
            switch (format)
            {
                case CapturePixelFormat.BGRA32:
                    return MediaEncodingSubtypes.Bgra8;
                case CapturePixelFormat.NV12:
                    return MediaEncodingSubtypes.Nv12;
                case CapturePixelFormat.JPEG:
                    return MediaEncodingSubtypes.Jpeg;
                case CapturePixelFormat.PNG:
                    return MediaEncodingSubtypes.Png;
                case CapturePixelFormat.H264:
                    return MediaEncodingSubtypes.H264;
                default:
                    return MediaEncodingSubtypes.Bgra8;
            }
        }
    }


	//	from https://forums.hololens.com/discussion/2009/mixedrealitycapture
	public class VideoMRCSettings : IVideoEffectDefinition
    {
        public string ActivatableClassId
        {
            get
            {
                return "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";
            }
        }

        public IPropertySet Properties
        {
            get; private set;
        }
        
        public VideoMRCSettings(bool HologramCompositionEnabled, bool VideoStabilizationEnabled, int VideoStabilizationBufferLength, float GlobalOpacityCoefficient)
        {
            Properties = (IPropertySet)new PropertySet();
            Properties.Add("HologramCompositionEnabled", HologramCompositionEnabled);
            Properties.Add("VideoStabilizationEnabled", VideoStabilizationEnabled);
            Properties.Add("VideoStabilizationBufferLength", VideoStabilizationBufferLength);
            Properties.Add("GlobalOpacityCoefficient", GlobalOpacityCoefficient);
            Properties.Add("RecordingIndicatorEnabled", false);
            Properties.Add("PreferredHologramPerspective", 1); // Force rendering from PhotoVideo camera
        }
    }

    public class AudioMRCSettings : IAudioEffectDefinition
    {
        public string ActivatableClassId
        {
            get
            {
                return "Windows.Media.MixedRealityCapture.MixedRealityCaptureAudioEffect";
            }
        }

        public IPropertySet Properties
        {
            get; private set;
        }

        /**
         * mixerMode: 0 = Mic, 1 = System, 2 = Both
         *  
         */
        public AudioMRCSettings(uint mixerMode = 2, float loopbackGain = 0.0f, float microphoneGain = 0.0f)
        {
            Properties = (IPropertySet)new PropertySet();
            Properties.Add("MixerMode", mixerMode);
            Properties.Add("LoopbackGain", loopbackGain);
            Properties.Add("MicrophoneGain", microphoneGain);

        }
    }
}
