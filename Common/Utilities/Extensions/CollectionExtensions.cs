//-----------------------------------------------------------------------
// <copyright file="CollectionExtensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

/// <summary>Global scope collection related extensions</summary>
[SuppressMessage("Microsoft.Design", "CA1050", Justification = "Global by design")]
public static class CollectionExtensions
{
    /// <summary>Maximum depth to map objects to dictionaries</summary>
    private const int MaximumObjectToDictionaryDepth = 3;

    /// <summary>
    /// Random number generator for IEnumerable&lt;TValue&gt;.Random()
    /// </summary>
    private static readonly Random random = new Random();

    /// <summary>
    /// Converts an enumerable of KeyValuePair to a dictionary
    /// </summary>
    /// <typeparam name="TKey">Type of the keys</typeparam>
    /// <typeparam name="TValue">Type of the values</typeparam>
    /// <param name="source">The enumerable</param>
    /// <returns>The dictionary</returns>
    [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting is desired here to avoid it elsewhere")]
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
        return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Merges two sequences into a sequence of tuples
    /// </summary>
    /// <typeparam name="TFirst">
    /// Type of the elements of the first input sequence.
    /// </typeparam>
    /// <typeparam name="TSecond">
    /// Type of the elements of the second input sequence.
    /// </typeparam>
    /// <param name="first">The first sequence to merge.</param>
    /// <param name="second">The second sequence to merge.</param>
    /// <returns>
    /// An System.Collections.Generic.IEnumerable&lt;Tuple&lt;TFirst, TSecond&gt;&gt;
    /// that contains merged elements of two input sequences.
    /// </returns>
    [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting is desired here to avoid it elsewhere")]
    public static IEnumerable<Tuple<TFirst, TSecond>> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
    {
        return first.Zip(second, (a, b) => new Tuple<TFirst, TSecond>(a, b));
    }

    /// <summary>Adds all the elements of a sequence to a collection</summary>
    /// <typeparam name="TValue">Type of the elements.</typeparam>
    /// <param name="destination">Destination collection.</param>
    /// <param name="source">Source sequence.</param>
    /// <returns>The list.</returns>
    public static ICollection<TValue> Add<TValue>(this ICollection<TValue> destination, IEnumerable<TValue> source)
    {
        foreach (var value in source)
        {
            destination.Add(value);
        }

        return destination;
    }

    /// <summary>
    /// Copies KeyValuePairs from the source dictionary, overwriting any
    /// existing entries in the destination with values from the source.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys</typeparam>
    /// <typeparam name="TValue">Type of the values</typeparam>
    /// <param name="destination">Destination dictionary.</param>
    /// <param name="source">Source dictionary.</param>
    /// <returns>The dictionary.</returns>
    public static IDictionary<TKey, TValue> Add<TKey, TValue>(this IDictionary<TKey, TValue> destination, IDictionary<TKey, TValue> source)
    {
        return Add(destination, source, true);
    }

    /// <summary>Copies KeyValuePairs from the source dictionary.</summary>
    /// <typeparam name="TKey">Type of the keys</typeparam>
    /// <typeparam name="TValue">Type of the values</typeparam>
    /// <param name="destination">Destination dictionary.</param>
    /// <param name="source">Source dictionary.</param>
    /// <param name="overwrite">Whether to overwrite existing values.</param>
    /// <returns>The dictionary.</returns>
    public static IDictionary<TKey, TValue> Add<TKey, TValue>(this IDictionary<TKey, TValue> destination, IDictionary<TKey, TValue> source, bool overwrite)
    {
        foreach (var kvp in source)
        {
            if (overwrite)
            {
                destination[kvp.Key] = kvp.Value;
            }
            else
            {
                destination.Add(kvp);
            }
        }

        return destination;
    }

    /// <summary>
    /// Removes the elements matching the predicate from the
    /// System.Collections.Generic.IDictionary&lt;TSource&gt;.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys</typeparam>
    /// <typeparam name="TValue">Type of the values</typeparam>
    /// <param name="source">
    /// The IDictionary&lt;TKey, TValue&gt; to remove elements from.
    /// </param>
    /// <param name="predicate">
    /// A function to test each element for a condition.
    /// </param>
    /// <returns>The number of elements removed.</returns>
    [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting here is fine.")]
    public static int Remove<TKey, TValue>(this IDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        var keys = source
            .Where(predicate)
            .Select(kvp => kvp.Key)
            .ToArray();
        
        // The keys must be realized into an array otherwise the below will
        // attempt to modify the dictionary while it is being enumerated.
        foreach (var key in keys)
        {
            source.Remove(key);
        }

        return keys.Length;
    }

    /// <summary>Returns a random element from an enumeration</summary>
    /// <typeparam name="TSource">Type of the source elements</typeparam>
    /// <param name="source">The source enumeration</param>
    /// <returns>A random element</returns>
    public static TSource Random<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null || source.Count() == 0)
        {
            throw new ArgumentException("source must not be null or empty", "source");
        }

        return source.ElementAt(random.Next(source.Count()));
    }

    /// <summary>Returns distinct elements from a source by using a specified selector to compare values.</summary>
    /// <remarks>When the selected value of multiple elements are equal, the first element will be the one included.</remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TValue">The type of the selected values.</typeparam>
    /// <param name="source">The source to remove duplicate elements from.</param>
    /// <param name="selector">Expression that selects the value used to compare source elements.</param>
    /// <returns>An IEnumerable containing only the distinct elements from the source sequence.</returns>
    public static IEnumerable<TSource> Distinct<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector)
    {
        return source.Distinct(new SelectedValueEqualityComparer<TSource, TValue>(selector));
    }

