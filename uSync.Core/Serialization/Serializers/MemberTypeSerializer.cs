using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("F45B5C7B-C206-4971-858B-6D349E153ACE", "MemberTypeSerializer", uSyncConstants.Serialization.MemberType)]
public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>, ISyncSerializer<IMemberType>
{
    private readonly IMemberTypeService _memberTypeService;

    public MemberTypeSerializer(
        IEntityService entityService, ILogger<MemberTypeSerializer> logger,
        IDataTypeService dataTypeService,
        IMemberTypeService memberTypeService,
        IShortStringHelper shortStringHelper,
        AppCaches appCaches,
        IContentTypeService contentTypeService)
        : base(entityService, logger, dataTypeService, memberTypeService, UmbracoObjectTypes.Unknown, shortStringHelper, appCaches, contentTypeService)
    {
        this._memberTypeService = memberTypeService;
    }

	protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IMemberType item, SyncSerializerOptions options)
	{
        var node = SerializeBase(item);
        var info = SerializeInfo(item);

        var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
        if (parent != null)
        {
            info.Add(new XElement(uSyncConstants.Xml.Parent, parent.Alias,
                new XAttribute(uSyncConstants.Xml.Key, parent.Key)));
        }
        else if (item.Level != 1)
        {
            // in a folder
            var folderNode = await GetFolderNodeAsync(item); //TODO: Cache this call.
            if (folderNode != null)
                info.Add(folderNode);
        }

        info.Add(SerializeCompositions((ContentTypeCompositionBase)item));

        node.Add(info);
        node.Add(SerializeProperties(item));
        node.Add(SerializeStructureAsync(item));
        node.Add(SerializeTabs(item));

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Alias, node, typeof(IMediaType), ChangeType.Export);

    }

    protected override void SerializeExtraProperties(XElement node, IMemberType item, IPropertyType property)
    {
        node.Add(new XElement("CanEdit", item.MemberCanEditProperty(property.Alias)));
        node.Add(new XElement("CanView", item.MemberCanViewProperty(property.Alias)));
        node.Add(new XElement("IsSensitive", item.IsSensitiveProperty(property.Alias)));
    }

    //
    // for the member type, the built in properties are created with GUIDs that are really int values
    // as a result the Key value you get back for them, can change between reboots. 
    //
    // here we tag on to the SerializeProperties step, and blank the Key value for any of the built in 
    // properties. 
    //
    //   this means we don't get false positives between reboots, 
    //   it also means that these properties won't get deleted if/when they are removed - but 
    //   we limit it only to these items by listing them (so custom items in a member type will still
    //   get removed when required. 
    // 

    private static readonly Dictionary<string, string> _builtInProperties = new()
    {
        {  "umbracoMemberApproved", "e79dccfb-0000-0000-0000-000000000000" },
        {  "umbracoMemberComments", "2a280588-0000-0000-0000-000000000000" },
        {  "umbracoMemberFailedPasswordAttempts", "0f2ea539-0000-0000-0000-000000000000" },
        {  "umbracoMemberLastLockoutDate", "3a7bc3c6-0000-0000-0000-000000000000" },
        {  "umbracoMemberLastLogin", "b5e309ba-0000-0000-0000-000000000000" },
        {  "umbracoMemberLastPasswordChangeDate", "ded56d3f-0000-0000-0000-000000000000" },
        {  "umbracoMemberLockedOut", "c36093d2-0000-0000-0000-000000000000" },
        {  "umbracoMemberPasswordRetrievalAnswer", "9700bd39-0000-0000-0000-000000000000" },
        {  "umbracoMemberPasswordRetrievalQuestion", "e2d9286a-0000-0000-0000-000000000000" },
    };

    protected override XElement SerializeProperties(IMemberType item)
    {
        var node = base.SerializeProperties(item);
        foreach (var property in node.Elements("GenericProperty"))
        {
            var alias = property.Element("Alias").ValueOrDefault(string.Empty);
            if (!string.IsNullOrWhiteSpace(alias) && _builtInProperties.TryGetValue(alias, out string? value))
            {
                var key = value;
                if (!item.Alias.InvariantEquals("Member"))
                {
                    key = $"{item.Alias}{alias}".GetDeterministicHashCode().ToGuid().ToString();
                }

                var keyElement = property.Element(uSyncConstants.Xml.Key);
                if (keyElement is not null)
                    keyElement.Value = key;
            }
        }
        return node;
    }

	protected override async Task<SyncAttempt<IMemberType>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        details.AddRange(DeserializeBase(item, node));
        details.AddRange(DeserializeTabs(item, node));
        details.AddRange(DeserializeProperties(item, node, options));

        CleanTabs(item, node, options);

        // memberTypeService.Save(item);

        return DeserializedResult(item, details, options);
    }

	public override Task<SyncAttempt<IMemberType>> DeserializeSecondPassAsync(IMemberType item, XElement node, SyncSerializerOptions options)
	{
        CleanTabAliases(item);
        var details = CleanTabs(item, node, options).ToList();

        SetSafeAliasValue(item, node, false);

        bool saveInSerializer = !options.Flags.HasFlag(SerializerFlags.DoNotSave);
        if (saveInSerializer && item.IsDirty())
            _memberTypeService.Save(item);

        return Task.FromResult(SyncAttempt<IMemberType>.Succeed(item.Name ?? item.Alias, item, ChangeType.Import, string.Empty, saveInSerializer, details));
    }

    protected override IEnumerable<uSyncChange> DeserializeExtraProperties(IMemberType item, IPropertyType property, XElement node)
    {
        var changes = new List<uSyncChange>();

        var canEdit = node.Element("CanEdit").ValueOrDefault(false);
        if (item.MemberCanEditProperty(property.Alias) != canEdit)
        {
            changes.AddUpdate("CanEdit", !canEdit, canEdit, $"{property.Alias}/CanEdit");
            item.SetMemberCanEditProperty(property.Alias, canEdit);
        }

        var canView = node.Element("CanView").ValueOrDefault(false);
        if (item.MemberCanViewProperty(property.Alias) != canView)
        {
            changes.AddUpdate("CanView", !canView, canView, $"{property.Alias}/CanView");
            item.SetMemberCanViewProperty(property.Alias, canView);
        }

        var isSensitive = node.Element("IsSensitive").ValueOrDefault(true);
        if (item.IsSensitiveProperty(property.Alias) != isSensitive)
        {
            changes.AddUpdate("IsSensitive", !isSensitive, isSensitive, $"{property.Alias}/IsSensitive");
            item.SetIsSensitiveProperty(property.Alias, isSensitive);
        }

        return changes;
    }


	protected override Task<Attempt<IMemberType?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
	{
        var safeAlias = GetSafeItemAlias(alias);

        var item = new MemberType(shortStringHelper, -1)
        {
            Alias = safeAlias
        };

        if (parent != null)
        {
            if (parent is IMediaType mediaTypeParent)
                item.AddContentType(mediaTypeParent);

            item.SetParent(parent);
        }

        AddAlias(safeAlias);


        return Task.FromResult(Attempt.Succeed(item as IMemberType));
    }
}
