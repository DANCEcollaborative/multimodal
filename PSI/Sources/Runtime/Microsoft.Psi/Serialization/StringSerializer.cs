﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using Microsoft.Psi.Common;

    /// <summary>
    /// Simple string serializer.
    /// </summary>
    /// <remarks>Don't use this as a template for other custom serializers!!!.</remarks>
    internal sealed class StringSerializer : ISerializer<string>
    {
        private const int Version = 0;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            // schema is not used, since the behavior of arrays is hard-coded
            return null;
        }

        public void Clone(string instance, ref string target, SerializationContext context)
        {
        }

        public void Serialize(BufferWriter writer, string instance, SerializationContext context)
        {
            writer.Write(instance);
        }

        public void Deserialize(BufferReader reader, ref string target, SerializationContext context)
        {
            target = reader.ReadString();
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref string target, SerializationContext context)
        {
        }

        public void PrepareCloningTarget(string instance, ref string target, SerializationContext context)
        {
            target = instance;
        }

        public void Clear(ref string target, SerializationContext context)
        {
        }
    }
}
