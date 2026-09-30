using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Serialization;

using uSync.Core.Cache;
using uSync.Core.Mapping;
using uSync.Core.Mapping.Mappers;
using uSync.Core.Mapping.Tracking;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Mappers;

/// <summary>
///  an invariant block list containing culture variant elements is re-serialized by
///  Umbraco when it is published (per culture merge), and the edited flag is a string
///  compare of the draft and published values. so the draft uSync writes has to be
///  exactly what Umbraco's merge produces, or the document shows pending changes (#1097).
/// </summary>
[TestFixture]
public class BlockListMapperPublishTests
{
    private static readonly string[] _cultures = ["en-US", "da-DK"];

    private IJsonSerializer _jsonSerializer;
    private IContentType _elementType;
    private IPropertyType _textPropertyType;
    private SyncValueMapperCollection _mappers;
    private IPropertyType _blockListPropertyType;
    private BlockListPropertyEditor _blockListEditor;

    private static IShortStringHelper ShortStringHelper
        => new DefaultShortStringHelper(new DefaultShortStringHelperConfig());

    [SetUp]
    public void Setup()
    {
        _jsonSerializer = new SystemTextJsonSerializer(new DefaultJsonSerializerEncoderFactory());

        _elementType = BuildElementType();
        _textPropertyType = _elementType.PropertyTypes.First(x => x.Alias == "alt");

        _mappers = BuildMapperCollection(_elementType);
        _blockListPropertyType = new PropertyType(ShortStringHelper, UmbConstants.PropertyEditors.Aliases.BlockList, ValueStorageType.Ntext)
        {
            Alias = "images",
            Variations = ContentVariation.Nothing
        };
        _blockListEditor = BuildBlockListEditor(_elementType, _jsonSerializer);
    }

    [Test]
    public async Task ImportedDraft_MatchesPublishedValue()
    {
        var source = BuildSourceValue();

        var draft = await ImportAsync(source);
        var published = PublishAllCultures(draft);

        Assert.That(draft, Is.EqualTo(published));
    }

    [Test]
    public async Task ImportedDraft_HasEditorAlias()
    {
        var source = BuildSourceValue();

        var draft = await ImportAsync(source);

        Assert.That(draft, Does.Contain("\"editorAlias\":\"Umbraco.TextBox\""));
        Assert.That(draft, Does.Not.Contain("\"editorAlias\":null"));
    }

    [Test]
    public async Task ImportedDraft_SortsValuesByCulture()
    {
        // values out of culture order (as an older export might have them)
        var source = BuildSourceValue(reverseCultures: true);

        var draft = await ImportAsync(source);
        var published = PublishAllCultures(draft);

        Assert.That(draft, Is.EqualTo(published));
    }

    private async Task<string> ImportAsync(string value)
        => (await _mappers.GetImportValueAsync(value, _blockListPropertyType, new SyncSerializerOptions()))?.ToString();

    /// <summary>
    ///  mimics what ContentRepositoryExtensions.PublishCulture does for an invariant
    ///  block property on a culture variant document, when all cultures are published.
    /// </summary>
    private string PublishAllCultures(string draft)
    {
        object published = null;
        foreach (var culture in _cultures.Append(null))
            published = _blockListEditor.MergePartialPropertyValueForCulture(draft, published, culture);

        return published?.ToString();
    }

    /// <summary>
    ///  a block list value as Umbraco stores it (and so as uSync exports it).
    /// </summary>
    private string BuildSourceValue(bool reverseCultures = false)
    {
        var blockKey = Guid.NewGuid();
        var urlPropertyType = _elementType.PropertyTypes.First(x => x.Alias == "url");

        var cultures = reverseCultures ? _cultures.Reverse() : _cultures.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        List<BlockPropertyValue> values = [
            new BlockPropertyValue { Alias = "url", Value = "https://example.com", PropertyType = urlPropertyType },
            .. cultures.Select(c => new BlockPropertyValue { Alias = "alt", Culture = c, Value = $"alt text {c}", PropertyType = _textPropertyType })
        ];

        var blockValue = new BlockListValue([new BlockListLayoutItem(blockKey)])
        {
            ContentData = [new BlockItemData(blockKey, _elementType.Key, _elementType.Alias) { Values = values }],
            Expose = [.. _cultures.Select(c => new BlockItemVariation(blockKey, c, null))]
        };

        return _jsonSerializer.Serialize(blockValue);
    }

