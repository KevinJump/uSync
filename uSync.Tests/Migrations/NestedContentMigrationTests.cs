using System;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class NestedContentMigrationTests : MigrationTestBase
{
    private NestedContentConfigurationMigrator _serializer;
    private Mock<IContentTypeService> _mockContentTypeService;

    [SetUp]
    public void Setup()
    {
        _mockContentTypeService = new Mock<IContentTypeService>();

        var articleContentType = new Mock<IContentType>();
        articleContentType.Setup(x => x.Key).Returns(Guid.Parse("cc07b313-0843-4aa8-bbda-871c8da728c8"));

        var blogContentType = new Mock<IContentType>();
        blogContentType.Setup(x => x.Key).Returns(Guid.Parse("4c52d8ab-54e6-40cd-999c-7a5f24903e4d"));

        _mockContentTypeService.Setup(x => x.Get("articleType")).Returns(articleContentType.Object);
        _mockContentTypeService.Setup(x => x.Get("blogType")).Returns(blogContentType.Object);

        _serializer = new NestedContentConfigurationMigrator(_mockContentTypeService.Object);
    }

    private static readonly string FullMigrationSource = @"{
  ""minItems"": 1,
  ""maxItems"": 5,
  ""contentTypes"": [
    { ""ncAlias"": ""articleType"" },
    { ""ncAlias"": ""blogType"" }
  ]
}";

    private static readonly string FullMigrationTarget = @"{
  ""blocks"": [
    {
      ""contentElementTypeKey"": ""cc07b313-0843-4aa8-bbda-871c8da728c8"",
      ""settingsElementTypeKey"": null
    },
    {
      ""contentElementTypeKey"": ""4c52d8ab-54e6-40cd-999c-7a5f24903e4d"",
      ""settingsElementTypeKey"": null
    }
  ],
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": 5,
    ""min"": 1
  }
}";

    [Test]
    public void FullMigrationTest()
        => TestSerializerPropertyMigration(_serializer, FullMigrationSource, FullMigrationTarget);

    private static readonly string MinOnlySource = @"{
  ""minItems"": 2,
  ""contentTypes"": [
    { ""ncAlias"": ""articleType"" }
  ]
}";

    private static readonly string MinOnlyTarget = @"{
  ""blocks"": [
    {
      ""contentElementTypeKey"": ""cc07b313-0843-4aa8-bbda-871c8da728c8"",
      ""settingsElementTypeKey"": null
    }
  ],
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": null,
    ""min"": 2
  }
}";

    [Test]
    public void MinOnlyMigrationTest()
        => TestSerializerPropertyMigration(_serializer, MinOnlySource, MinOnlyTarget);

    private static readonly string MaxOnlySource = @"{
  ""maxItems"": 3,
  ""contentTypes"": [
    { ""ncAlias"": ""blogType"" }
  ]
}";

    private static readonly string MaxOnlyTarget = @"{
  ""blocks"": [
    {
      ""contentElementTypeKey"": ""4c52d8ab-54e6-40cd-999c-7a5f24903e4d"",
      ""settingsElementTypeKey"": null
    }
  ],
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": 3,
    ""min"": null
  }
}";

    [Test]
    public void MaxOnlyMigrationTest()
        => TestSerializerPropertyMigration(_serializer, MaxOnlySource, MaxOnlyTarget);

    private static readonly string NoValidationLimitsSource = @"{
  ""contentTypes"": [
    { ""ncAlias"": ""articleType"" }
  ]
}";

    private static readonly string NoValidationLimitsTarget = @"{
  ""blocks"": [
    {
      ""contentElementTypeKey"": ""cc07b313-0843-4aa8-bbda-871c8da728c8"",
      ""settingsElementTypeKey"": null
    }
  ],
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": null,
    ""min"": null
  }
}";

    [Test]
    public void NoValidationLimitsMigrationTest()
        => TestSerializerPropertyMigration(_serializer, NoValidationLimitsSource, NoValidationLimitsTarget);

    private static readonly string UnknownAliasSource = @"{
  ""contentTypes"": [
    { ""ncAlias"": ""unknownType"" }
  ]
}";

    private static readonly string UnknownAliasTarget = @"{
  ""blocks"": null,
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": null,
    ""min"": null
  }
}";

    [Test]
    public void UnknownAliasIsSkippedTest()
        => TestSerializerPropertyMigration(_serializer, UnknownAliasSource, UnknownAliasTarget);

    private static readonly string MixedAliasesSource = @"{
  ""contentTypes"": [
    { ""ncAlias"": ""articleType"" },
    { ""ncAlias"": ""unknownType"" }
  ]
}";

    private static readonly string MixedAliasesTarget = @"{
  ""blocks"": [
    {
      ""contentElementTypeKey"": ""cc07b313-0843-4aa8-bbda-871c8da728c8"",
      ""settingsElementTypeKey"": null
    }
  ],
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": null,
    ""min"": null
  }
}";

    [Test]
    public void UnknownAliasInMixedListIsSkippedTest()
        => TestSerializerPropertyMigration(_serializer, MixedAliasesSource, MixedAliasesTarget);

    private static readonly string EmptyContentTypesSource = @"{
  ""minItems"": 1,
  ""maxItems"": 2,
  ""contentTypes"": []
}";

    private static readonly string EmptyContentTypesTarget = @"{
  ""blocks"": null,
  ""useSingleBlockMode"": false,
  ""validationLimit"": {
    ""max"": 2,
    ""min"": 1
  }
}";

    [Test]
    public void EmptyContentTypesMigrationTest()
        => TestSerializerPropertyMigration(_serializer, EmptyContentTypesSource, EmptyContentTypesTarget);
}
