//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoloLensCameraStream
{
    public delegate void OnVideoCaptureResourceCreatedCallback(VideoCapture captureObject);

    public delegate void OnVideoModeStartedCallback(VideoCaptureResult result);

    public delegate void FrameSampleAcquiredCallback(VideoCaptureSample videoCaptureSample);

    public delegate void OnVideoModeStoppedCallback(VideoCaptureResult result);

    public delegate void OnSessionPublishFailed(string reason);

    public delegate void OnSessionClosedHandler(string reason);

    public sealed class VideoCapture
    {
        public static IEnumerable<Resolution> SupportedResolutions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<float> SupportedFrameRatesForResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

#pragma warning disable 067
        public event FrameSampleAcquiredCallback FrameSampleAcquired;
#pragma warning restore 067

        public bool IsStreaming
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IntPtr WorldOriginPtr
        {
            get; set;
        }

        public static void CreateAsync(OnVideoCaptureResourceCreatedCallback onCreatedCallback)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Resolution> GetSupportedResolutions()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<float> GetSupportedFrameRatesForResolution(Resolution resolution)
        {
            throw new NotImplementedException();
        }

        public void StartVideoModeAsync(CameraParameters setupParams, OnVideoModeStartedCallback onVideoModeStartedCallback)
        {
            throw new NotImplementedException();
        }

        public void RequestNextFrameSample(FrameSampleAcquiredCallback onFrameSampleAcquired)
        {
            throw new NotImplementedException();
        }

        public void StopVideoModeAsync(OnVideoModeStoppedCallback onVideoModeStoppedCallback)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException("This can only be executed on the actual hololens");
        }

        public async Task stopRTMPStreamingAsync()
        {
            throw new NotImplementedException("This can only be executed on the actual hololens");
        }

        public async Task pauseRTMPStreamingAsync()
        {
            throw new NotImplementedException("This can only be executed on the actual hololens");
        }

        async public Task resumeRTMPStreamingAsync()
        {
            throw new NotImplementedException("This can only be executed on the actual hololens");
        }
    }
}
