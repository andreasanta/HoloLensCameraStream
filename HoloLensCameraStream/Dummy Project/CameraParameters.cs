//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using System;

namespace HoloLensCameraStream
{
    public class CameraParameters
    {
        public CapturePixelFormat pixelFormat;

        public uint cameraResolutionHeight;

        public uint cameraResolutionWidth;

        public uint frameRate;

		public bool rotateImage180Degrees;

		public float hologramOpacity;
		public bool enableHolograms
		{
			get {	throw new NotImplementedException(); }
			set {	throw new NotImplementedException(); }
		}

 		public int videoStabilizationBufferSize;
		public bool enableVideoStabilization
		{
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        CameraParameters() { }

        public CameraParameters(
            CapturePixelFormat pixelFormat = CapturePixelFormat.NV12,
            uint cameraResolutionHeight = 720,
            uint cameraResolutionWidth = 1280,
            uint frameRate = 30,
			bool rotateImage180Degrees = true
         )
        { throw new NotImplementedException(); }
    }
}
