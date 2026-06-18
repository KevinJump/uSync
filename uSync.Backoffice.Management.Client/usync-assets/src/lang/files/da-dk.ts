export default {
	uSync: {
		section: 'Synkronisering',
		name: 'uSync',
		banner: 'uSync alt på én gang',

		Report: 'Rapport',
		Import: 'Importer',
		Export: 'Eksporter',
		ImportForce: 'Importer (Tvungen)',
		ExportClean: 'Eksporter (Ren)',
		ExportFile: 'Eksporter til fil',
		ImportFile: 'Importer fra fil',

		noChange: 'Intet har ændret sig',
		showAll: 'Vis alle elementer',

		detailHeadline: 'Registrerede ændringer',
		detailHeader: 'Ting der er anderledes',

		runningInBackground:
			'uSync kører denne proces i baggrunden. Hvis du navigerer væk fra denne side, vil den fortsætte med at køre.',

		connectionLost:
			'Forbindelsen til serveren er gået tabt. Processen vil fortsætte med at køre i baggrunden, men du vil ikke se opdateringer her.',

		importSingle: 'Importer',
		importSingleWarning:
			'Dette vil importere dette element til Umbraco. Hvis dette element har afhængigheder, vil de ikke blive importeret og skal løses manuelt.',
		importSingleSuccess: 'Elementet er blevet importeret korrekt',
		importSingleFailed: `<p>Der opstod en fejl ved import af elementet</p><strong>%0%</strong>`,

		changeAction: 'Handling',
		changeItem: 'Element',
		changeDiffrence: 'Forskel',
		changeCreate: 'Dette element findes ikke i Umbraco og oprettes',
		noChangesImport: 'Der blev ikke foretaget ændringer i dette element',
		noChangesReport: 'Ingen ændringer registreret',
		noChangesDelete: 'Dette element er blevet slettet fra Umbraco',

		importHeader: 'Importer fra fil',

		success: 'Succes',
		change: 'Ændring',
		changeType: 'Type',
		changeName: 'Navn',
		changeDetail: 'Detalje',
		changeHeading: 'Resultater',
		changeCount: '{1}/{0} ændringer',
		noChangeCount: '0/{0} ændringer',

		legacyInfo: `<p>uSync har fundet en ældre uSync-mappe på <strong>%0%</strong>.<br />
						Det er sandsynligt, at indholdet skal konverteres på en eller anden måde</p>`,

		legacyObsolete: `<h4>Forældede datatyper</h4>
				<ul>%0%</ul>
				<p>Du kan konvertere disse datatyper ved hjælp af
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br />
					<em>(I den fulde uSync-udgivelse vil konvertering ske her.)</em></p>`,

		legacyCopy: `<h4>Kopiér til uSync/v14</h4>
					<p>Du kan kopiere din %0%-mappe til ~/uSync/v14-mappen<br />og køre en import.</p>
					<p>Hvis intet skal konverteres, bør alt importeres korrekt.</p>
					<p><strong>Fjern eller omdøb %0%-mappen for at forhindre denne popup</strong></p>`,

		hmacMismatch: `<h4>HMAC-uoverensstemmelse <uui-icon name="alert"></uui-icon></h4>
			<p>Det ser ud til, at indstillingen <code>Imaging:HMAC</code>, der bruges til at generere filerne i uSync-mappen, ikke stemmer overens med den aktuelle indstilling for dette websted.</p>
			<p>Billeder i RTE-kontrolelementer vil have HMAC-værdien tilføjet til URL-værdien, og uden yderligere konfiguration vises disse billeder muligvis ikke korrekt.</p>
			<ul><li>Du kan aktivere "HMAC-mapping" i uSync,</li>
			<li>eller du kan sikre, at HMAC-værdien i indstillingen <code>Imaging:HMAC</code> stemmer overens med den værdi, der bruges til at generere filerne i uSync-mappen</li></ul>`,
		formatMismatch:
			'Synkroniseringsfilformatversionen stemmer ikke overens med den forventede version. Dette kan indikere et potentielt kompatibilitetsproblem.',

		legacyBanner:
			'Dette websted indeholder filer fra en tidligere version af uSync. Se detaljerne under fanen Ældre.',

		legacyCopyTitle: 'Overskriv v%0%-filer',
		legacyCopyContent:
			'Er du sikker på, at du vil overskrive indholdet af mappen %0% med de ældre uSync-mappefilerne?',

		legacyIgnoreTitle: 'Ignorer ældre filer',
		legacyIgnoreContent:
			'Er du sikker på, at du vil ignorere filerne i den ældre uSync-mappe?',

		errorHeader:
			'Dette element stødte på en fejl under processen. Detaljerne er nedenfor:',

		uploadIntro: 'Vælg en zip-fil med uSync-filer, som du vil uploade',
		uploadSuccess: 'Filerne er blevet uploadet og udtrukket til uSync-mappen',
		uploadError: 'Der opstod en fejl ved upload af filerne',

		ILanguage: 'Sprog',
		IDictionaryItem: 'Ordbogselementer',
		IDataType: 'Datatyper',
		ITemplate: 'Skabeloner',
		IContentType: 'Indholdstyper',
		IMediaType: 'Medietyper',
		IMemberType: 'Medlemstyper',
		IContent: 'Indhold',
		IMedia: 'Medie',
		IDomain: 'Domæner',
		IWebhook: 'Webhooks',
		IRelationType: 'Relationstyper',
		MediaFile: 'Mediefiler',
		XElement: 'Andet',
		LanguageHandler: 'Sprog',
		DictionaryHandler: 'Ordbogselementer',
		DataTypeHandler: 'Datatyper',
		TemplateHandler: 'Skabeloner',
		ContentTypeHandler: 'Indholdstyper',
		MediaTypeHandler: 'Medietyper',
		MemberTypeHandler: 'Medlemstyper',
		ContentHandler: 'Indhold',
		MediaHandler: 'Medie',
		RelationTypeHandler: 'Relationstyper',
		EntityContainer: 'Containere',
	},
	USyncSettings: {
		settings: 'uSync-indstillinger',
		filesAndFolders: 'Filer og mapper',
		handlerDefaults: 'Handlerstandarder',

		processingMode: 'Behandlingstilstand',
		processingModeDesc:
			'Hvordan uSync-processen kører, enten i baggrunden eller interaktivt (Normal)',

		importAtStartup: 'Importer ved opstart',
		importAtStartupDesc: 'Kør en import af filer fra disken, når Umbraco starter',

		exportAtStartup: 'Eksporter ved opstart',
		exportAtStartupDesc: 'Eksporter Umbraco-indstillingerne, når webstedet starter',

		exportOnSave: 'Eksporter ved gem',
		exportOnSaveDesc: 'Generer uSync-filer, når elementer gemmes',

		uiEnabledGroups: 'Aktiverede UI-grupper',
		uiEnabledGroupsDesc: 'Handlergrupper, der kan ses/bruges på dashboardet',

		failOnMissingParent: 'Fejl ved manglende overordnet',
		failOnMissingParentDesc: 'Fejl ved manglende overordnet element',

		currentHandlerSet: 'Aktuelt sæt',

		handlerSet: 'Standardhandlersæt',
		handlerSetDesc: 'Det standardhandlersæt, der bruges for webstedet',

		flatStructure: 'Flad struktur',
		flatStructureDesc: 'Alle elementer af en type gemmes i en flad mappestruktur',

		guidNames: 'Brug GUID som filnavne',
		guidNamesDesc: 'Brug et elements GUID som filnavn',

		handlerGroups: 'Handlergrupper',
		handlerGroupsDesc: 'Grupper til at begrænse handlersættet til',

		disabledHandlers: 'Deaktiverede handlere',
		disabledHandlersDesc: 'Handlere eksplicit deaktiveret for dette handlersæt',

		folders: 'Mapper',
		foldersDesc:
			'Mapper uSync vil søge efter filer i (elementer gemmes normalt i den sidste mappe på listen)',

		rootSite: 'Rodwebsted',
		rootSiteDesc: 'Er dette websted en rod for andre websteder.',

		rootLocked: 'Rod låst',
		rootLockedDesc: 'Er ændringer for elementer fra rodwebstedet låst?',

		help: 'Indstillinger styres via filen appsettings.json. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">Se vores dokumentation</a>',

		bootSettings: 'Indstillinger for første opstart 🥾',

		firstBoot: 'Importer ved første opstart',
		firstBootDesc: 'Kør importprocessen ved webstedets første opstart',

		firstBootGroup: 'Grupper for første opstart',
		firstBootGroupDesc: 'De grupper, der køres ved første opstart',
	},
};
