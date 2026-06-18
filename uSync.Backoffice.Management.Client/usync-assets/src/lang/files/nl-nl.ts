export default {
	uSync: {
		section: 'Synchronisatie',
		name: 'uSync',
		banner: 'uSync voor alles',

		Report: 'Rapport',
		Import: 'Importeren',
		Export: 'Exporteren',
		ImportForce: 'Importeren (Forceren)',
		ExportClean: 'Exporteren (Schoon)',
		ExportFile: 'Exporteren naar bestand',
		ImportFile: 'Importeren vanuit bestand',

		noChange: 'Er is niets veranderd',
		showAll: 'Alle items weergeven',

		detailHeadline: 'Gedetecteerde wijzigingen',
		detailHeader: 'Wat er anders is',

		runningInBackground:
			'uSync voert dit proces op de achtergrond uit. Als u van deze pagina navigeert, blijft het doorlopen.',

		connectionLost:
			'De verbinding met de server is verbroken. Het proces blijft op de achtergrond draaien, maar u ziet hier geen updates.',

		importSingle: 'Importeren',
		importSingleWarning:
			'Dit importeert dit item in Umbraco. Als dit item afhankelijkheden heeft, worden deze niet geïmporteerd en moeten handmatig worden opgelost.',
		importSingleSuccess: 'Het item is succesvol geïmporteerd',
		importSingleFailed: `<p>Er is een fout opgetreden bij het importeren van het item</p><strong>%0%</strong>`,

		changeAction: 'Actie',
		changeItem: 'Item',
		changeDiffrence: 'Verschil',
		changeCreate: 'Dit item bestaat niet in Umbraco en wordt aangemaakt',
		noChangesImport: 'Er zijn geen wijzigingen aangebracht aan dit item',
		noChangesReport: 'Geen wijzigingen gedetecteerd',
		noChangesDelete: 'Dit item is verwijderd uit Umbraco',

		importHeader: 'Importeren vanuit bestand',

		success: 'Succes',
		change: 'Wijziging',
		changeType: 'Type',
		changeName: 'Naam',
		changeDetail: 'Detail',
		changeHeading: 'Resultaten',
		changeCount: '{1}/{0} wijzigingen',
		noChangeCount: '0/{0} wijzigingen',

		legacyInfo: `<p>uSync heeft een verouderde uSync-map gevonden op <strong>%0%</strong>.<br />
						Het is waarschijnlijk dat de inhoud op de een of andere manier geconverteerd moet worden</p>`,

		legacyObsolete: `<h4>Verouderde gegevenstypen</h4>
				<ul>%0%</ul>
				<p>U kunt deze gegevenstypen converteren met
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br />
					<em>(In de volledige uSync-release zal de conversie hier plaatsvinden.)</em></p>`,

		legacyCopy: `<h4>Kopiëren naar uSync/v14</h4>
					<p>U kunt uw %0%-map kopiëren naar de ~/uSync/v14-map<br />en een import uitvoeren.</p>
					<p>Als er niets geconverteerd hoeft te worden, zou alles correct moeten importeren.</p>
					<p><strong>Verwijder of hernoem de %0%-map om deze popup te voorkomen</strong></p>`,

		hmacMismatch: `<h4>HMAC-mismatch <uui-icon name="alert"></uui-icon></h4>
			<p>Het lijkt erop dat de <code>Imaging:HMAC</code>-instelling die werd gebruikt om de bestanden in de uSync-map te genereren, niet overeenkomt met de huidige instelling voor deze site.</p>
			<p>Afbeeldingen in RTE-besturingselementen hebben de HMAC-waarde toegevoegd aan de URL-waarde, en zonder aanvullende configuratie worden deze afbeeldingen mogelijk niet correct weergegeven.</p>
			<ul><li>U kunt "HMAC-mapping" inschakelen in uSync,</li>
			<li>of u kunt ervoor zorgen dat de HMAC-waarde in de <code>Imaging:HMAC</code>-instelling overeenkomt met de waarde die werd gebruikt om de bestanden in de uSync-map te genereren</li></ul>`,
		formatMismatch:
			'De versie van het synchronisatiebestandsformaat komt niet overeen met de verwachte versie. Dit kan wijzen op een mogelijk compatibiliteitsprobleem.',

		legacyBanner:
			'Deze site bevat bestanden van een eerdere versie van uSync. Bekijk de details op het tabblad Verouderd.',

		legacyCopyTitle: 'v%0%-bestanden overschrijven',
		legacyCopyContent:
			'Weet u zeker dat u de inhoud van de map %0% wilt overschrijven met de verouderde uSync-mapbestanden?',

		legacyIgnoreTitle: 'Verouderde bestanden negeren',
		legacyIgnoreContent:
			'Weet u zeker dat u de bestanden in de verouderde uSync-map wilt negeren?',

		errorHeader:
			'Dit item heeft een fout ondervonden tijdens het proces. De details staan hieronder:',

		uploadIntro: 'Selecteer een zip-bestand met uSync-bestanden die u wilt uploaden',
		uploadSuccess: 'De bestanden zijn geüpload en uitgepakt naar de uSync-map',
		uploadError: 'Er is een fout opgetreden bij het uploaden van de bestanden',

		ILanguage: 'Taal',
		IDictionaryItem: 'Woordenboekitems',
		IDataType: 'Gegevenstypen',
		ITemplate: 'Sjablonen',
		IContentType: 'Inhoudstypen',
		IMediaType: 'Mediatypen',
		IMemberType: 'Ledentypen',
		IContent: 'Inhoud',
		IMedia: 'Media',
		IDomain: 'Domeinen',
		IWebhook: 'Webhooks',
		IRelationType: 'Relatietypen',
		MediaFile: 'Mediabestanden',
		XElement: 'Overig',
		LanguageHandler: 'Talen',
		DictionaryHandler: 'Woordenboekitems',
		DataTypeHandler: 'Gegevenstypen',
		TemplateHandler: 'Sjablonen',
		ContentTypeHandler: 'Inhoudstypen',
		MediaTypeHandler: 'Mediatypen',
		MemberTypeHandler: 'Ledentypen',
		ContentHandler: 'Inhoud',
		MediaHandler: 'Media',
		RelationTypeHandler: 'Relatietypen',
		EntityContainer: 'Containers',
	},
	USyncSettings: {
		settings: 'uSync-instellingen',
		filesAndFolders: 'Bestanden en mappen',
		handlerDefaults: 'Handler-standaardwaarden',

		processingMode: 'Verwerkingsmodus',
		processingModeDesc:
			'Hoe het uSync-proces wordt uitgevoerd, op de achtergrond of interactief (Normaal)',

		importAtStartup: 'Importeren bij opstarten',
		importAtStartupDesc: 'Een import van bestanden van de schijf uitvoeren wanneer Umbraco start',

		exportAtStartup: 'Exporteren bij opstarten',
		exportAtStartupDesc: 'De Umbraco-instellingen exporteren wanneer de site opstart',

		exportOnSave: 'Exporteren bij opslaan',
		exportOnSaveDesc: 'uSync-bestanden genereren wanneer items worden opgeslagen',

		uiEnabledGroups: 'UI-ingeschakelde groepen',
		uiEnabledGroupsDesc: 'Handlergroepen die zichtbaar/bruikbaar zijn op het dashboard',

		failOnMissingParent: 'Fout bij ontbrekend bovenliggend item',
		failOnMissingParentDesc: 'Fout bij ontbrekend bovenliggend item',

		currentHandlerSet: 'Huidige set',

		handlerSet: 'Standaard handlerset',
		handlerSetDesc: 'De standaard handlerset voor de site',

		flatStructure: 'Platte structuur',
		flatStructureDesc: 'Alle items van een type worden opgeslagen in een platte mappenstructuur',

		guidNames: 'GUID\'s als bestandsnamen gebruiken',
		guidNamesDesc: 'De GUID van een item als bestandsnaam gebruiken',

		handlerGroups: 'Handlergroepen',
		handlerGroupsDesc: 'Groepen om de handlerset te beperken',

		disabledHandlers: 'Uitgeschakelde handlers',
		disabledHandlersDesc: 'Handlers die expliciet zijn uitgeschakeld voor deze handlerset',

		folders: 'Mappen',
		foldersDesc:
			'Mappen waarin uSync naar bestanden zoekt (items worden normaal opgeslagen in de laatste map in de lijst)',

		rootSite: 'Rootsite',
		rootSiteDesc: 'Is deze site een root voor andere sites.',

		rootLocked: 'Root vergrendeld',
		rootLockedDesc: 'Zijn wijzigingen voor items van de rootsite vergrendeld?',

		help: 'Instellingen worden beheerd via het bestand appsettings.json. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">Zie onze documentatie</a>',

		bootSettings: 'Instellingen voor eerste opstart 🥾',

		firstBoot: 'Importeren bij eerste opstart',
		firstBootDesc: 'Het importproces uitvoeren bij de eerste opstart van de site',

		firstBootGroup: 'Groepen voor eerste opstart',
		firstBootGroupDesc: 'De groepen die worden uitgevoerd bij de eerste opstart',
	},
};
