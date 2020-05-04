﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Pipeline component that converts an Image to a different format.
    /// </summary>
    internal class ToPixelFormat : ConsumerProducer<Shared<Image>, Shared<Image>>
    {
        private PixelFormat pixelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToPixelFormat"/> class.
        /// </summary>
        /// <param name="pipeline">The pipline.</param>
        /// <param name="pixelFormat">The pixel format to conver to.</param>
        internal ToPixelFormat(Pipeline pipeline, PixelFormat pixelFormat)
            : base(pipeline)
        {
            this.pixelFormat = pixelFormat;
        }

        /// <summary>
        /// Receiver for incoming image.
        /// </summary>
        /// <param name="sharedImage">The incoming image.</param>
        /// <param name="e">The message envelope for the incoming image.</param>
        protected override void Receive(Shared<Image> sharedImage, Envelope e)
        {
            // if it has the same format, shortcut the loop
            if (this.pixelFormat == sharedImage.Resource.PixelFormat)
            {
                this.Out.Post(sharedImage, e.OriginatingTime);
            }
            else
            {
                using (var image = ImagePool.GetOrCreate(sharedImage.Resource.Width, sharedImage.Resource.Height, this.pixelFormat))
                {
                    sharedImage.Resource.CopyTo(image.Resource);
                    this.Out.Post(image, e.OriginatingTime);
                }
            }
        }
    }
}