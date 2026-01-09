using System;

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
    private Mock<IMediaTypeService> _mediaTypeServiceMock;

    [SetUp]
    public void Setup()
    {
        _mediaTypeServiceMock = new Mock<IMediaTypeService>();
        _serializer = new MediaPickerConfigSerializer(_mediaTypeServiceMock.Object);
    }

    [Test]
    public void FilterMigrationFromAliasToGuid()
    {
        // Arrange
        var mediaTypeGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var mediaTypeMock = new Mock<IMediaType>();
        mediaTypeMock.Setup(x => x.Key).Returns(mediaTypeGuid);

        _mediaTypeServiceMock
            .Setup(x => x.Get("myMediaType"))
            .Returns(mediaTypeMock.Object);

        var source = @"{
  ""filter"": ""myMediaType"",
  ""ignoreUserStartNodes"": false
}";

        var target = @"{
  ""filter"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
  ""ignoreUserStartNodes"": false
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void FilterMigrationAlreadyGuid()
    {
        // Arrange - filter is already a GUID, should not change
        var source = @"{
  ""filter"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
  ""ignoreUserStartNodes"": false
}";

        var target = @"{
  ""filter"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
  ""ignoreUserStartNodes"": false
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void FilterMigrationMultipleAliasesToGuids()
    {
        // Arrange
        var mediaType1Guid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mediaType2Guid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var mediaType3Guid = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var mediaType1Mock = new Mock<IMediaType>();
        mediaType1Mock.Setup(x => x.Key).Returns(mediaType1Guid);

        var mediaType2Mock = new Mock<IMediaType>();
        mediaType2Mock.Setup(x => x.Key).Returns(mediaType2Guid);

        var mediaType3Mock = new Mock<IMediaType>();
        mediaType3Mock.Setup(x => x.Key).Returns(mediaType3Guid);

        _mediaTypeServiceMock
            .Setup(x => x.Get("mediaTypeOne"))
            .Returns(mediaType1Mock.Object);

        _mediaTypeServiceMock
            .Setup(x => x.Get("mediaTypeTwo"))
            .Returns(mediaType2Mock.Object);

        _mediaTypeServiceMock
            .Setup(x => x.Get("mediaTypeThree"))
            .Returns(mediaType3Mock.Object);

        var source = @"{
  ""filter"": ""mediaTypeOne,mediaTypeTwo,mediaTypeThree"",
  ""ignoreUserStartNodes"": false
}";

        var target = @"{
  ""filter"": ""11111111-1111-1111-1111-111111111111,22222222-2222-2222-2222-222222222222,33333333-3333-3333-3333-333333333333"",
  ""ignoreUserStartNodes"": false
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void FilterMigrationMixedAliasesAndGuids()
    {
        // Arrange - mix of aliases and GUIDs
        var mediaType1Guid = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var mediaType1Mock = new Mock<IMediaType>();
        mediaType1Mock.Setup(x => x.Key).Returns(mediaType1Guid);

        _mediaTypeServiceMock
            .Setup(x => x.Get("mediaTypeOne"))
            .Returns(mediaType1Mock.Object);

        var source = @"{
  ""filter"": ""mediaTypeOne,22222222-2222-2222-2222-222222222222"",
  ""ignoreUserStartNodes"": false
}";

        var target = @"{
  ""filter"": ""11111111-1111-1111-1111-111111111111,22222222-2222-2222-2222-222222222222"",
  ""ignoreUserStartNodes"": false
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void StartNodeIdMigrationFromUdiToGuid()
    {
        // Arrange
        var source = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""umb://media/a1b2c3d4e5f67890abcdef1234567890""
}";

        var target = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890""
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void StartNodeIdMigrationAlreadyGuid()
    {
        // Arrange - startNodeId is already a GUID, should not change
        var source = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890""
}";

        var target = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890""
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void StartNodeIdMigrationEmpty()
    {
        // Arrange - empty startNodeId should remain empty
        var source = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": """"
}";

        var target = @"{
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": """"
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }

    [Test]
    public void FilterAndStartNodeIdMigrationTogether()
    {
        // Arrange - test both filter and startNodeId migration in the same config
        var mediaTypeGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var mediaTypeMock = new Mock<IMediaType>();
        mediaTypeMock.Setup(x => x.Key).Returns(mediaTypeGuid);

        _mediaTypeServiceMock
            .Setup(x => x.Get("myMediaType"))
            .Returns(mediaTypeMock.Object);

        var source = @"{
  ""filter"": ""myMediaType"",
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""umb://media/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb""
}";

        var target = @"{
  ""filter"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb""
}";

        // Act & Assert
        TestSerializerPropertyMigration(_serializer, source, target);
    }
}
