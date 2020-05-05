﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        #region Scalar joins

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="outputCreator">Function mapping the primary and secondary messages to an output message type.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<TOut> Join<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                new[] { secondary },
                interpolator,
                (m, secondaryArray) => outputCreator(m, secondaryArray[0]),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<(TPrimary, TSecondary)> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(primary, secondary, Reproducible.Exact<TSecondary>(), primaryDeliveryPolicy, secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<(TPrimary, TSecondary)> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(primary, secondary, new RelativeTimeInterval(-tolerance, tolerance), primaryDeliveryPolicy, secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<(TPrimary, TSecondary)> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                ValueTuple.Create,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<(TPrimary, TSecondary)> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(primary, secondary, interpolator, ValueTuple.Create, primaryDeliveryPolicy, secondaryDeliveryPolicy);
        }

        #endregion Scalar joins

        #region Tuple-flattening scalar joins

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<TSecondary>(),
                (p, s) => (p.Item1, p.Item2, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(tolerance),
                (p, s) => (p.Item1, p.Item2, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                (p, s) => (p.Item1, p.Item2, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<TSecondary>(),
                (p, s) => (p.Item1, p.Item2, p.Item3, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(tolerance),
                (p, s) => (p.Item1, p.Item2, p.Item3, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                (p, s) => (p.Item1, p.Item2, p.Item3, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<TSecondary>(),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(tolerance),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<TSecondary>(),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(tolerance),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            ReproducibleInterpolator<TSecondary> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<TSecondary>(),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(tolerance),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<TSecondary>(relativeTimeInterval),
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion Tuple-flattening scalar joins

        #region Reverse tuple-flattening scalar joins

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            ReproducibleInterpolator<(TSecondaryItem1, TSecondaryItem2)> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<(TSecondaryItem1, TSecondaryItem2)>(),
                (p, s) => (p, s.Item1, s.Item2),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2)>(tolerance),
                (p, s) => (p, s.Item1, s.Item2),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2)>(relativeTimeInterval),
                (p, s) => (p, s.Item1, s.Item2),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            ReproducibleInterpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)>(),
                (p, s) => (p, s.Item1, s.Item2, s.Item3),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)>(tolerance),
                (p, s) => (p, s.Item1, s.Item2, s.Item3),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)>(relativeTimeInterval),
                (p, s) => (p, s.Item1, s.Item2, s.Item3),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            ReproducibleInterpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)>(),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)>(tolerance),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)>(relativeTimeInterval),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            ReproducibleInterpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)>(),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)>(tolerance),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)>(relativeTimeInterval),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values from a secondary stream based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            ReproducibleInterpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with values with the same originating time from a secondary stream.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Exact{T}"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Exact<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)>(),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified time tolerance.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <param name="tolerance">Time tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            TimeSpan tolerance,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)>(tolerance),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Join with the nearest values from a secondary stream, within a specified relative time interval.
        /// </summary>
        /// <remarks>Uses the <see cref="Reproducible.Nearest{T}(RelativeTimeInterval)"/> interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <param name="relativeTimeInterval">Relative time interval tolerance.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of joined tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            return Join(
                primary,
                secondary,
                Reproducible.Nearest<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)>(relativeTimeInterval),
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion Reverse tuple-flattening scalar joins

        #region Vector joins

        /// <summary>
        /// Joins a primary stream with an enumeration of secondary streams based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary stream messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary stream messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondaries">Enumeration of secondary streams.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="outputCreator">Mapping function from primary and secondary messages to output.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondariesDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Join<TPrimary, TSecondary, TInterpolation, TOut>(
            this IProducer<TPrimary> primary,
            IEnumerable<IProducer<TSecondary>> secondaries,
            ReproducibleInterpolator<TSecondary, TInterpolation> interpolator,
            Func<TPrimary, TInterpolation[], TOut> outputCreator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondariesDeliveryPolicy = null)
        {
            var join = new Join<TPrimary, TSecondary, TInterpolation, TOut>(
                primary.Out.Pipeline,
                interpolator,
                outputCreator,
                secondaries.Count(),
                null);

            primary.PipeTo(join.InPrimary, primaryDeliveryPolicy);

            var i = 0;
            foreach (var input in secondaries)
            {
                input.PipeTo(join.InSecondaries[i++], secondariesDeliveryPolicy);
            }

            return join;
        }

        /// <summary>
        /// Joins an enumeration of streams into a vector stream, based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TIn">Type of input stream messages.</typeparam>
        /// <param name="inputs">Collection of input streams.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy to use for the streams.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TIn[]> Join<TIn>(
            this IEnumerable<IProducer<TIn>> inputs,
            ReproducibleInterpolator<TIn> interpolator,
            DeliveryPolicy deliveryPolicy = null)
        {
            var count = inputs.Count();
            if (count > 1)
            {
                var buffer = new TIn[count];
                return Join(
                    inputs.First(),
                    inputs.Skip(1),
                    interpolator,
                    (m, secondaryArray) =>
                    {
                        buffer[0] = m;
                        Array.Copy(secondaryArray, 0, buffer, 1, count - 1);
                        return buffer;
                    },
                    deliveryPolicy,
                    deliveryPolicy);
            }
            else if (count == 1)
            {
                return inputs.First().Select(x => new[] { x }, deliveryPolicy);
            }
            else
            {
                throw new ArgumentException("Vector join with empty inputs collection.");
            }
        }

        /// <summary>
        /// Joins a primary stream of integers with an enumeration of seconary streams based on a specified reproducible interpolator.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TInterpolation[]> Join<TIn, TInterpolation>(
            this IProducer<int> primary,
            IEnumerable<IProducer<TIn>> inputs,
            ReproducibleInterpolator<TIn, TInterpolation> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            var join = new Join<int, TIn, TInterpolation, TInterpolation[]>(
                primary.Out.Pipeline,
                interpolator,
                (count, values) => values,
                inputs.Count(),
                count => Enumerable.Range(0, count));

            primary.PipeTo(join.InPrimary, primaryDeliveryPolicy);

            var i = 0;
            foreach (var input in inputs)
            {
                input.PipeTo(join.InSecondaries[i++], secondaryDeliveryPolicy);
            }

            return join;
        }

        #endregion Vector joins

        #region Sparse vector (dictionary) joins

        /// <summary>
        /// Sparse vector join.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key values.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Output stream.</returns>
        public static Join<Dictionary<TKey, int>, TIn, TInterpolation, Dictionary<TKey, TInterpolation>> Join<TIn, TKey, TInterpolation>(
            this IProducer<Dictionary<TKey, int>> primary,
            IEnumerable<IProducer<TIn>> inputs,
            ReproducibleInterpolator<TIn, TInterpolation> interpolator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            var buffer = new Dictionary<TKey, TInterpolation>();
            var join = new Join<Dictionary<TKey, int>, TIn, TInterpolation, Dictionary<TKey, TInterpolation>>(
                primary.Out.Pipeline,
                interpolator,
                (keys, values) =>
                {
                    buffer.Clear();
                    foreach (var keyPair in keys)
                    {
                        buffer[keyPair.Key] = values[keyPair.Value];
                    }

                    return buffer;
                },
                inputs.Count(),
                keys => keys.Select(p => p.Value));

            primary.PipeTo(join.InPrimary, primaryDeliveryPolicy);

            var i = 0;
            foreach (var input in inputs)
            {
                input.PipeTo(join.InSecondaries[i++], secondaryDeliveryPolicy);
            }

            return join;
        }
    }

    #endregion Sparse vector joins
}