﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a time interval visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeIntervalVisualizationObject : TimelineVisualizationObject<Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalVisualizationObjectView));

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Configuration.Color;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override string LegendValue => this.CurrentValue.HasValue ? (this.CurrentValue.Value.Data.Item2 - this.CurrentValue.Value.Data.Item1).ToString() : string.Empty;
    }
}
