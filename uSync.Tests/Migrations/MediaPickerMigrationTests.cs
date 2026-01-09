using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class MediaPickerMigrationTests : MigrationTestBase
{
    private MediaPickerConfigSerializer _serializer;
    private Mock<IMediaTypeService> _mockMediaTypeService;

    [SetUp]
    public void Setup()
    {
        _mockMediaTypeService = new Mock<IMediaTypeService>();
        
        // Mock media types with their aliases and keys
        var imageMediaType = new Mock<IMediaType>();
        imageMediaType.Setup(x => x.Key).Returns(Guid.Parse("cc07b313-0843-4aa8-bbda-871c8da728c8"));
        imageMediaType.Setup(x => x.Alias).Returns("Image");
        
        var videoMediaType = new Mock<IMediaType>();
        videoMediaType.Setup(x => x.Key).Returns(Guid.Parse("f6c515bb-653c-4bdc-821c-987729ebe327"));
        videoMediaType.Setup(x => x.Alias).Returns("Video");
        
        var fileMediaType = new Mock<IMediaType>();
        fileMediaType.Setup(x => x.Key).Returns(Guid.Parse("4c52d8ab-54e6-40cd-999c-7a5f24903e4d"));
        fileMediaType.Setup(x => x.Alias).Returns("File");
        
        _mockMediaTypeService.Setup(x => x.Get("Image")).Returns(imageMediaType.Object);
        _mockMediaTypeService.Setup(x => x.Get("Video")).Returns(videoMediaType.Object);
        _mockMediaTypeService.Setup(x => x.Get("File")).Returns(fileMediaType.Object);
        
        _serializer = new MediaPickerConfigSerializer(_mockMediaTypeService.Object);
    }

    // Test migrating filter from aliases to GUIDs
    private static string FilterAliasSource = @"{
  ""filter"": ""Image,Video"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    private static string FilterAliasTarget = @"{
  ""filter"": ""cc07b313-0843-4aa8-bbda-871c8da728c8,f6c515bb-653c-4bdc-821c-987729ebe327"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void FilterAliasMigrationTest()
        => TestSerializerPropertyMigration(_serializer, FilterAliasSource, FilterAliasTarget);

    // Test that already migrated filter (GUIDs) remain unchanged
    private static string FilterGuidSource = @"{
  ""filter"": ""cc07b313-0843-4aa8-bbda-871c8da728c8,f6c515bb-653c-4bdc-821c-987729ebe327"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void FilterAlreadyMigratedTest()
        => TestSerializerPropertyMigration(_serializer, FilterGuidSource, FilterGuidSource);

    // Test migrating startNodeId from UDI to GUID
    private static string StartNodeUdiSource = @"{
  ""startNodeId"": ""umb://media/71332aa78bea44f19aa600de961b66e8"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    private static string StartNodeUdiTarget = @"{
  ""startNodeId"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void StartNodeUdiMigrationTest()
        => TestSerializerPropertyMigration(_serializer, StartNodeUdiSource, StartNodeUdiTarget);

    // Test that already migrated startNodeId (GUID) remains unchanged
    private static string StartNodeGuidSource = @"{
  ""startNodeId"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void StartNodeAlreadyMigratedTest()
        => TestSerializerPropertyMigration(_serializer, StartNodeGuidSource, StartNodeGuidSource);

    // Test migrating both filter and startNodeId together
    private static string BothSource = @"{
  ""filter"": ""Image,File"",
  ""startNodeId"": ""umb://media/71332aa78bea44f19aa600de961b66e8"",
  ""multiple"": true,
  ""validationLimit"": {}
}";

    private static string BothTarget = @"{
  ""filter"": ""cc07b313-0843-4aa8-bbda-871c8da728c8,4c52d8ab-54e6-40cd-999c-7a5f24903e4d"",
  ""startNodeId"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
  ""multiple"": true,
  ""validationLimit"": {}
}";

    [Test]
    public void BothFilterAndStartNodeMigrationTest()
        => TestSerializerPropertyMigration(_serializer, BothSource, BothTarget);

    // Test with null/empty filter
    private static string NullFilterSource = @"{
  ""filter"": null,
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void NullFilterTest()
        => TestSerializerPropertyMigration(_serializer, NullFilterSource, NullFilterSource);

    // Test with unknown alias (should preserve the alias)
    private static string UnknownAliasSource = @"{
  ""filter"": ""UnknownMediaType"",
  ""multiple"": false,
  ""validationLimit"": {}
}";

    [Test]
    public void UnknownAliasPreservedTest()
        => TestSerializerPropertyMigration(_serializer, UnknownAliasSource, UnknownAliasSource);
}
