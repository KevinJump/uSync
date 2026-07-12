export default {
	uSync: {
		section: 'Synchronisation',
		name: 'uSync',
		banner: 'uSync pour tout synchroniser',

		migrate: 'Migrer',
		defaultView: 'Défaut',
		settingsView: 'Paramètres',
		addons: 'Extensions',

		groupEverything: 'Tout',
		groupContent: 'Contenu',
		groupSettings: 'Paramètres',
		groupForms: 'Formulaires',
		groupMedia: 'Médias',
		groupMembers: 'Membres',

		Report: 'Rapport',
		Import: 'Importer',
		Export: 'Exporter',
		ImportForce: 'Importer (Forcer)',
		ExportClean: 'Exporter (Propre)',
		ExportFile: 'Exporter vers fichier',
		ImportFile: 'Importer depuis fichier',

		noChange: 'Rien n\'a changé',
		showAll: 'Afficher tous les éléments',

		detailHeadline: 'Modifications détectées',
		detailHeader: 'Ce qui est différent',

		runningInBackground:
			'uSync exécute ce processus en arrière-plan. Si vous quittez cette page, il continuera de fonctionner.',

		connectionLost:
			'La connexion au serveur a été perdue. Le processus continuera de s\'exécuter en arrière-plan, mais vous ne verrez pas les mises à jour ici.',

		importSingle: 'Importer',
		importSingleWarning:
			'Cela importera cet élément dans Umbraco. Si cet élément a des dépendances, elles ne seront pas importées et devront être résolues manuellement.',
		importSingleSuccess: 'L\'élément a été importé avec succès',
		importSingleFailed: `<p>Une erreur s'est produite lors de l'importation de l'élément</p><strong>%0%</strong>`,

		changeAction: 'Action',
		changeItem: 'Élément',
		changeDiffrence: 'Différence',
		changeCreate: 'Cet élément n\'existe pas dans Umbraco et est en cours de création',
		noChangesImport: 'Aucune modification n\'a été apportée à cet élément',
		noChangesReport: 'Aucune modification détectée',
		noChangesDelete: 'Cet élément a été supprimé d\'Umbraco',

		importHeader: 'Importer depuis un fichier',

		success: 'Succès',
		change: 'Modification',
		changeType: 'Type',
		changeName: 'Nom',
		changeDetail: 'Détail',
		changeHeading: 'Résultats',
		changeCount: '{1}/{0} modifications',
		noChangeCount: '0/{0} modifications',

		legacyInfo: `<p>uSync a trouvé un dossier uSync hérité à <strong>%0%</strong>.<br />
						Il est probable que son contenu devra être converti d'une façon ou d'une autre</p>`,

		legacyObsolete: `<h4>Types de données obsolètes</h4>
				<ul>%0%</ul>
				<p>Vous pouvez convertir ces types de données en utilisant
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br />
					<em>(Dans la version complète d'uSync, la conversion se fera ici.)</em></p>`,

		legacyCopy: `<h4>Copier vers uSync/v14</h4>
					<p>Vous pouvez copier votre dossier %0% vers le dossier ~/uSync/v14<br />et exécuter une importation.</p>
					<p>Si rien ne nécessite de conversion, tout devrait s'importer correctement.</p>
					<p><strong>Supprimez ou renommez le dossier %0% pour éviter cette popup</strong></p>`,

		hmacMismatch: `<h4>Incompatibilité HMAC <uui-icon name="alert"></uui-icon></h4>
			<p>Il semble que le paramètre <code>Imaging:HMAC</code> utilisé pour générer les fichiers dans le dossier uSync ne corresponde pas au paramètre actuel de ce site.</p>
			<p>Les images dans les contrôles RTE auront la valeur HMAC ajoutée à l'URL, et sans configuration supplémentaire, ces images pourraient ne pas s'afficher correctement.</p>
			<ul><li>Vous pouvez activer le "mappage HMAC" dans uSync,</li>
			<li>ou vous pouvez vous assurer que la valeur HMAC dans le paramètre <code>Imaging:HMAC</code> correspond à la valeur utilisée pour générer les fichiers dans le dossier uSync</li></ul>`,
		formatMismatch:
			'La version du format du fichier de synchronisation ne correspond pas à la version attendue. Cela peut indiquer un problème de compatibilité potentiel.',

		legacyBanner:
			'Ce site contient des fichiers d\'une version précédente d\'uSync. Consultez les détails dans l\'onglet Héritage.',

		legacyCopyTitle: 'Écraser les fichiers v%0%',
		legacyCopyContent:
			'Êtes-vous sûr de vouloir écraser le contenu du dossier %0% avec les fichiers du dossier uSync hérité ?',

		legacyIgnoreTitle: 'Ignorer les fichiers hérités',
		legacyIgnoreContent:
			'Êtes-vous sûr de vouloir ignorer les fichiers dans le dossier uSync hérité ?',

		errorHeader:
			'Cet élément a rencontré une erreur lors du processus. Les détails sont ci-dessous :',

		uploadIntro: 'Sélectionnez un fichier zip contenant les fichiers uSync que vous souhaitez télécharger',
		uploadSuccess: 'Les fichiers ont été téléchargés et extraits dans le dossier uSync',
		uploadError: 'Une erreur s\'est produite lors du téléchargement des fichiers',

		ILanguage: 'Langue',
		IDictionaryItem: 'Éléments du dictionnaire',
		IDataType: 'Types de données',
		ITemplate: 'Modèles',
		IContentType: 'Types de contenu',
		IMediaType: 'Types de médias',
		IMemberType: 'Types de membres',
		IContent: 'Contenu',
		IMedia: 'Média',
		IDomain: 'Domaines',
		IWebhook: 'Webhooks',
		IRelationType: 'Types de relations',
		MediaFile: 'Fichiers médias',
		XElement: 'Autre',
		LanguageHandler: 'Langues',
		DictionaryHandler: 'Éléments du dictionnaire',
		DataTypeHandler: 'Types de données',
		TemplateHandler: 'Modèles',
		ContentTypeHandler: 'Types de contenu',
		MediaTypeHandler: 'Types de médias',
		MemberTypeHandler: 'Types de membres',
		ContentHandler: 'Contenu',
		MediaHandler: 'Média',
		RelationTypeHandler: 'Types de relations',
		EntityContainer: 'Conteneurs',
	},
	USyncSettings: {
		settings: 'Paramètres uSync',
		filesAndFolders: 'Fichiers et dossiers',
		handlerDefaults: 'Paramètres par défaut des gestionnaires',

		processingMode: 'Mode de traitement',
		processingModeDesc:
			'Comment le processus uSync s\'exécute, soit en arrière-plan, soit de manière interactive (Normal)',

		importAtStartup: 'Importer au démarrage',
		importAtStartupDesc: 'Exécuter une importation de fichiers depuis le disque au démarrage d\'Umbraco',

		exportAtStartup: 'Exporter au démarrage',
		exportAtStartupDesc: 'Exporter les paramètres Umbraco au démarrage du site',

		exportOnSave: 'Exporter à l\'enregistrement',
		exportOnSaveDesc: 'Générer des fichiers uSync lors de l\'enregistrement des éléments',

		uiEnabledGroups: 'Groupes activés dans l\'interface',
		uiEnabledGroupsDesc: 'Groupes de gestionnaires visibles/utilisables sur le tableau de bord',

		failOnMissingParent: 'Échec si parent manquant',
		failOnMissingParentDesc: 'Échec si l\'élément parent est manquant',

		currentHandlerSet: 'Ensemble actuel',

		handlerSet: 'Ensemble de gestionnaires par défaut',
		handlerSetDesc: 'L\'ensemble de gestionnaires par défaut à utiliser pour le site',

		flatStructure: 'Structure plate',
		flatStructureDesc: 'Tous les éléments d\'un type sont stockés dans une structure de dossiers plate',

		guidNames: 'Utiliser les GUID comme noms de fichiers',
		guidNamesDesc: 'Utiliser le GUID d\'un élément comme nom de fichier',

		handlerGroups: 'Groupes de gestionnaires',
		handlerGroupsDesc: 'Groupes pour limiter l\'ensemble de gestionnaires',

		disabledHandlers: 'Gestionnaires désactivés',
		disabledHandlersDesc: 'Gestionnaires explicitement désactivés pour cet ensemble de gestionnaires',

		folders: 'Dossiers',
		foldersDesc:
			'Dossiers dans lesquels uSync recherchera des fichiers (les éléments sont normalement enregistrés dans le dernier dossier de la liste)',

		rootSite: 'Site racine',
		rootSiteDesc: 'Ce site est-il une racine pour d\'autres sites.',

		rootLocked: 'Racine verrouillée',
		rootLockedDesc: 'Les modifications des éléments provenant du site racine sont-elles verrouillées ?',

		help: 'Les paramètres sont contrôlés via le fichier appsettings.json. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">Voir notre documentation</a>',

		bootSettings: 'Paramètres du premier démarrage 🥾',

		firstBoot: 'Importer au premier démarrage',
		firstBootDesc: 'Exécuter le processus d\'importation au premier démarrage du site',

		firstBootGroup: 'Groupes du premier démarrage',
		firstBootGroupDesc: 'Les groupes à exécuter au premier démarrage',
	},
};
