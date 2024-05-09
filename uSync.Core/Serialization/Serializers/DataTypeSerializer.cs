using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("C06E92B7-7440-49B7-B4D2-AF2BF4F3D75D", "DataType Serializer", uSyncConstants.Serialization.DataType)]
public class DataTypeSerializer : SyncContainerSerializerBase<IDataType>, ISyncSerializer<IDataType>
{
    private readonly IDataTypeService _dataTypeService;
    private readonly IDataTypeContainerService _dataTypeContainerService;
    private readonly DataEditorCollection _dataEditors;
    private readonly ConfigurationSerializerCollection _configurationSerializers;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _jsonSerializer;

	public DataTypeSerializer(IEntityService entityService, ILogger<DataTypeSerializer> logger,
		IDataTypeService dataTypeService,
		IDataTypeContainerService dataTypeContainerService,
		DataEditorCollection dataEditors,
		ConfigurationSerializerCollection configurationSerializers,
		PropertyEditorCollection propertyEditors,
		IConfigurationEditorJsonSerializer jsonSerializer)
		: base(entityService, logger, UmbracoObjectTypes.DataTypeContainer)
	{
		this._dataTypeService = dataTypeService;
		this._dataEditors = dataEditors;
		this._configurationSerializers = configurationSerializers;
		this._propertyEditors = propertyEditors;
		this._jsonSerializer = jsonSerializer;
		_dataTypeContainerService = dataTypeContainerService;
	}

