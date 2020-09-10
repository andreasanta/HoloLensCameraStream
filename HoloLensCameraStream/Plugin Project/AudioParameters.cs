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
    public class AudioParameters
    {
        public uint SampleRate { get; set; }
        //
        // Summary:
        //     Gets or sets the number of audio channels.
        //
        // Returns:
        //     The number of audio channels.
        public uint ChannelCount { get; set; }

        AudioParameters() { }


        public AudioParameters(
            uint sampleRate = 16000, // Default for hololens 2
            uint channelCount = 1U)
        {
            this.SampleRate = sampleRate;
            this.ChannelCount = channelCount;
        }
    }
}
