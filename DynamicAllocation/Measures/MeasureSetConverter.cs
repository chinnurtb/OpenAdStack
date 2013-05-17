// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasureSetConverter.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>
    /// Converter for the MeasureSet class
    /// </summary>
    public class MeasureSetConverter : TypeConverter
    {
        /// <summary>
        /// Overrides the CanConvertFrom method of TypeConverter.
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="sourceType">the source type</param>
        /// <returns>if sourceType can be converted to MeasureSet</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Overrides the ConvertFrom method of TypeConverter.
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="culture">the culture</param>
        /// <param name="value">the value</param>
        /// <returns>value converted to a MeasureSet</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var valueAsString = value as string;
            if (valueAsString != null)
            {
                // TODO: error handling/logging?
                var measures = valueAsString.Split(new[] { ',' })
                    .Select(s => long.Parse(s, CultureInfo.InvariantCulture));
                return new MeasureSet(measures);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
