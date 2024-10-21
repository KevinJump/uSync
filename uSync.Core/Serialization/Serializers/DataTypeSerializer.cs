using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Api.Management.Factories;
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
		: base(entityService,  dataTypeContainerService, logger, UmbracoObjectTypes.DataTypeContainer)
	{
		this._dataTypeService = dataTypeService;
        this._dataTypeContainerService = dataTypeContainerService;
        this._dataEditors = dataEditors;
		this._configurationSerializers = configurationSerializers;
		this._propertyEditors = propertyEditors;
		this._jsonSerializer = jsonSerializer;
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
    ///  that have ContainerTrees because they are not 
    ///  real trees - but flat (each alias is unique)
    /// </remarks>
    /// 
    protected override async Task<SyncAttempt<IDataType>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        if (flags.HasFlag(SerializerFlags.LastPass))
        {
            logger.LogDebug("Processing deletes as part of the last pass)");
            return await base.ProcessDeleteAsync(key, alias, flags);
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
        var editor = FindDataEditor(editorAlias);
        if (editorAlias != item.EditorAlias)
        {
            // change the editor type.....
            if (editor is not null)
            {
                details.AddUpdate("EditorAlias", item.EditorAlias, editorAlias, "EditorAlias");
                item.Editor = editor;
            }
        }

        var dataBaseType = GetEditorValueStorageType(editor);
        if (item.DatabaseType != dataBaseType)
        {
			details.AddUpdate("DatabaseType", item.DatabaseType, dataBaseType, "DatabaseType");
            item.DatabaseType = dataBaseType;
		}

        var editorUiAlias = info?.Element("EditorUIAlias").ValueOrDefault(string.Empty) ?? string.Empty;

        // migration thing if this is missing we guess it.
        if (editorUiAlias.IsNullOrWhiteSpace())
			editorUiAlias = ToPropertyEditorUiAlias(editorAlias) ?? string.Empty;

        if (item.EditorUiAlias != editorUiAlias)
        {
			details.AddUpdate("EditorUIAlias", item.EditorUiAlias ?? "", editorUiAlias, "EditorUIAlias");
			item.EditorUiAlias = editorUiAlias;
		}

		// we no longer read the db type value from the xml. 
		// info?.Element("DatabaseType")?.ValueOrDefault(ValueStorageType.Nvarchar) ?? ValueStorageType.Nvarchar;

		// config 
		if (ShouldDesterilizeConfig(name, editorAlias, options))
        {
            details.AddRange(DeserializeConfiguration(item, node));
        }

        details!.AddNotNull(await SetFolderFromElementAsync(item, info?.Element("Folder")));

        return SyncAttempt<IDataType>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, details);
    }

    private ValueStorageType GetEditorValueStorageType(IDataEditor? editor)
    {
        if (editor is null) return ValueStorageType.Ntext;
        return ValueTypes.ToStorageType(editor.GetValueEditor().ValueType);
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

        if (importData.IsJsonEqual(item.ConfigurationData) is false)
        {
            changes.AddUpdateJson("Data", item.ConfigurationData, importData, "Configuration Data");
            logger.LogDebug("Setting Config for {item} : {data}", item.Name, importData);
            item.ConfigurationData = importData;
        }
        // else no change. 

        return changes;

    }

    ///////////////////////

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IDataType item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.Name ?? item.Id.ToString(), item.Level);

        var info = new XElement(uSyncConstants.Xml.Info,
            new XElement(uSyncConstants.Xml.Name, item.Name),
            new XElement("EditorAlias", item.EditorAlias),
            new XElement("EditorUIAlias", item.EditorUiAlias));
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

    protected override async Task<Attempt<IDataType?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
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

    public override async Task SaveItemAsync(IDataType item)
    {
        if (item.IsDirty() is false) return;

        if (item.Id <= 0)
            await _dataTypeService.CreateAsync(item, Constants.Security.SuperUserKey);
        else 
            await _dataTypeService.UpdateAsync(item, Constants.Security.SuperUserKey);
    }

    public override async Task SaveAsync(IEnumerable<IDataType> items)
    {
        foreach(var item in items)
            await SaveItemAsync(item);
    }

    public override Task DeleteItemAsync(IDataType item)
        => _dataTypeService.DeleteAsync(item.Key, Constants.Security.SuperUserKey);

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

    /// <summary>
    ///  Convert an editor alias or an property editor ui alias
    /// </summary>
    /// <remarks>
    ///  these values are taken from an Umbraco migration in v14.
    /// </remarks>
    private static string? ToPropertyEditorUiAlias(string editorAlias)
    {
		return editorAlias switch
		{
			Constants.PropertyEditors.Aliases.BlockList => "Umb.PropertyEditorUi.BlockList",
			Constants.PropertyEditors.Aliases.BlockGrid => "Umb.PropertyEditorUi.BlockGrid",
			Constants.PropertyEditors.Aliases.CheckBoxList => "Umb.PropertyEditorUi.CheckBoxList",
			Constants.PropertyEditors.Aliases.ColorPicker => "Umb.PropertyEditorUi.ColorPicker",
			Constants.PropertyEditors.Aliases.ColorPickerEyeDropper => "Umb.PropertyEditorUi.EyeDropper",
			Constants.PropertyEditors.Aliases.ContentPicker => "Umb.PropertyEditorUi.DocumentPicker",
			Constants.PropertyEditors.Aliases.DateTime => "Umb.PropertyEditorUi.DatePicker",
			Constants.PropertyEditors.Aliases.DropDownListFlexible => "Umb.PropertyEditorUi.Dropdown",
			Constants.PropertyEditors.Aliases.ImageCropper => "Umb.PropertyEditorUi.ImageCropper",
			Constants.PropertyEditors.Aliases.Integer => "Umb.PropertyEditorUi.Integer",
			Constants.PropertyEditors.Aliases.Decimal => "Umb.PropertyEditorUi.Decimal",
			Constants.PropertyEditors.Aliases.ListView => "Umb.PropertyEditorUi.Collection",
			Constants.PropertyEditors.Aliases.MediaPicker3 => "Umb.PropertyEditorUi.MediaPicker",
			Constants.PropertyEditors.Aliases.MemberPicker => "Umb.PropertyEditorUi.MemberPicker",
			Constants.PropertyEditors.Aliases.MemberGroupPicker => "Umb.PropertyEditorUi.MemberGroupPicker",
			Constants.PropertyEditors.Aliases.MultiNodeTreePicker => "Umb.PropertyEditorUi.ContentPicker",
			Constants.PropertyEditors.Aliases.MultipleTextstring => "Umb.PropertyEditorUi.MultipleTextString",
			Constants.PropertyEditors.Aliases.Label => "Umb.PropertyEditorUi.Label",
			Constants.PropertyEditors.Aliases.RadioButtonList => "Umb.PropertyEditorUi.RadioButtonList",
			Constants.PropertyEditors.Aliases.Slider => "Umb.PropertyEditorUi.Slider",
			Constants.PropertyEditors.Aliases.Tags => "Umb.PropertyEditorUi.Tags",
			Constants.PropertyEditors.Aliases.TextBox => "Umb.PropertyEditorUi.TextBox",
			Constants.PropertyEditors.Aliases.TextArea => "Umb.PropertyEditorUi.TextArea",
			Constants.PropertyEditors.Aliases.RichText => "Umb.PropertyEditorUi.TinyMCE",
            "Umbraco.TinyMCE" => "Umb.PropertyEditorUi.TinyMCE",
			Constants.PropertyEditors.Aliases.Boolean => "Umb.PropertyEditorUi.Toggle",
			Constants.PropertyEditors.Aliases.MarkdownEditor => "Umb.PropertyEditorUi.MarkdownEditor",
			Constants.PropertyEditors.Aliases.UserPicker => "Umb.PropertyEditorUi.UserPicker",
			Constants.PropertyEditors.Aliases.UploadField => "Umb.PropertyEditorUi.UploadField",
			Constants.PropertyEditors.Aliases.EmailAddress => "Umb.PropertyEditorUi.EmailAddress",
			Constants.PropertyEditors.Aliases.MultiUrlPicker => "Umb.PropertyEditorUi.MultiUrlPicker",
			_ => null
		};
	}
}
