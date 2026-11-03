using System;
using System.Collections.Generic;

using Moq;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Cache;
using uSync.Core.Mapping;

namespace uSync.Tests.Mapping
{
    /// <summary>
    ///  Repro for uSync.Complete.Issues #314: a JSON boolean inside a block
    ///  (e.g. a TrueFalse value written by code, not the backoffice)
    ///  comes out of the mapper as the string "False".
    /// </summary>
    [TestFixture]
    public class BlockListBooleanTests
    {
        private static readonly Guid _elementKey = Guid.Parse("8d3a5b3c-2b1e-4f6a-9c1d-0e6b9b1f1a01");

        private BlockListMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var propertyType = new Mock<IPropertyType>();
            propertyType.SetupGet(x => x.Alias).Returns("backgroundTint");
            propertyType.SetupGet(x => x.PropertyEditorAlias).Returns(Constants.PropertyEditors.Aliases.Boolean);

            var elementType = new Mock<IContentType>();
            elementType.SetupGet(x => x.Key).Returns(_elementKey);
            elementType.SetupGet(x => x.Alias).Returns("element");
            elementType.SetupGet(x => x.CompositionPropertyTypes).Returns(new List<IPropertyType> { propertyType.Object });

            var contentTypeService = new Mock<IContentTypeService>();
            contentTypeService.Setup(x => x.Get(_elementKey)).Returns(elementType.Object);

            var entityService = new Mock<IEntityService>();
            var dataTypeService = new Mock<IDataTypeService>();

            SyncValueMapperCollection collection = null;
            var lazyCollection = new Lazy<SyncValueMapperCollection>(() => collection);

            _mapper = new BlockListMapper(entityService.Object, lazyCollection, contentTypeService.Object, dataTypeService.Object);

            collection = new SyncValueMapperCollection(
                new SyncEntityCache(entityService.Object, contentTypeService.Object),
                () => new ISyncMapper[] { _mapper });
        }

        private static string BlockValue(string tintJson) => $$"""
            {
              "layout": { "Umbraco.BlockList": [ { "contentUdi": "umb://element/0b9e5b7e4a3f4b8c9d2a1c3e5f7a9b11" } ] },
              "contentData": [
                {
                  "contentTypeKey": "{{_elementKey}}",
                  "udi": "umb://element/0b9e5b7e4a3f4b8c9d2a1c3e5f7a9b11",
                  "backgroundTint": {{tintJson}}
                }
              ],
              "settingsData": []
            }
            """;

        private static JToken GetTint(string blockJson)
            => JObject.Parse(blockJson)["contentData"][0]["backgroundTint"];

        [TestCase("false")]
        [TestCase("true")]
        public void Export_JsonBoolean_IsNotTurnedIntoDotNetString(string tintJson)
        {
            var exported = _mapper.GetExportValue(BlockValue(tintJson), Constants.PropertyEditors.Aliases.BlockList);
            var tint = GetTint(exported);

            TestContext.WriteLine($"exported backgroundTint: {tint.ToString(Newtonsoft.Json.Formatting.None)} ({tint.Type})");
            Assert.That(tint.Type, Is.EqualTo(JTokenType.Boolean));
        }

        [TestCase("false")]
        [TestCase("true")]
        public void Import_JsonBoolean_IsNotTurnedIntoDotNetString(string tintJson)
        {
            var imported = _mapper.GetImportValue(BlockValue(tintJson), Constants.PropertyEditors.Aliases.BlockList);
            var tint = GetTint(imported);

            TestContext.WriteLine($"imported backgroundTint: {tint.ToString(Newtonsoft.Json.Formatting.None)} ({tint.Type})");
            Assert.That(tint.Type, Is.EqualTo(JTokenType.Boolean));
        }

        [TestCase("\"0\"")]
        [TestCase("\"1\"")]
        public void RoundTrip_BackofficeStringValue_IsUnchanged(string tintJson)
        {
            var exported = _mapper.GetExportValue(BlockValue(tintJson), Constants.PropertyEditors.Aliases.BlockList);
            var imported = _mapper.GetImportValue(exported, Constants.PropertyEditors.Aliases.BlockList);

            Assert.That(GetTint(imported).ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo(tintJson));
        }
    }
}
