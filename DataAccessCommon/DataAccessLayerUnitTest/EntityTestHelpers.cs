// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityTestHelpers.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
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