    private static IContentType BuildElementType()
    {
        var elementType = new ContentType(ShortStringHelper, -1)
        {
            Alias = "imageBlock",
            Name = "Image Block",
            Key = Guid.NewGuid(),
            IsElement = true,
            Variations = ContentVariation.Culture
        };

        elementType.AddPropertyType(new PropertyType(ShortStringHelper, UmbConstants.PropertyEditors.Aliases.TextBox, ValueStorageType.Nvarchar)
        {
            Alias = "alt",
            Name = "Alt",
            Variations = ContentVariation.Culture
        });

        elementType.AddPropertyType(new PropertyType(ShortStringHelper, UmbConstants.PropertyEditors.Aliases.TextBox, ValueStorageType.Nvarchar)
        {
            Alias = "url",
            Name = "Url",
            Variations = ContentVariation.Nothing
        });

        return elementType;
    }

    private static SyncValueMapperCollection BuildMapperCollection(IContentType elementType)
    {
        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(x => x.GetAsync(elementType.Key)).ReturnsAsync(elementType);

        var entityService = Mock.Of<IEntityService>();

        BlockListMapper mapper = null;
        var mapperCollection = new Lazy<SyncValueMapperCollection>(() => new SyncValueMapperCollection(
            new SyncEntityCache(entityService, contentTypeService.Object),
            () => [mapper],
            new SyncMapperTrackerCollection(() => [])));

        mapper = new BlockListMapper(entityService, contentTypeService.Object, mapperCollection, NullLogger<BlockListMapper>.Instance);
        return mapperCollection.Value;
    }

    private static BlockListPropertyEditor BuildBlockListEditor(IContentType elementType, IJsonSerializer jsonSerializer)
    {
        var elementTypeCache = new Mock<IBlockEditorElementTypeCache>();
        elementTypeCache.Setup(x => x.GetMany(It.IsAny<IEnumerable<Guid>>())).Returns([elementType]);

        var propertyEditors = new PropertyEditorCollection(new DataEditorCollection(Enumerable.Empty<IDataEditor>));
        var dataValueReferenceFactories = new DataValueReferenceFactoryCollection(Enumerable.Empty<IDataValueReferenceFactory>, NullLogger<DataValueReferenceFactoryCollection>.Instance);
        var blockVarianceHandler = new BlockEditorVarianceHandler(Mock.Of<ILanguageService>(), Mock.Of<IContentTypeService>());

        // the block list value editor is internal to Umbraco, so we have to create it via reflection.
        var valueEditorType = typeof(BlockListPropertyEditorBase)
            .GetNestedType("BlockListEditorPropertyValueEditor", BindingFlags.NonPublic)!;

        var logger = typeof(NullLogger<>).MakeGenericType(valueEditorType)
            .GetField(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null);

        var dataValueEditorFactory = new ReflectionDataValueEditorFactory(() => Activator.CreateInstance(valueEditorType,
            new DataEditorAttribute(UmbConstants.PropertyEditors.Aliases.BlockList),
            new BlockListEditorDataConverter(jsonSerializer),
            propertyEditors,
            dataValueReferenceFactories,
            Mock.Of<IDataTypeConfigurationCache>(),
            elementTypeCache.Object,
            Mock.Of<ILocalizedTextService>(),
            logger,
            ShortStringHelper,
            jsonSerializer,
            Mock.Of<IPropertyValidationService>(),
            blockVarianceHandler,
            Mock.Of<ILanguageService>(),
            Mock.Of<IIOHelper>()));

        return new BlockListPropertyEditor(
            dataValueEditorFactory,
            Mock.Of<IIOHelper>(),
            Mock.Of<IBlockValuePropertyIndexValueFactory>(),
            jsonSerializer);
    }

    private sealed class ReflectionDataValueEditorFactory(Func<object> create) : IDataValueEditorFactory
    {
        public TDataValueEditor Create<TDataValueEditor>(params object[] args)
            where TDataValueEditor : class, IDataValueEditor
            => (TDataValueEditor)create();
    }
}
