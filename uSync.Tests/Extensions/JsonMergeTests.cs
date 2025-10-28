#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using NUnit.Framework;

using SixLabors.ImageSharp.PixelFormats;

using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Tests.Extensions;

[TestFixture]
internal class JsonMergeTests
{
    private static readonly Type ImageCropperConfigMergerType = Type.GetType("uSync.Core.Roots.Configs.ImageCropperConfigMerger, uSync.Core")!;

    // Create an instance of the concrete merger for testing
    private object _mergerInstance = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mergerInstance = Activator.CreateInstance(ImageCropperConfigMergerType)!;
    }

    // Test helper class for MergeObjects tests
    private class TestObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsRemoved { get; set; }

        public override bool Equals(object? obj) => obj is TestObject other && Id == other.Id && Name == other.Name;
        public override int GetHashCode() => HashCode.Combine(Id, Name);
    }

    #region MergeObjects Tests

    [Test]
    public void MergeObjects_WithValidRootAndTargetObjects_MergesCorrectly()
    {
        // Arrange
        var rootObjects = new[]
        {
new TestObject { Id = "1", Name = "Root1" },
            new TestObject { Id = "2", Name = "Root2" },
   new TestObject { Id = "3", Name = "Root3" }
        };

        var targetObjects = new[]
         {
            new TestObject { Id = "2", Name = "Target2" }, // Override
       new TestObject { Id = "4", Name = "Target4" }  // New
    };

        var method = GetGenericStaticMethod("MergeObjects", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var predicate = new Predicate<TestObject>(x => x.IsRemoved);

        // Act
        var result = (TestObject[])method.Invoke(null, [rootObjects, targetObjects, keySelector, predicate])!;

        // Assert
        Assert.That(result, Has.Length.EqualTo(4));
        Assert.That(result.Any(x => x.Id == "1" && x.Name == "Root1"), Is.True);
        Assert.That(result.Any(x => x.Id == "2" && x.Name == "Target2"), Is.True);
        Assert.That(result.Any(x => x.Id == "3" && x.Name == "Root3"), Is.True);
        Assert.That(result.Any(x => x.Id == "4" && x.Name == "Target4"), Is.True);
    }

    [Test]
    public void MergeObjects_WithNullTargetObjects_ReturnsRootObjectsOnly()
    {
        // Arrange
        var rootObjects = new[]
                {
    new TestObject { Id = "1", Name = "Root1" },
            new TestObject { Id = "2", Name = "Root2" }
        };

        var method = GetGenericStaticMethod("MergeObjects", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var predicate = new Predicate<TestObject>(x => x.IsRemoved);

        // Act
        var result = (TestObject[])method.Invoke(null, [rootObjects, null, keySelector, predicate])!;

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result.Any(x => x.Id == "1"), Is.True);
        Assert.That(result.Any(x => x.Id == "2"), Is.True);
    }

    [Test]
    public void MergeObjects_WithRemovedItems_FiltersOutRemovedItems()
    {
        // Arrange
        var rootObjects = new[]
        {
            new TestObject { Id = "1", Name = "Root1" },
            new TestObject { Id = "2", Name = "Root2" }
 };

        var targetObjects = new[]
        {
    new TestObject { Id = "3", Name = "Target3", IsRemoved = true },
     new TestObject { Id = "4", Name = "Target4", IsRemoved = false }
        };

        var method = GetGenericStaticMethod("MergeObjects", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var predicate = new Predicate<TestObject>(x => x.IsRemoved);

        // Act
        var result = (TestObject[])method.Invoke(null, [rootObjects, targetObjects, keySelector, predicate])!;

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result.Any(x => x.Id == "3"), Is.False); // Removed item should not be present
        Assert.That(result.Any(x => x.Id == "4"), Is.True);
    }

    [Test]
    public void MergeObjects_WithStringKeysContainingRemovedLabel_HandlesCorrectly()
    {
        // Arrange
        var rootObjects = new[]
        {
    new TestObject { Id = "1", Name = "Root1" },
        new TestObject { Id = "2", Name = "Root2" }
        };

        var targetObjects = new[]
{
       new TestObject { Id = "uSync:Removed in child site.:1", Name = "Target1" }, // Contains removal label
     new TestObject { Id = "3", Name = "Target3" }
        };

        var method = GetGenericStaticMethod("MergeObjects", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var predicate = new Predicate<TestObject>(x => x.IsRemoved);

        // Act
        var result = (TestObject[])method.Invoke(null, [rootObjects, targetObjects, keySelector, predicate])!;

        // Assert
        // When the removal label is stripped from "uSync:Removed in child site.:1", it becomes "1"
        // This matches the root object with Id "1", so the root object is excluded from the merge
        // The final result should have: Target1 (with full removal label), Root2, Target3
        Assert.That(result, Has.Length.EqualTo(3));

        // Should include the target item with the full removal label
        Assert.That(result.Any(x => x.Id.Contains("uSync:Removed in child site.:1")), Is.True);
        // Should include Root2 since it has no matching target
        Assert.That(result.Any(x => x.Id == "2" && x.Name == "Root2"), Is.True);
        // Should include Target3
        Assert.That(result.Any(x => x.Id == "3" && x.Name == "Target3"), Is.True);
        // Should NOT include Root1 because it was excluded due to key matching after label stripping
        Assert.That(result.Any(x => x.Id == "1" && x.Name == "Root1"), Is.False);
    }

    #endregion

    #region GetObjectDifferences Tests

    [Test]
    public void GetObjectDifferences_WithRemovedItems_MarksThemAsRemoved()
    {
        // Arrange
        var rootObjects = new[]
        {
     new TestObject { Id = "1", Name = "Root1" },
            new TestObject { Id = "2", Name = "Root2" },
  new TestObject { Id = "3", Name = "Root3" }
        };

        var targetObjects = new[]
        {
          new TestObject { Id = "1", Name = "Target1" }, // Exists in both
   new TestObject { Id = "4", Name = "Target4" }  // Only in target
     };

        var method = GetGenericStaticMethod("GetObjectDifferences", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var setMarker = new Action<TestObject, string>((obj, marker) => obj.Name = $"{marker}:{obj.Name}");

        // Act
        var result = (TestObject[])method.Invoke(null, [rootObjects, targetObjects, keySelector, setMarker])!;

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));

        // Should contain items only in target
        Assert.That(result.Any(x => x.Id == "4" && x.Name == "Target4"), Is.True);

        // Should contain removed items from root marked with removal label
        var removedItems = result.Where(x => x.Name.StartsWith("uSync:Removed in child site.:")).ToArray();
        Assert.That(removedItems, Has.Length.EqualTo(2));
        Assert.That(removedItems.Any(x => x.Id == "2"), Is.True);
        Assert.That(removedItems.Any(x => x.Id == "3"), Is.True);
    }

    [Test]
    public void GetObjectDifferences_WithNullArrays_HandlesGracefully()
    {
        // Arrange
        var method = GetGenericStaticMethod("GetObjectDifferences", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var setMarker = new Action<TestObject, string>((obj, marker) => obj.Name = $"{marker}:{obj.Name}");

        // Act & Assert - null root objects
        var result1 = (TestObject[])method.Invoke(null, [null, new TestObject[0], keySelector, setMarker])!;
        Assert.That(result1, Has.Length.EqualTo(0));

        // Act & Assert - null target objects
        var result2 = (TestObject[])method.Invoke(null, [new TestObject[0], null, keySelector, setMarker])!;
        Assert.That(result2, Has.Length.EqualTo(0));
    }

    #endregion

    #region GetJsonArrayDifferences Tests

    [Test]
    public void GetJsonArrayDifferences_WithBasicArrays_ReturnsCorrectDifferences()
    {
        // Arrange
        var sourceJson = @"[
         { ""key"": ""item1"", ""value"": ""source1"", ""label"": ""Source Item 1"" },
      { ""key"": ""item2"", ""value"": ""source2"", ""label"": ""Source Item 2"" }
        ]";

        var targetJson = @"[
  { ""key"": ""item1"", ""value"": ""target1"", ""label"": ""Target Item 1"" },
            { ""key"": ""item3"", ""value"": ""target3"", ""label"": ""Target Item 3"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "label"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));

        // Should have the modified item1
        var item1 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item1");
        Assert.That(item1, Is.Not.Null);
        Assert.That(item1!["value"]?.ToString(), Is.EqualTo("target1"));

        // Should have the new item3
        var item3 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item3");
        Assert.That(item3, Is.Not.Null);

        // Should have the removed item2 marked as removed
        var item2 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item2");
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2!["label"]?.ToString(), Is.EqualTo("uSync:Removed in child site."));
    }

    [Test]
    public void GetJsonArrayDifferences_WithNullTargetArray_ReturnsEmptyArray()
    {
        // Arrange
        var sourceJson = @"[
      { ""key"": ""item1"", ""value"": ""source1"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, null, "key", "label"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonArrayDifferences_WithIdenticalArrays_ReturnsInheritedValues()
    {
        // Arrange
        var json = @"[
   { ""key"": ""item1"", ""value"": ""same"", ""label"": ""Same Item"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(json);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(json);
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "label"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        // when everything is identical nothing is returned because the root is right.
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonArrayDifferences_WithNestedArrayProperties_HandlesCorrectly()
    {
        // Arrange
        var sourceJson = @"[
        { 
                ""key"": ""item1"", 
       ""value"": ""source1"",
                ""nestedArray"": [
   { ""subkey"": ""sub1"", ""subvalue"": ""sourceSubValue1"" }
        ]
         }
        ]";

        var targetJson = @"[
   { 
      ""key"": ""item1"", 
      ""value"": ""target1"",
        ""nestedArray"": [
      { ""subkey"": ""sub1"", ""subvalue"": ""targetSubValue1"" }
  ]
            }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "label"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));

        var item = result[0];
        Assert.That(item!["key"]?.ToString(), Is.EqualTo("item1"));
        Assert.That(item["value"]?.ToString(), Is.EqualTo("target1"));

        // Should handle nested array differences
        var nestedArray = item["nestedArray"] as JsonArray;
        Assert.That(nestedArray, Is.Not.Null);
    }

    #endregion

    #region MergeJsonArrays Tests

    [Test]
    public void MergeJsonArrays_WithBothArrays_MergesCorrectly()
    {
        // Arrange
        var sourceJson = @"[
  { ""key"": ""item1"", ""value"": ""source1"", ""name"": ""Source Item 1"" },
       { ""key"": ""item2"", ""value"": ""source2"", ""name"": ""Source Item 2"" }
        ]";

        var targetJson = @"[
{ ""key"": ""item1"", ""value"": ""target1"", ""name"": ""Target Item 1"" },
            { ""key"": ""item3"", ""value"": ""target3"", ""name"": ""Target Item 3"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "name"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));

        // Should have merged item1 (target wins)
        var item1 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item1");
        Assert.That(item1, Is.Not.Null);
        Assert.That(item1!["value"]?.ToString(), Is.EqualTo("target1"));

        // Should have source item2
        var item2 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item2");
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2!["value"]?.ToString(), Is.EqualTo("source2"));

        // Should have target item3
        var item3 = result.FirstOrDefault(x => x?["key"]?.ToString() == "item3");
        Assert.That(item3, Is.Not.Null);
        Assert.That(item3!["value"]?.ToString(), Is.EqualTo("target3"));
    }

    [Test]
    public void MergeJsonArrays_WithNullSource_ReturnsTarget()
    {
        // Arrange
        var targetJson = @"[
      { ""key"": ""item1"", ""value"": ""target1"" }
 ]";

        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [null, targetArray, "key", "name"])!;

        // Assert
        Assert.That(result, Is.EqualTo(targetArray));
    }

    [Test]
    public void MergeJsonArrays_WithNullTarget_ReturnsClonedSource()
    {
        // Arrange
        var sourceJson = @"[
            { ""key"": ""item1"", ""value"": ""source1"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, null, "key", "name"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result, Is.Not.SameAs(sourceArray)); // Should be cloned
        Assert.That(result[0]!["key"]?.ToString(), Is.EqualTo("item1"));
    }

    [Test]
    public void MergeJsonArrays_WithRemovedItems_FiltersOutRemovedItems()
    {
        // Arrange
        var sourceJson = @"[
            { ""key"": ""item1"", ""value"": ""source1"" }
        ]";

        var targetJson = @"[
            { ""key"": ""item2"", ""value"": ""target2"", ""name"": ""uSync:Removed in child site.:Item2"" },
            { ""key"": ""item3"", ""value"": ""uSync:Inherited from root."", ""name"": ""Item3"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "name"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1)); // Only item1 should remain
        Assert.That(result[0]!["key"]?.ToString(), Is.EqualTo("item1"));
    }

    #endregion

    #region Public Method Tests via Reflection

    [Test]
    public void GetJsonPropertyDifferences_WithMissingPropertiesInTarget_AddsInheritedMarkers()
    {
        // Arrange
        var sourceJson = @"{
            ""key"": ""item1"",
            ""prop1"": ""sourceProp1"",
            ""prop2"": ""sourceProp2""
        }";

        var targetJson = @"{
            ""key"": ""item1"",
            ""prop1"": ""targetProp1""
        }";

        var sourceObject = JsonSerializer.Deserialize<JsonObject>(sourceJson)!;
        var targetObject = JsonSerializer.Deserialize<JsonObject>(targetJson)!;
        var method = GetPublicInstanceMethod("GetJsonPropertyDifferences");

        // Act
        var result = (JsonObject)method.Invoke(_mergerInstance, [sourceObject, targetObject, "key"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result["key"]?.ToString(), Is.EqualTo("item1"));
        Assert.That(result["prop1"]?.ToString(), Is.EqualTo("targetProp1")); // Target value wins
        Assert.That(result.ContainsKey("prop2"), Is.False); // Missing property inherits
    }

    [Test]
    public void GetJsonPropertyDifferences_WithIdenticalProperties_AddsInheritedMarkers()
    {
        // Arrange
        var json = @"{
        ""key"": ""item1"",
         ""prop1"": ""sameProp1"",
            ""prop2"": ""sameProp2""
        }";

        var sourceObject = JsonSerializer.Deserialize<JsonObject>(json)!;
        var targetObject = JsonSerializer.Deserialize<JsonObject>(json)!;
        var method = GetPublicInstanceMethod("GetJsonPropertyDifferences");

        // Act
        var result = (JsonObject)method.Invoke(_mergerInstance, [sourceObject, targetObject, "key"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result["key"]?.ToString(), Is.EqualTo("item1"));
        Assert.That(result.ContainsKey("prop1"), Is.False);
        Assert.That(result.ContainsKey("prop2"), Is.False);
    }

    [Test]
    public void MergeJsonProperties_WithInheritedValues_ReplacesWithSourceValues()
    {
        // Arrange
        var sourceJson = @"{
            ""key"": ""item1"",
            ""prop1"": ""sourceProp1"",
            ""prop2"": ""sourceProp2""
        }";

        var targetJson = @"{
            ""key"": ""item1"",
            ""prop2"": ""targetProp2"",
            ""prop3"": ""uSync:Inherited from root.""
        }";

        var sourceObject = JsonSerializer.Deserialize<JsonObject>(sourceJson)!;
        var targetObject = JsonSerializer.Deserialize<JsonObject>(targetJson)!;
        var method = GetPublicInstanceMethod("MergeJsonProperties");

        // Act
        var result = (JsonObject)method.Invoke(_mergerInstance, [sourceObject, targetObject, "key"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result["key"]?.ToString(), Is.EqualTo("item1"));
        Assert.That(result["prop1"]?.ToString(), Is.EqualTo("sourceProp1")); // Inherited value replaced with source
        Assert.That(result["prop2"]?.ToString(), Is.EqualTo("targetProp2")); // Target value preserved
        Assert.That(result.ContainsKey("prop3"), Is.False); // Inherited property without source should be removed
    }

    #endregion

    #region Helper Methods

    private static MethodInfo GetStaticMethod(string methodName)
    {
        var method = ImageCropperConfigMergerType.BaseType!.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        if (method == null)
        {
            throw new ArgumentException($"Method '{methodName}' not found in SyncConfigMergerBase");
        }

        return method;
    }

    private static MethodInfo GetGenericStaticMethod(string methodName, params Type[] genericTypes)
    {
        var methods = ImageCropperConfigMergerType.BaseType!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        .Where(m => m.Name == methodName && m.IsGenericMethodDefinition);

        var method = methods.FirstOrDefault();
        if (method == null)
        {
            throw new ArgumentException($"Generic method '{methodName}' not found in SyncConfigMergerBase");
        }

        // Create the generic method with specific types
        return method.MakeGenericMethod(genericTypes);
    }

    private MethodInfo GetInstanceMethod(string methodName)
    {
        var method = ImageCropperConfigMergerType.BaseType!.GetMethod(methodName,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (method == null)
        {
            throw new ArgumentException($"Instance method '{methodName}' not found in SyncConfigMergerBase");
        }

        return method;
    }

    private MethodInfo GetPublicInstanceMethod(string methodName)
    {
        var method = ImageCropperConfigMergerType.GetMethod(methodName,
      BindingFlags.Instance | BindingFlags.Public)
         ?? ImageCropperConfigMergerType.BaseType!.GetMethod(methodName,
    BindingFlags.Instance | BindingFlags.Public);

        if (method == null)
        {
            throw new ArgumentException($"Public instance method '{methodName}' not found in ImageCropperConfigMerger or its base");
        }

        return method;
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void MergeObjects_WithEmptyArrays_HandlesCorrectly()
    {
        // Arrange
        var method = GetGenericStaticMethod("MergeObjects", typeof(TestObject), typeof(string));
        var keySelector = new Func<TestObject, string>(x => x.Id);
        var predicate = new Predicate<TestObject>(x => x.IsRemoved);

        // Act
        var result = (TestObject[])method.Invoke(null, [Array.Empty<TestObject>(), Array.Empty<TestObject>(), keySelector, predicate])!;

        // Assert
        Assert.That(result, Has.Length.EqualTo(0));
    }

    [Test]
    public void GetJsonArrayDifferences_WithMalformedJson_HandlesGracefully()
    {
        // Arrange
        var sourceJson = @"[
 { ""key"": ""item1"", ""value"": ""source1"" },
            { ""wrongstructure"": ""invalid"" }
        ]";

        var targetJson = @"[
        { ""key"": ""item1"", ""value"": ""target1"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act & Assert (should not throw)
        Assert.DoesNotThrow(() =>
 {
     var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "label"])!;
     Assert.That(result, Is.Not.Null);
 });
    }

    [Test]
    public void MergeJsonArrays_WithComplexNestedStructures_HandlesCorrectly()
    {
        // Arrange
        var sourceJson = @"[
            { 
                ""key"": ""item1"", 
                ""value"": ""source1"",
                ""nested"": {
                    ""prop1"": ""sourceProp1"",
                ""prop2"": ""sourceProp2""
                }
            }
        ]";

        var targetJson = @"[
            { 
                ""key"": ""item1"", 
                ""value"": ""target1"",
                ""nested"": {
                    ""prop1"": ""targetProp1"",
                    ""prop3"": ""targetProp3""
                }
            }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "name"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));

        var item = result[0];
        Assert.That(item!["key"]?.ToString(), Is.EqualTo("item1"));
        Assert.That(item["value"]?.ToString(), Is.EqualTo("target1"));

        // Nested object should be handled appropriately
        var nested = item["nested"];
        Assert.That(nested, Is.Not.Null);
    }

    [Test]
    public void GetJsonArrayDifferences_WithEmptyArrays_ReturnsEmptyResult()
    {
        // Arrange
        var sourceArray = JsonSerializer.Deserialize<JsonArray>("[]");
        var targetArray = JsonSerializer.Deserialize<JsonArray>("[]");
        var method = GetInstanceMethod("GetJsonArrayDifferences");

        // Act
        var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "label"])!;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void MergeJsonArrays_WithNonObjectElements_HandlesGracefully()
    {
        // Arrange - Array containing non-object elements
        var sourceJson = @"[
            ""stringValue"",
            { ""key"": ""item1"", ""value"": ""source1"" },
            123
        ]";

        var targetJson = @"[
            { ""key"": ""item1"", ""value"": ""target1"" }
        ]";

        var sourceArray = JsonSerializer.Deserialize<JsonArray>(sourceJson);
        var targetArray = JsonSerializer.Deserialize<JsonArray>(targetJson);
        var method = GetInstanceMethod("MergeJsonArrays");

        // Act & Assert (should not throw)
        Assert.DoesNotThrow(() =>
        {
            var result = (JsonArray)method.Invoke(_mergerInstance, [sourceArray, targetArray, "key", "name"])!;
            Assert.That(result, Is.Not.Null);
        });
    }

    #endregion
}
