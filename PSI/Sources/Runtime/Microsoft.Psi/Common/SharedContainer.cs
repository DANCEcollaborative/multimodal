﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Threading;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Provides a container that tracks the usage of a resource (such as a large memory allocation) and allows reusing it once not in use
    /// This class performs AddRef and Release and overrides serialization to preempt cloning from making deep copies of the resource.
    /// This class is for internal use only. The Shared class is the public-facing API for this functionality.
    /// </summary>
    /// <typeparam name="T">The type of data held by this container.</typeparam>
    [Serializer(typeof(SharedContainer<>.CustomSerializer))]
    internal class SharedContainer<T>
        where T : class
    {
        private readonly SharedPool<T> sharedPool;
        private int refCount;
        private T resource;

        internal SharedContainer(T resource, SharedPool<T> pool)
        {
            this.sharedPool = pool;
            this.resource = resource;
            this.refCount = 1;
        }

        public T Resource => this.resource;

        public SharedPool<T> SharedPool => this.sharedPool;

        public void AddRef()
        {
            var newVal = Interlocked.Increment(ref this.refCount);
        }

        public void Release()
        {
            if (this.resource == null)
            {
                return;
            }

            var newVal = Interlocked.Decrement(ref this.refCount);
            if (newVal == 0)
            {
                // return it to the pool
                if (this.sharedPool != null)
                {
                    this.sharedPool.Recycle(this.resource);
                }
                else
                {
                    if (this.resource is IDisposable)
                    {
                        ((IDisposable)this.resource).Dispose();
                    }
                }

                this.resource = null;
            }
            else if (newVal < 0)
            {
                throw new InvalidOperationException("The referenced object has been released too many times.");
            }
        }

        private class CustomSerializer : ISerializer<SharedContainer<T>>
        {
            public const int Version = 2;
            private SerializationHandler<T> handler;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                this.handler = serializers.GetHandler<T>();
                var type = this.GetType();
                var name = TypeSchema.GetContractName(type, serializers.RuntimeVersion);
                var resourceMember = new TypeMemberSchema("resource", typeof(T).AssemblyQualifiedName, true);
                var schema = new TypeSchema(name, TypeSchema.GetId(name), type.AssemblyQualifiedName, TypeFlags.IsClass, new TypeMemberSchema[] { resourceMember }, Version);
                return targetSchema ?? schema;
            }

            public void Serialize(BufferWriter writer, SharedContainer<T> instance, SerializationContext context)
            {
                // only serialize the resource.
                // The refCount needs not be serialized (it will be always 1 when deserializing)
                // The shared pool cannot be serialized, and needs to be provided by the deserializer, by providing a deserializing target that is already pool-aware
                this.handler.Serialize(writer, instance.resource, context);
            }

            public void PrepareCloningTarget(SharedContainer<T> instance, ref SharedContainer<T> target, SerializationContext context)
            {
                if (target != null)
                {
                    target.Release();
                }

                target = instance; // needs to be set to the final object so that single-instancing works correctly
            }

            public void Clone(SharedContainer<T> instance, ref SharedContainer<T> target, SerializationContext context)
            {
                target.AddRef();
            }

            public void PrepareDeserializationTarget(BufferReader reader, ref SharedContainer<T> target, SerializationContext context)
            {
                SharedPool<T> sharedPool = null;
                T resource = default(T);

                if (target != null)
                {
                    target.Release();
                    sharedPool = target.SharedPool;
                    sharedPool?.TryGet(out resource);
                }

                target = new SharedContainer<T>(resource, sharedPool);
            }

            public void Deserialize(BufferReader reader, ref SharedContainer<T> target, SerializationContext context)
            {
                this.handler.Deserialize(reader, ref target.resource, context);
            }

            public void Clear(ref SharedContainer<T> target, SerializationContext context)
            {
                // shared containers cannot be reused
                throw new InvalidOperationException();
            }
        }
    }
}
