﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Implements a converter from a boolean to a <see cref="Visibility"/>, with true
    /// corresponding to Visible, and false corresponding to Collapsed.
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && parameter == null)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                throw new ArgumentException();
            }

            return (Visibility)value == Visibility.Visible;
        }
    }
}
