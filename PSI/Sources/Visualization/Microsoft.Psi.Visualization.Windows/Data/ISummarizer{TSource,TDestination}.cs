﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents summarizers that perform interval-based data summarization over a series of data values.
    /// </summary>
    /// <typeparam name="TSource">The source data type.</typeparam>
    /// <typeparam name="TDestination">The summarized data type.</typeparam>
    public interface ISummarizer<TSource, TDestination> : ISummarizer
    {
        /// <summary>
        /// Combines two coincident or overlapping <see cref="IntervalData"/> values into a single value,
        /// the computation of which is implementation-specific and defined by the derived class.
        /// </summary>
        /// <param name="left">The first value to combine.</param>
        /// <param name="right">The second value to combine.</param>
        /// <returns>The combined value.</returns>
        IntervalData<TDestination> Combine(IntervalData<TDestination> left, IntervalData<TDestination> right);

        /// <summary>
        /// Summarizes data items into a series of summary values each spanning a time span no greater
        /// than the specified interval.
        /// </summary>
        /// <param name="items">The data items to be summarized.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>A list of <see cref="IntervalData"/> values each containing summarized data.</returns>
        List<IntervalData<TDestination>> Summarize(IEnumerable<Message<TSource>> items, TimeSpan interval);
    }
}