	/// <summary>
	///  Process deletes
	/// </summary>
	/// <remarks>
	///  datatypes are deleted late (in the last pass)
	///  this means they are actually deleted at the very
	///  end of the process. 
	///  
	///  In theory this should be fine, 
	///  
	///  any content types that may or may not use
	///  datatypes we are about to delete will have
	///  already been updated. 
	/// 
	///  by moving the datatypes to the end we capture the 
	///  case where the datatype might have been replaced 
	///  in the content type, by not deleting first we 
	///  stop the triggering of any of Umbraco's delete
	///  processes.
	///  
	///  this only works because we are keeping the track of
	///  all the deletes and renames when they happen
	///  and we can only reliably do that for items
	///  that have ContainerTree's because they are not 
	///  real trees - but flat (each alias is unique)
	/// </remarks>
	protected override async Task<SyncAttempt<IDataType>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags, Guid userKey)
	{
        if (flags.HasFlag(SerializerFlags.LastPass))
        {
            logger.LogDebug("Processing deletes as part of the last pass)");
            return await base.ProcessDeleteAsync(key, alias, flags, userKey);
        }

        logger.LogDebug("Delete not processing as this is not the final pass");
        return SyncAttempt<IDataType>.Succeed(alias, ChangeType.Hidden);
    }

	protected override async Task<SyncAttempt<IDataType>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
        var info = node.Element(uSyncConstants.Xml.Info);
        var name = info?.Element(uSyncConstants.Xml.Name).ValueOrDefault(string.Empty) ?? string.Empty;
        var key = node.GetKey();

        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception("Unknown serialization error");

        var details = new List<uSyncChange>();
        var item = attempt.Result;

        // basic
        if (item.Name is not null && item.Name != name)
        {
            details.AddUpdate(uSyncConstants.Xml.Name, item.Name, name, uSyncConstants.Xml.Name);
            item.Name = name;
        }

        if (item.Key != key)
        {
            details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key, uSyncConstants.Xml.Key);
            item.Key = key;
        }

        var editorAlias = info?.Element("EditorAlias").ValueOrDefault(string.Empty) ?? string.Empty;
        if (editorAlias != item.EditorAlias)
        {
            // change the editor type.....
            var newEditor = FindDataEditor(editorAlias);
            if (newEditor != null)
            {
                details.AddUpdate("EditorAlias", item.EditorAlias, editorAlias, "EditorAlias");
                item.Editor = newEditor;
            }
        }

        // removing sort order - as its not used on datatypes, 
        // and can change based on minor things (so gives false out of sync results)

        // item.SortOrder = info.Element("SortOrder").ValueOrDefault(0);
        var dbType = info?.Element("DatabaseType")?.ValueOrDefault(ValueStorageType.Nvarchar) ?? ValueStorageType.Nvarchar;
        if (item.DatabaseType != dbType)
        {
            details.AddUpdate("DatabaseType", item.DatabaseType, dbType, "DatabaseType");
            item.DatabaseType = dbType;
        }

        // config 
        if (ShouldDesterilizeConfig(name, editorAlias, options))
        {
            details.AddRange(DeserializeConfiguration(item, node));
        }

        details!.AddNotNull(await SetFolderFromElementAsync(item, info?.Element("Folder")));

        return SyncAttempt<IDataType>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, details);

    }

    private async Task<uSyncChange?> SetFolderFromElementAsync(IDataType item, XElement? folderNode)
    {
        if (folderNode == null) return null;

        var folder = folderNode.ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(folder)) return null;

        var container = await FindFolderAsync(folderNode.GetKey(), folder);
        if (container != null && container.Id != item.ParentId)
        {
            var change = uSyncChange.Update("", "Folder", container.Id, item.ParentId);

            item.SetParent(container);

            return change;
        }

        return null;
    }

	private List<uSyncChange> DeserializeConfiguration(IDataType item, XElement node)
    {
        var config = node.Element("Config").ValueOrDefault(string.Empty);
        if (string.IsNullOrEmpty(config)) return [];

        var changes = new List<uSyncChange>();

        if (config.TryDeserialize(out IDictionary<string, object>? importData) is false || importData is null)
        {
            changes.AddWarning("Data", item.Name ?? item.Id.ToString(), "Failed to deserialize config for item");
            return changes;
        }

		// v8,9,etc configs the properties 
		importData = importData.ConvertToCamelCase();

		// multiple serializers can run per property. 
		var serializers = _configurationSerializers.GetSerializers(item.EditorAlias);
        foreach(var serializer in serializers) {
            logger.LogDebug("Running Configuration Serializer : {name} for {type}", serializer.Name, item.EditorAlias);
            importData = serializer.GetConfigurationImport(importData);
        }

        if (IsJsonEqual(importData, item.ConfigurationData) is false)
        {
            changes.AddUpdateJson("Data", item.ConfigurationData, importData, "Configuration Data");
            logger.LogDebug("Setting Config for {item} : {data}", item.Name, importData);
            item.ConfigurationData = importData;
        }
        // else no change. 

        return changes;

    }

    /// <summary>
    ///  tells us if the json for an object is equal, helps when the config objects don't have their
    ///  own Equals functions
    /// </summary>
    private static bool IsJsonEqual(object currentObject, object newObject)
    {
        var currentString = currentObject.SerializeJsonString(false);
        var newString = newObject.SerializeJsonString(false);
        return currentString == newString;
    }


	///////////////////////

	protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IDataType item, SyncSerializerOptions options)
	{
        var node = InitializeBaseNode(item, item.Name ?? item.Id.ToString(), item.Level);

        var info = new XElement(uSyncConstants.Xml.Info,
            new XElement(uSyncConstants.Xml.Name, item.Name),
            new XElement("EditorAlias", item.EditorAlias),
            new XElement("DatabaseType", item.DatabaseType));
        // new XElement("SortOrder", item.SortOrder));

        if (item.Level != 1)
        {
            var folderNode = await this.GetFolderNodeAsync(item); //TODO - CACHE THIS CALL. 
            if (folderNode != null)
                info.Add(folderNode);
        }

        node.Add(info);

        var config = SerializeConfiguration(item);
        if (config != null)
            node.Add(config);

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Id.ToString(), node, typeof(IDataType), ChangeType.Export);
    }

    private XElement SerializeConfiguration(IDataType item)
    {
        var serializer = _configurationSerializers.GetSerializer(item.EditorAlias);
        
        var configurationObject = TryGetConfigurationObject(item);

        // merge the configurationData and configurationObject into one dictionary
        // there might be duplicates, but they will be of the same value. 
        var merged = configurationObject?.TryConvertToDictionary(out var objectDictionary) is true
            ? item.ConfigurationData.MergeIgnoreDuplicates(objectDictionary)
            : item.ConfigurationData;

        var exportConfig = serializer == null ? merged : serializer.GetConfigurationExport(merged);

        var json = exportConfig
            .OrderBy(x => x.Key)
            .ToDictionary()
            .SerializeJsonString() ?? string.Empty;
        return new XElement("Config", new XCData(json));
    }

    private static object? TryGetConfigurationObject(IDataType item)
    {
        // PREVIEW008 - ISSUE.
        // getting the object can cause an exception if the inner data is badly formatted.
        try
        {
            return item.ConfigurationObject;
        }
        catch
        {
            return null;
        }
    }

    protected override Attempt<IDataType?> CreateItem(string alias, ITreeEntity? parent, string itemType)
    {
        var editorType = FindDataEditor(itemType);
        if (editorType == null)
            return Attempt.Fail<IDataType?>(default, new ArgumentException($"(Missing Package?) DataEditor {itemType} is not installed"));

        var item = new DataType(editorType, _jsonSerializer, -1)
        {
            Name = alias
        };

        if (parent != null)
            item.SetParent(parent);

        return Attempt.Succeed((IDataType)item);
    }

	private IDataEditor? FindDataEditor(string editorAlias)
	{
		var newEditor = _dataEditors.FirstOrDefault(x => x.Alias.InvariantEquals(editorAlias))
			?? _propertyEditors.FirstOrDefault(x => x.Alias.InvariantEquals(editorAlias));

		if (newEditor is not null) return newEditor;

		var serializers = _configurationSerializers.GetSerializers(editorAlias);
		if (serializers is null) return null;

		foreach (var serializer in serializers)
		{
			var newAlias = serializer.GetEditorAlias();
			if (newAlias is not null)
			{
				newEditor = _dataEditors.FirstOrDefault(x => x.Alias.InvariantEquals(newAlias))
					?? _propertyEditors.FirstOrDefault(x => x.Alias.InvariantEquals(newAlias));

                if (newEditor is not null)
                {
                    logger.LogDebug("Editor replacement for {alias} found : {newAlias}", editorAlias, newAlias);
                    return newEditor;
                }
			}
		}

		return null;


	}
	protected override string GetItemBaseType(XElement node)
        => node.Element(uSyncConstants.Xml.Info)?.Element("EditorAlias").ValueOrDefault(string.Empty) ?? string.Empty;

	public override async Task<IDataType?> FindItemAsync(Guid key)
        => await _dataTypeService.GetAsync(key);

    public override async Task<IDataType?> FindItemAsync(string alias)
        => await _dataTypeService.GetAsync(alias);

    public override async Task SaveItemAsync(IDataType? item, Guid userKey)
    {
        if (item is null) return;

		if (item.Id <= 0)
			await _dataTypeService.CreateAsync(item, userKey);
        else 
            await _dataTypeService.UpdateAsync(item, userKey);
	}

	public override async Task DeleteItemAsync(IDataType? item, Guid userKey)
	{
        if (item is null) return;
		await _dataTypeService.DeleteAsync(item.Key, userKey);
	}

    public override string ItemAlias(IDataType item)
        => item.Name ?? item.Id.ToString();


    /// <summary>
    ///  Checks the config to see if we should be deserializing the config element of a data type.
    /// </summary>
    /// <remarks>
    ///   a key value on the handler will allow users to add editorAliases that they don't want the 
    ///   config importing for. 
    ///   e.g - to not import all the colour picker values.
    ///   <code>
    ///      <Add Key="NoConfigEditors" Value="Umbraco.ColorPicker" />
    ///   </code>
    ///   
    ///   To ignore just specific colour pickers (so still import config for other colour pickers)
    ///   <code>
    ///     <Add Key="NoConfigNames" Value="Approved Colour,My Colour Picker" />
    ///   </code>
    /// </remarks>
    private static bool ShouldDesterilizeConfig(string itemName, string editorAlias, SyncSerializerOptions options)
    {
        var noConfigEditors = options.GetSetting(
            uSyncConstants.DefaultSettings.NoConfigEditors,
            uSyncConstants.DefaultSettings.NoConfigEditors_Default);

        if (!string.IsNullOrWhiteSpace(noConfigEditors) && noConfigEditors.InvariantContains(editorAlias))
            return false;

        var noConfigAliases = options.GetSetting(
            uSyncConstants.DefaultSettings.NoConfigNames,
            uSyncConstants.DefaultSettings.NoConfigNames_Default);

        if (!string.IsNullOrWhiteSpace(noConfigAliases) && noConfigAliases.InvariantContains(itemName))
            return false;

        return true;
    }


	protected override async Task<Attempt<EntityContainer?, EntityContainerOperationStatus>> CreateContainerAsync(Guid key, string name, Guid parentKey, Guid userKey)
        => await _dataTypeContainerService.CreateAsync(key, name, parentKey, userKey);

    protected override async Task SaveContainerAsync(EntityContainer container, Guid userKey)
    {
        if (container.Id == 0) {
			var parentGuid = container.ParentId == 0 ? Guid.Empty : (await _dataTypeContainerService.GetParentAsync(container))?.Key ?? Guid.Empty;
			await _dataTypeContainerService.CreateAsync(container.Key, container.Name ?? "container", parentGuid, userKey);
            return;
        }

        await _dataTypeContainerService.UpdateAsync(container.Key, container.Name ?? "container", userKey); 
    }

	protected override async Task<EntityContainer?> GetContainerAsync(IDataType item)
        => await _dataTypeContainerService.GetParentAsync(item);
}
