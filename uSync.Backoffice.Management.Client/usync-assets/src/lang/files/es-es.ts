export default {
	uSync: {
		section: 'Sincronización',
		name: 'uSync',
		banner: 'uSync para todo',

		Report: 'Informe',
		Import: 'Importar',
		Export: 'Exportar',
		ImportForce: 'Importar (Forzar)',
		ExportClean: 'Exportar (Limpio)',
		ExportFile: 'Exportar a archivo',
		ImportFile: 'Importar desde archivo',

		noChange: 'Nada ha cambiado',
		showAll: 'Mostrar todos los elementos',

		detailHeadline: 'Cambios detectados',
		detailHeader: 'Cosas que son diferentes',

		runningInBackground:
			'uSync está ejecutando este proceso en segundo plano. Si navega fuera de esta página, continuará ejecutándose.',

		connectionLost:
			'Se ha perdido la conexión con el servidor. El proceso continuará ejecutándose en segundo plano, pero no verá actualizaciones aquí.',

		importSingle: 'Importar',
		importSingleWarning:
			'Esto importará este elemento en Umbraco. Si este elemento tiene dependencias, no serán importadas y deberán resolverse manualmente.',
		importSingleSuccess: 'El elemento se ha importado correctamente',
		importSingleFailed: `<p>Se produjo un error al importar el elemento</p><strong>%0%</strong>`,

		changeAction: 'Acción',
		changeItem: 'Elemento',
		changeDiffrence: 'Diferencia',
		changeCreate: 'Este elemento no existe en Umbraco y está siendo creado',
		noChangesImport: 'No se realizaron cambios en este elemento',
		noChangesReport: 'No se detectaron cambios',
		noChangesDelete: 'Este elemento ha sido eliminado de Umbraco',

		importHeader: 'Importar desde archivo',

		success: 'Éxito',
		change: 'Cambio',
		changeType: 'Tipo',
		changeName: 'Nombre',
		changeDetail: 'Detalle',
		changeHeading: 'Resultados',
		changeCount: '{1}/{0} cambios',
		noChangeCount: '0/{0} cambios',

		legacyInfo: `<p>uSync ha encontrado una carpeta uSync heredada en <strong>%0%</strong>.<br />
						Es probable que su contenido necesite convertirse de alguna manera</p>`,

		legacyObsolete: `<h4>Tipos de datos obsoletos</h4>
				<ul>%0%</ul>
				<p>Puede convertir estos tipos de datos usando
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br />
					<em>(En la versión completa de uSync, la conversión se realizará aquí.)</em></p>`,

		legacyCopy: `<h4>Copiar a uSync/v14</h4>
					<p>Puede copiar su carpeta %0% a la carpeta ~/uSync/v14<br />y ejecutar una importación.</p>
					<p>Si nada necesita convertirse, todo debería importarse correctamente.</p>
					<p><strong>Elimine o cambie el nombre de la carpeta %0% para evitar este popup</strong></p>`,

		hmacMismatch: `<h4>Discrepancia HMAC <uui-icon name="alert"></uui-icon></h4>
			<p>Parece que la configuración <code>Imaging:HMAC</code> utilizada para generar los archivos en la carpeta uSync no coincide con la configuración actual de este sitio.</p>
			<p>Las imágenes dentro de los controles RTE tendrán el valor HMAC añadido a la URL, y sin configuración adicional, estas imágenes pueden no mostrarse correctamente.</p>
			<ul><li>Puede activar el "mapeo HMAC" en uSync,</li>
			<li>o puede asegurarse de que el valor HMAC en la configuración <code>Imaging:HMAC</code> coincida con el valor utilizado para generar los archivos en la carpeta uSync</li></ul>`,
		formatMismatch:
			'La versión del formato del archivo de sincronización no coincide con la versión esperada. Esto puede indicar un posible problema de compatibilidad.',

		legacyBanner:
			'Este sitio contiene archivos de una versión anterior de uSync. Consulte los detalles en la pestaña Heredado.',

		legacyCopyTitle: 'Sobrescribir archivos v%0%',
		legacyCopyContent:
			'¿Está seguro de que desea sobrescribir el contenido de la carpeta %0% con los archivos de la carpeta uSync heredada?',

		legacyIgnoreTitle: 'Ignorar archivos heredados',
		legacyIgnoreContent:
			'¿Está seguro de que desea ignorar los archivos en la carpeta uSync heredada?',

		errorHeader:
			'Este elemento encontró un error durante el proceso. Los detalles se muestran a continuación:',

		uploadIntro: 'Seleccione un archivo zip que contenga los archivos uSync que desea cargar',
		uploadSuccess: 'Los archivos han sido cargados y extraídos en la carpeta uSync',
		uploadError: 'Se produjo un error al cargar los archivos',

		ILanguage: 'Idioma',
		IDictionaryItem: 'Elementos del diccionario',
		IDataType: 'Tipos de datos',
		ITemplate: 'Plantillas',
		IContentType: 'Tipos de contenido',
		IMediaType: 'Tipos de medios',
		IMemberType: 'Tipos de miembros',
		IContent: 'Contenido',
		IMedia: 'Medios',
		IDomain: 'Dominios',
		IWebhook: 'Webhooks',
		IRelationType: 'Tipos de relaciones',
		MediaFile: 'Archivos de medios',
		XElement: 'Otro',
		LanguageHandler: 'Idiomas',
		DictionaryHandler: 'Elementos del diccionario',
		DataTypeHandler: 'Tipos de datos',
		TemplateHandler: 'Plantillas',
		ContentTypeHandler: 'Tipos de contenido',
		MediaTypeHandler: 'Tipos de medios',
		MemberTypeHandler: 'Tipos de miembros',
		ContentHandler: 'Contenido',
		MediaHandler: 'Medios',
		RelationTypeHandler: 'Tipos de relaciones',
		EntityContainer: 'Contenedores',
	},
	USyncSettings: {
		settings: 'Configuración de uSync',
		filesAndFolders: 'Archivos y carpetas',
		handlerDefaults: 'Valores predeterminados del controlador',

		processingMode: 'Modo de procesamiento',
		processingModeDesc:
			'Cómo se ejecuta el proceso uSync, ya sea en segundo plano o de forma interactiva (Normal)',

		importAtStartup: 'Importar al inicio',
		importAtStartupDesc: 'Ejecutar una importación de archivos desde el disco cuando Umbraco se inicia',

		exportAtStartup: 'Exportar al inicio',
		exportAtStartupDesc: 'Exportar la configuración de Umbraco cuando el sitio se inicia',

		exportOnSave: 'Exportar al guardar',
		exportOnSaveDesc: 'Generar archivos uSync cuando se guardan elementos',

		uiEnabledGroups: 'Grupos habilitados en la interfaz',
		uiEnabledGroupsDesc: 'Grupos de controladores que se pueden ver/usar en el panel',

		failOnMissingParent: 'Error si falta el padre',
		failOnMissingParentDesc: 'Error si falta el elemento padre',

		currentHandlerSet: 'Conjunto actual',

		handlerSet: 'Conjunto de controladores predeterminado',
		handlerSetDesc: 'El conjunto de controladores predeterminado a usar para el sitio',

		flatStructure: 'Estructura plana',
		flatStructureDesc: 'Todos los elementos de un tipo se almacenan en una estructura de carpetas plana',

		guidNames: 'Usar GUID como nombres de archivo',
		guidNamesDesc: 'Usar el GUID de un elemento como nombre de archivo',

		handlerGroups: 'Grupos de controladores',
		handlerGroupsDesc: 'Grupos para limitar el conjunto de controladores',

		disabledHandlers: 'Controladores deshabilitados',
		disabledHandlersDesc: 'Controladores explícitamente deshabilitados para este conjunto de controladores',

		folders: 'Carpetas',
		foldersDesc:
			'Carpetas donde uSync buscará archivos (los elementos normalmente se guardan en la última carpeta de la lista)',

		rootSite: 'Sitio raíz',
		rootSiteDesc: 'Este sitio es una raíz para otros sitios.',

		rootLocked: 'Raíz bloqueada',
		rootLockedDesc: '¿Están bloqueados los cambios de los elementos que provienen del sitio raíz?',

		help: 'La configuración se controla mediante el archivo appsettings.json. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">Ver nuestra documentación</a>',

		bootSettings: 'Configuración del primer arranque 🥾',

		firstBoot: 'Importar en el primer arranque',
		firstBootDesc: 'Ejecutar el proceso de importación en el primer arranque del sitio',

		firstBootGroup: 'Grupos del primer arranque',
		firstBootGroupDesc: 'Los grupos a ejecutar en el primer arranque',
	},
};
