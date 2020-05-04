﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// File persistence component.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class FileWriter<T> : IConsumer<T>, IDisposable
    {
        private FileStream file;
        private bool first = true;
        private dynamic state;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="filename">File name to which to persist.</param>
        /// <param name="serializer">Format serializer with which messages are serialized.</param>
        public FileWriter(Pipeline pipeline, string filename, IPersistentFormatSerializer serializer)
        {
            this.file = File.Create(filename);
            this.In = pipeline.CreateReceiver<T>(this, (m, e) => this.WriteRecord(m, e, serializer), nameof(this.In));
            this.In.Unsubscribed += _ => serializer.PersistFooter(this.file, this.state);
        }

        /// <inheritdoc />
        public Receiver<T> In { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.file != null)
            {
                this.file.Dispose();
                this.file = null;
            }
        }

        private void WriteRecord(T message, Envelope envelope, IPersistentFormatSerializer serializer)
        {
            if (this.first)
            {
                this.state = serializer.PersistHeader(message, this.file);
            }

            serializer.PersistRecord(message, envelope.OriginatingTime, this.first, this.file, this.state);
            this.first = false;
        }
    }
}
