using System;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class ContentPickerMigrationTests : MigrationTestBase
{
    private ContentPickerConfigSerializer _serializer;
    private Mock<IContentTypeService> _contentTypeServiceMock;

    [SetUp]
    public void Setup()
    {
        _contentTypeServiceMock = new Mock<IContentTypeService>();
        _serializer = new ContentPickerConfigSerializer(_contentTypeServiceMock.Object);
    }

    [Test]
    public void FilterMigrationFromAliasToGuid()
    {
        // Arrange
        var contentTypeGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var contentTypeMock = new Mock<IContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeGuid);

        _contentTypeServiceMock
            .Setup(x => x.Get("myContentType"))
            .Returns(contentTypeMock.Object);

        var source = @"{
  ""filter"": ""myContentType"",
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
        var contentType1Guid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var contentType2Guid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var contentType3Guid = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var contentType1Mock = new Mock<IContentType>();
        contentType1Mock.Setup(x => x.Key).Returns(contentType1Guid);

        var contentType2Mock = new Mock<IContentType>();
        contentType2Mock.Setup(x => x.Key).Returns(contentType2Guid);

        var contentType3Mock = new Mock<IContentType>();
        contentType3Mock.Setup(x => x.Key).Returns(contentType3Guid);

        _contentTypeServiceMock
            .Setup(x => x.Get("contentTypeOne"))
            .Returns(contentType1Mock.Object);

        _contentTypeServiceMock
            .Setup(x => x.Get("contentTypeTwo"))
            .Returns(contentType2Mock.Object);

        _contentTypeServiceMock
            .Setup(x => x.Get("contentTypeThree"))
            .Returns(contentType3Mock.Object);

        var source = @"{
  ""filter"": ""contentTypeOne,contentTypeTwo,contentTypeThree"",
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
        var contentType1Guid = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var contentType1Mock = new Mock<IContentType>();
        contentType1Mock.Setup(x => x.Key).Returns(contentType1Guid);

        _contentTypeServiceMock
            .Setup(x => x.Get("contentTypeOne"))
            .Returns(contentType1Mock.Object);

        var source = @"{
  ""filter"": ""contentTypeOne,22222222-2222-2222-2222-222222222222"",
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
  ""startNodeId"": ""umb://document/a1b2c3d4e5f67890abcdef1234567890""
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
        var contentTypeGuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var contentTypeMock = new Mock<IContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeGuid);

        _contentTypeServiceMock
            .Setup(x => x.Get("myContentType"))
            .Returns(contentTypeMock.Object);

        var source = @"{
  ""filter"": ""myContentType"",
  ""ignoreUserStartNodes"": false,
  ""startNodeId"": ""umb://document/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb""
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
