using System.Collections.Generic;

namespace uSync.BackOffice;

/// <summary>
/// Constant values for uSync 
/// </summary>
public static partial class uSyncConstants
{
	public const string MergedFolderName = "Combined";

	/// <summary>
	/// Information about the package name/files
	/// </summary>
	public static class Package
	{
		/// <summary>
		///  Name of the Package
		/// </summary>
		public const string Name = "uSync";

		/// <summary>
		///  Virtual path to the plugin files
		/// </summary>
		public const string PluginPath = "/App_Plugins/uSync";
	}

	/// <summary>
	/// Suffix to place on any release strings 
	/// </summary>
	public const string ReleaseSuffix = "";

	/// <summary>
	///  ordering of the handler items (what gets done when)
	/// </summary>
	public static class Priorites
	{
		/// <summary>
		/// Lower bound of uSync's reserved priority range
		/// </summary>
		public const int USYNC_RESERVED_LOWER = 1000;

		/// <summary>
		/// Upper bound of uSync's reserved priority range 
		/// </summary>
		public const int USYNC_RESERVED_UPPER = 2000;

		/// <summary>
		/// DataTypes priority
		/// </summary>
		public const int DataTypes = USYNC_RESERVED_LOWER + 10;

		/// <summary>
		/// Templates priority
		/// </summary>
		public const int Templates = USYNC_RESERVED_LOWER + 20;

		/// <summary>
		/// ContentTypes priority
		/// </summary>
		public const int ContentTypes = USYNC_RESERVED_LOWER + 30;

		/// <summary>
		/// MediaTypes priority
		/// </summary>
		public const int MediaTypes = USYNC_RESERVED_LOWER + 40;

		/// <summary>
		/// MemberTypes priority
		/// </summary>
		public const int MemberTypes = USYNC_RESERVED_LOWER + 45;

		/// <summary>
		/// Languages priority
		/// </summary>
		public const int Languages = USYNC_RESERVED_LOWER + 5;

		/// <summary>
		/// DictionaryItems priority
		/// </summary>
		public const int DictionaryItems = USYNC_RESERVED_LOWER + 6;

		/// <summary>
		/// Macros priority
		/// </summary>
		public const int Macros = USYNC_RESERVED_LOWER + 70;

		/// <summary>
		/// Media priority
		/// </summary>
		public const int Media = USYNC_RESERVED_LOWER + 200;

		/// <summary>
		/// Content priority
		/// </summary>
		public const int Content = USYNC_RESERVED_LOWER + 210;

		/// <summary>
		/// ContentTemplate priority
		/// </summary>
		public const int ContentTemplate = USYNC_RESERVED_LOWER + 215;

		/// <summary>
		/// DomainSettings priority
		/// </summary>
		public const int DomainSettings = USYNC_RESERVED_LOWER + 219;

		/// <summary>
		/// DataTypeMappings priority
		/// </summary>
		public const int DataTypeMappings = USYNC_RESERVED_LOWER + 220;

		/// <summary>
		/// RelationTypes priority
		/// </summary>
		public const int RelationTypes = USYNC_RESERVED_LOWER + 230;

		/// <summary>
		///  Webhooks priority.
		/// </summary>
		public const int Webhooks = USYNC_RESERVED_LOWER + 250;
	}

	/// <summary>
	///  Default group names 
	/// </summary>
	public static class Groups
	{
		/// <summary>
		/// Name for the settings group of handlers 
		/// </summary>
		public const string Settings = "Settings";

		/// <summary>
		/// Name for the Content group of handlers 
		/// </summary>
		public const string Content = "Content";

		/// <summary>
		/// Name for the Members group of handlers 
		/// </summary>
		public const string Members = "Members";

		/// <summary>
		/// Name for the > group of handlers 
		/// </summary>
		public const string Users = "Users";

		/// <summary>
		/// Name for the Forms group of handlers 
		/// </summary>
		public const string Forms = "Forms";

		/// <summary>
		/// Name for the Files group of handlers 
		/// </summary>
		public const string Files = "Files";

		/// <summary>
		/// Name for the default group (used for loading default global config)
		/// </summary>
		public const string Default = "__default__";

		/// <summary>
		/// Icons for the well known handler groups.
		/// </summary>
		public static Dictionary<string, string> Icons = new Dictionary<string, string> {
			{ Settings, "icon-settings-alt color-blue" },
			{ Content, "icon-documents color-purple" },
			{ Members, "icon-users" },
			{ Users, "icon-users color-green"},
			{ Default, "icon-settings" },
			{ Forms, "icon-umb-contour" },
			{ Files, "icon-script-alt" }
		};
	}

	/// <summary>
	/// names of the well know handlers 
	/// </summary>
	public static class Handlers
	{
		/// <summary>
		/// ContentHandler Name
		/// </summary>
		public const string ContentHandler = "ContentHandler";

		/// <summary>
		/// ContentTemplateHandler Name
		/// </summary>
		public const string ContentTemplateHandler = "ContentTemplateHandler";

		/// <summary>
		/// ContentTypeHandler Name
		/// </summary>
		public const string ContentTypeHandler = "ContentTypeHandler";

		/// <summary>
		/// DataTypeHandler Name
		/// </summary>
		public const string DataTypeHandler = "DataTypeHandler";

		/// <summary>
		/// DictionaryHandler Name
		/// </summary>
		public const string DictionaryHandler = "DictionaryHandler";

		/// <summary>
		/// DomainHandler Name
		/// </summary>
		public const string DomainHandler = "DomainHandler";

		/// <summary>
		/// LanguageHandler Name
		/// </summary>
		public const string LanguageHandler = "LanguageHandler";

		/// <summary>
		/// MacroHandler Name
		/// </summary>
		public const string MacroHandler = "MacroHandler";

		/// <summary>
		/// MediaHandler Name
		/// </summary>
		public const string MediaHandler = "MediaHandler";

		/// <summary>
		/// MediaTypeHandler Name
		/// </summary>
		public const string MediaTypeHandler = "MediaTypeHandler";

		/// <summary>
		/// MemberTypeHandler Name
		/// </summary>
		public const string MemberTypeHandler = "MemberTypeHandler";

		/// <summary>
		/// RelationTypeHandler Name
		/// </summary>
		public const string RelationTypeHandler = "RelationTypeHandler";

		/// <summary>
		/// TemplateHandler Name
		/// </summary>
		public const string TemplateHandler = "TemplateHandler";

		/// <summary>
		///  WebhooksHandler name
		/// </summary>
		public const string WebhookHandler = "WebhookHandler";


	}
}
