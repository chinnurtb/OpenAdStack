// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityTestHelpers.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Static helper methods for entity testing
/// </summary>
internal static class EntityTestHelpers
{
    /// <summary>Helper method to round-trip assert a property</summary>
    /// <param name="expectedValue">The initial value of the property to set.</param>
    /// <param name="newValue">The property value to set.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="targetObject">The object with the property.</param>
    public static void AssertPropertyAccessors(PropertyValue expectedValue, PropertyValue newValue, string propertyName, object targetObject)
    {
        var expectedProperty = new EntityProperty { Name = propertyName, Value = expectedValue };
        var newProperty = new EntityProperty { Name = propertyName, Value = newValue };

        // Assert member get original value
        var actualProperty = targetObject.GetType().GetProperty(propertyName).GetValue(targetObject, null);
        Assert.AreEqual(expectedProperty, actualProperty);

        // Assert member set round-trip
        targetObject.GetType().GetProperty(propertyName).SetValue(targetObject, newProperty, null);
        actualProperty = targetObject.GetType().GetProperty(propertyName).GetValue(targetObject, null);
        Assert.AreEqual(actualProperty, newProperty);
    }
}