    /// <summary>Creates a dictionary containing the properties of an object</summary>
    /// <typeparam name="TObj">Type of the object</typeparam>
    /// <param name="obj">The object</param>
    /// <returns>Dictionary of properties</returns>
    public static IDictionary<string, object> ToDictionaryValues<TObj>(this TObj obj)
    {
        return ToDictionaryValues<TObj>(obj, MaximumObjectToDictionaryDepth);
    }

    /// <summary>Creates a dictionary containing the properties of an object</summary>
    /// <typeparam name="TObj">Type of the object</typeparam>
    /// <param name="obj">The object</param>
    /// <param name="maxDepth">Maximum depth</param>
    /// <returns>Dictionary of properties</returns>
    public static IDictionary<string, object> ToDictionaryValues<TObj>(this TObj obj, int maxDepth)
    {
        return ToDictionaryValues(typeof(TObj), obj, 0, maxDepth);
    }

    /// <summary>Creates a dictionary containing the properties of an object</summary>
    /// <param name="type">Type of the object</param>
    /// <param name="obj">The object</param>
    /// <param name="depth">Current recursion depth</param>
    /// <param name="maxDepth">Maximum recursion depth</param>
    /// <returns>Dictionary of properties</returns>
    private static IDictionary<string, object> ToDictionaryValues(Type type, object obj, int depth, int maxDepth)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var values = new Dictionary<string, object>();
        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(obj, new object[0]);
            var propertyType = property.PropertyType;
            if (propertyType.IsValueType || propertyType == typeof(string))
            {
                if (propertyType.FullName.StartsWith("System.", StringComparison.Ordinal))
                {
                    values.Add(property.Name, propertyValue);
                }
                else if (propertyType.IsEnum)
                {
                    values.Add(property.Name, Enum.GetName(propertyType, propertyValue));
                }
                else
                {
                    values.Add(property.Name, "{0}".FormatInvariant(propertyValue));
                }
            }
            else if (depth < maxDepth)
            {
                values.Add(property.Name, ToDictionaryValues(property.PropertyType, propertyValue, depth + 1, maxDepth));
            }
        }

        return values;
    }

    /// <summary>Equality comparer that uses source elements' selected values.</summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TValue">The type of the selected values.</typeparam>
    private class SelectedValueEqualityComparer<TSource, TValue> : EqualityComparer<TSource>
    {
        /// <summary>Expression that selects the value used to compare source elements.</summary>
        private Func<TSource, TValue> selector;

        /// <summary>Initializes a new instance of the SelectedValueEqualityComparer class.</summary>
        /// <param name="selector">Expression that selects the value used to compare source elements.</param>
        public SelectedValueEqualityComparer(Func<TSource, TValue> selector)
        {
            this.selector = selector;
        }

        /// <summary>Determines whether two objects are equal using their selected values.</summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>True if the specified objects' selected values are equal; otherwise, false.</returns>
        public override bool Equals(TSource x, TSource y)
        {
            return this.selector(x).Equals(this.selector(y));
        }

        /// <summary>Gets the hash code of the object's selected value.</summary>
        /// <param name="obj">The object for which to get the selected value's hash code.</param>
        /// <returns>The object's selected value's hash code.</returns>
        public override int GetHashCode(TSource obj)
        {
            return this.selector(obj).GetHashCode();
        }
    }
}
