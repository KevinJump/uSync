export default {
	uSync: {
		section: 'Synchronisation',
		name: 'uSync',
		banner: 'uSync für alles',

		Report: 'Bericht',
		Import: 'Importieren',
		Export: 'Exportieren',
		ImportForce: 'Importieren (Erzwingen)',
		ExportClean: 'Exportieren (Bereinigt)',
		ExportFile: 'In Datei exportieren',
		ImportFile: 'Aus Datei importieren',

		noChange: 'Es hat sich nichts geändert',
		showAll: 'Alle Elemente anzeigen',

		detailHeadline: 'Erkannte Änderungen',
		detailHeader: 'Was sich unterscheidet',

		runningInBackground:
			'uSync führt diesen Prozess im Hintergrund aus. Wenn Sie diese Seite verlassen, wird er weiterhin ausgeführt.',

		connectionLost:
			'Die Verbindung zum Server wurde unterbrochen. Der Prozess wird im Hintergrund weiter ausgeführt, aber Sie werden hier keine Aktualisierungen sehen.',

		importSingle: 'Importieren',
		importSingleWarning:
			'Dadurch wird dieses Element in Umbraco importiert. Wenn dieses Element Abhängigkeiten hat, werden diese nicht importiert und müssen manuell aufgelöst werden.',
		importSingleSuccess: 'Das Element wurde erfolgreich importiert',
		importSingleFailed: `<p>Beim Importieren des Elements ist ein Fehler aufgetreten</p><strong>%0%</strong>`,

		changeAction: 'Aktion',
		changeItem: 'Element',
		changeDiffrence: 'Unterschied',
		changeCreate: 'Dieses Element existiert nicht in Umbraco und wird erstellt',
		noChangesImport: 'Es wurden keine Änderungen an diesem Element vorgenommen',
		noChangesReport: 'Keine Änderungen erkannt',
		noChangesDelete: 'Dieses Element wurde aus Umbraco gelöscht',

		importHeader: 'Aus Datei importieren',

		success: 'Erfolg',
		change: 'Änderung',
		changeType: 'Typ',
		changeName: 'Name',
		changeDetail: 'Detail',
		changeHeading: 'Ergebnisse',
		changeCount: '{1}/{0} Änderungen',
		noChangeCount: '0/{0} Änderungen',

		legacyInfo: `<p>uSync hat einen Legacy-uSync-Ordner unter <strong>%0%</strong> gefunden.<br />
						Der Inhalt muss wahrscheinlich konvertiert werden</p>`,

		legacyObsolete: `<h4>Veraltete Datentypen</h4>
				<ul>%0%</ul>
				<p>Sie können diese Datentypen mit
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a> konvertieren<br />
					<em>(In der vollständigen uSync-Version wird die Konvertierung hier stattfinden.)</em></p>`,

		legacyCopy: `<h4>Nach uSync/v14 kopieren</h4>
					<p>Sie können Ihren %0%-Ordner in den ~/uSync/v14-Ordner kopieren<br />und einen Import ausführen.</p>
					<p>Wenn nichts konvertiert werden muss, sollte alles korrekt importiert werden.</p>
					<p><strong>Entfernen oder benennen Sie den %0%-Ordner um, um dieses Popup zu verhindern</strong></p>`,

		hmacMismatch: `<h4>HMAC-Abweichung <uui-icon name="alert"></uui-icon></h4>
			<p>Es scheint, dass die <code>Imaging:HMAC</code>-Einstellung, die zum Generieren der Dateien im uSync-Ordner verwendet wurde, nicht mit der aktuellen Einstellung dieser Website übereinstimmt.</p>
			<p>Bilder in RTE-Steuerelementen haben den HMAC-Wert an die URL angehängt. Ohne zusätzliche Konfiguration werden diese Bilder möglicherweise nicht korrekt dargestellt.</p>
			<ul><li>Sie können die "HMAC-Zuordnung" in uSync aktivieren,</li>
			<li>oder Sie können sicherstellen, dass der HMAC-Wert in der <code>Imaging:HMAC</code>-Einstellung mit dem Wert übereinstimmt, der zum Generieren der Dateien im uSync-Ordner verwendet wurde</li></ul>`,
		formatMismatch:
			'Die Formatversion der Synchronisierungsdatei stimmt nicht mit der erwarteten Version überein. Dies kann auf ein potenzielles Kompatibilitätsproblem hinweisen.',

		legacyBanner:
			'Diese Website enthält Dateien einer früheren Version von uSync. Details finden Sie auf der Registerkarte Legacy.',

		legacyCopyTitle: 'v%0%-Dateien überschreiben',
		legacyCopyContent:
			'Möchten Sie den Inhalt des Ordners %0% wirklich mit den Legacy-uSync-Ordnerdateien überschreiben?',

		legacyIgnoreTitle: 'Legacy-Dateien ignorieren',
		legacyIgnoreContent:
			'Möchten Sie die Dateien im Legacy-uSync-Ordner wirklich ignorieren?',

		errorHeader:
			'Bei diesem Element ist während des Prozesses ein Fehler aufgetreten. Die Details sind unten aufgeführt:',

		uploadIntro: 'Wählen Sie eine ZIP-Datei mit uSync-Dateien aus, die Sie hochladen möchten',
		uploadSuccess: 'Die Dateien wurden hochgeladen und in den uSync-Ordner extrahiert',
		uploadError: 'Beim Hochladen der Dateien ist ein Fehler aufgetreten',

		ILanguage: 'Sprache',
		IDictionaryItem: 'Wörterbuchelemente',
		IDataType: 'Datentypen',
		ITemplate: 'Vorlagen',
		IContentType: 'Inhaltstypen',
		IMediaType: 'Medientypen',
		IMemberType: 'Mitgliedstypen',
		IContent: 'Inhalt',
		IMedia: 'Medien',
		IDomain: 'Domänen',
		IWebhook: 'Webhooks',
		IRelationType: 'Beziehungstypen',
		MediaFile: 'Mediendateien',
		XElement: 'Sonstiges',
		LanguageHandler: 'Sprachen',
		DictionaryHandler: 'Wörterbuchelemente',
		DataTypeHandler: 'Datentypen',
		TemplateHandler: 'Vorlagen',
		ContentTypeHandler: 'Inhaltstypen',
		MediaTypeHandler: 'Medientypen',
		MemberTypeHandler: 'Mitgliedstypen',
		ContentHandler: 'Inhalt',
		MediaHandler: 'Medien',
		RelationTypeHandler: 'Beziehungstypen',
		EntityContainer: 'Container',
	},
	USyncSettings: {
		settings: 'uSync-Einstellungen',
		filesAndFolders: 'Dateien und Ordner',
		handlerDefaults: 'Handler-Standardwerte',

		processingMode: 'Verarbeitungsmodus',
		processingModeDesc:
			'Wie der uSync-Prozess ausgeführt wird, entweder im Hintergrund oder interaktiv (Normal)',

		importAtStartup: 'Beim Start importieren',
		importAtStartupDesc: 'Einen Import von Dateien von der Festplatte beim Start von Umbraco ausführen',

		exportAtStartup: 'Beim Start exportieren',
		exportAtStartupDesc: 'Die Umbraco-Einstellungen beim Start der Website exportieren',

		exportOnSave: 'Beim Speichern exportieren',
		exportOnSaveDesc: 'uSync-Dateien generieren, wenn Elemente gespeichert werden',

		uiEnabledGroups: 'UI-aktivierte Gruppen',
		uiEnabledGroupsDesc: 'Handler-Gruppen, die im Dashboard angezeigt/verwendet werden können',

		failOnMissingParent: 'Fehler bei fehlendem Elternelement',
		failOnMissingParentDesc: 'Fehler bei fehlendem Elternelement',

		currentHandlerSet: 'Aktuelles Set',

		handlerSet: 'Standard-Handler-Set',
		handlerSetDesc: 'Das Standard-Handler-Set für die Website',

		flatStructure: 'Flache Struktur',
		flatStructureDesc: 'Alle Elemente eines Typs werden in einer flachen Ordnerstruktur gespeichert',

		guidNames: 'GUIDs als Dateinamen verwenden',
		guidNamesDesc: 'Die GUID eines Elements als Dateiname verwenden',

		handlerGroups: 'Handler-Gruppen',
		handlerGroupsDesc: 'Gruppen zur Einschränkung des Handler-Sets',

		disabledHandlers: 'Deaktivierte Handler',
		disabledHandlersDesc: 'Handler, die für dieses Handler-Set explizit deaktiviert sind',

		folders: 'Ordner',
		foldersDesc:
			'Ordner, in denen uSync nach Dateien sucht (Elemente werden normalerweise im letzten Ordner der Liste gespeichert)',

		rootSite: 'Root-Website',
		rootSiteDesc: 'Ist diese Website ein Root für andere Websites.',

		rootLocked: 'Root gesperrt',
		rootLockedDesc: 'Sind Änderungen an Elementen der Root-Website gesperrt?',

		help: 'Die Einstellungen werden über die Datei appsettings.json gesteuert. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">Siehe unsere Dokumentation</a>',

		bootSettings: 'Einstellungen für den ersten Start 🥾',

		firstBoot: 'Beim ersten Start importieren',
		firstBootDesc: 'Den Importprozess beim ersten Start der Website ausführen',

		firstBootGroup: 'Gruppen für den ersten Start',
		firstBootGroupDesc: 'Die Gruppen, die beim ersten Start ausgeführt werden',
	},
};
