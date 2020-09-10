//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

namespace HoloLensCameraStream
{
    /// <summary>
    /// When calling VideoCapture.StartPhotoModeAsync, you must pass in a CameraParameters object
    /// that contains the various settings that the web camera will use.
    /// </summary>
    public class CameraParameters
    {
        /// <summary>
        /// The pixel format used to capture and record your image data.
        /// </summary>
        public CapturePixelFormat pixelFormat;

        /// <summary>
        /// The height of the image frame. Must be a valid option.
        /// Valid options can be obtained from VideoCapture.SupportedResolutions.
        /// </summary>
        public uint cameraResolutionHeight;

        /// <summary>
        /// The width of the image frame. Must be a valid option.
        /// Valid options can be obtained from VideoCapture.SupportedResolutions.
        /// </summary>
        public uint cameraResolutionWidth;

        /// <summary>
        /// The frames per second that the video will stream at. Must be supported based on the selected resolution.
        /// </summary>
        public uint frameRate;

		/// <summary>
        /// Rotate image by 180 degrees if you find it to be upside down in your application.
        /// Depending on your usage, some image readers will require this to be toggled.
        /// </summary>
        public bool rotateImage180Degrees;

		/// <summary>
        /// EXPERIMENTAL: Enable holograms if opacity > 0. (internally these are two options, but rolled into one)
		/// https://developer.microsoft.com/en-us/windows/mixed-reality/mixed_reality_capture_for_developers
        /// </summary>
        public float hologramOpacity;

        /// <summary>
        /// EXPERIMENTAL: Sets the hologram opacity to opaque if true, or to invisible if false.
        /// </summary>
		public bool enableHolograms
		{
			get {	return hologramOpacity > 0.0f; }
			set {	hologramOpacity = value ? 0.9f : 0.0f; }
		}

        /// <summary>
        /// EXPERIMENTAL: Enable Video stabilisation and set the buffer amount. Docs recommend 15.
        /// https://developer.microsoft.com/en-us/windows/mixed-reality/mixed_reality_capture_for_developers
        /// </summary>
        public int videoStabilizationBufferSize;

        /// <summary>
        /// EXPERIMENTAL: Flag to enable or disable video stabilization powered by the HoloLens tracker.
        /// </summary>
		public bool enableVideoStabilization
		{
			get {	return videoStabilizationBufferSize > 0; }
			set {	videoStabilizationBufferSize = value ? 15 : 0; }
		}

        CameraParameters() { }


        public CameraParameters(
            CapturePixelFormat pixelFormat = CapturePixelFormat.NV12,
            uint cameraResolutionHeight = 720,
            uint cameraResolutionWidth = 1280,
            uint frameRate = 30,
            bool rotateImage180Degrees = true
        )
        {
            this.pixelFormat = pixelFormat;
            this.cameraResolutionHeight = cameraResolutionHeight;
            this.cameraResolutionWidth = cameraResolutionWidth;
            this.frameRate = frameRate;
			this.rotateImage180Degrees = rotateImage180Degrees;
            enableHolograms = true;
        }
    }
}
