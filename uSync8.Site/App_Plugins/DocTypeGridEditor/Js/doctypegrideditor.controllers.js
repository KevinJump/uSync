angular.module("umbraco").controller("Our.Umbraco.DocTypeGridEditor.GridEditors.DocTypeGridEditor", [

    "$scope",
    "$rootScope",
    "$timeout",
    "editorState",
    'assetsService',
    "Our.Umbraco.DocTypeGridEditor.Resources.DocTypeGridEditorResources",
    "umbRequestHelper",
    "localizationService",
    "editorService",

    function ($scope, $rootScope, $timeout, editorState, assetsService, dtgeResources, umbRequestHelper, localizationService, editorService) {

        var overlayOptions = {
            view: umbRequestHelper.convertVirtualToAbsolutePath(
                "~/App_Plugins/DocTypeGridEditor/Views/doctypegrideditor.dialog.html"),
            model: {},
            titles: {
                insertItem: "Click to insert item",
                editItem: "Edit item",
                selectContentType: "Choose a Content Type",
                selectBlueprint: "Choose a Content Template"
            },
            title: "Edit item",
            submitButtonlabelKey: "bulk_done"
        };

        $scope.icon = "icon-item-arrangement";

        // init cached content types if it doesnt exist.
        if (!$rootScope.dtgeContentTypes) $rootScope.dtgeContentTypes = {};

        // localize strings
        localizationService.localizeMany(["docTypeGridEditor_insertItem", "docTypeGridEditor_editItem", "docTypeGridEditor_selectContentType", "blueprints_selectBlueprint"]).then(function (data) {
            overlayOptions.titles.insertItem = data[0];
            overlayOptions.titles.editItem = data[1];
            overlayOptions.titles.selectContentType = data[2];
            overlayOptions.titles.selectBlueprint = data[3];
        });

        $scope.setValue = function (data, callback) {
            $scope.title = $scope.control.editor.name;
            $scope.icon = $scope.control.editor.icon;
            $scope.control.value = data;

            if (!("id" in $scope.control.value) || $scope.control.value.id == "") {
                $scope.control.value.id = guid();
            }
            if ("name" in $scope.control.value.value && $scope.control.value.value.name) {
                $scope.title = $scope.control.value.value.name;
            }
            if ("dtgeContentTypeAlias" in $scope.control.value && $scope.control.value.dtgeContentTypeAlias) {
                if (!$rootScope.dtgeContentTypes[$scope.control.value.dtgeContentTypeAlias]) {

                    dtgeResources.getContentType($scope.control.value.dtgeContentTypeAlias).then(function (data2) {
                        var contentType = {
                            title: data2.title,
                            description: data2.description,
                            icon: data2.icon
                        };

                        // save to cached content types
                        $rootScope.dtgeContentTypes[$scope.control.value.dtgeContentTypeAlias] = contentType;
                        $scope.setTitleAndDescription(contentType);
                    });
                }
                else {
                    $scope.setTitleAndDescription($rootScope.dtgeContentTypes[$scope.control.value.dtgeContentTypeAlias]);
                }
            }
            if (callback)
                callback();
        };

        $scope.setTitleAndDescription = function (contentType) {
            $scope.title = contentType.title;
            $scope.description = contentType.description;
            $scope.icon = contentType.icon;
        };

        $scope.setDocType = function () {

            overlayOptions.editorName = $scope.control.editor.name;
            overlayOptions.allowedDocTypes = $scope.control.editor.config.allowedDocTypes || [];
            overlayOptions.showDocTypeSelectAsGrid = $scope.control.editor.config.showDocTypeSelectAsGrid === true;
            overlayOptions.nameTemplate = $scope.control.editor.config.nameTemplate;
            overlayOptions.size = $scope.control.editor.config.largeDialog ? null : "small";

            overlayOptions.dialogData = {
                docTypeAlias: $scope.control.value.dtgeContentTypeAlias,
                value: $scope.control.value.value,
                id: $scope.control.value.id
            };
            overlayOptions.close = function () {
                editorService.close();
            }
            overlayOptions.submit = function (newModel) {

                // Copy property values to scope model value
                if (newModel.node) {
                    var value = {
                        name: newModel.editorName
                    };

                    for (var v = 0; v < newModel.node.variants.length; v++) {
                        var variant = newModel.node.variants[v];
                        for (var t = 0; t < variant.tabs.length; t++) {
                            var tab = variant.tabs[t];
                            for (var p = 0; p < tab.properties.length; p++) {
                                var prop = tab.properties[p];
                                if (typeof prop.value !== "function") {
                                    value[prop.alias] = prop.value;
                                }
                            }
                        }
                    }

                    if (newModel.nameExp) {
                        var newName = newModel.nameExp(value); // Run it against the stored dictionary value, NOT the node object
                        if (newName && (newName = $.trim(newName))) {
                            value.name = newName;
                        }
                    }

                    newModel.dialogData.value = value;
                } else {
                    newModel.dialogData.value = null;

                }

                $scope.setValue({
                    dtgeContentTypeAlias: newModel.dialogData.docTypeAlias,
                    value: newModel.dialogData.value,
                    id: newModel.dialogData.id
                });
                $scope.setPreview($scope.control.value);
                editorService.close();
            };

            editorService.open(overlayOptions);
        };

        $scope.setPreview = function (model) {
            if ($scope.control.editor.config && "enablePreview" in $scope.control.editor.config && $scope.control.editor.config.enablePreview) {
                dtgeResources.getEditorMarkupForDocTypePartial(editorState.current.id, model.id,
                    $scope.control.editor.alias, model.dtgeContentTypeAlias, model.value,
                    $scope.control.editor.config.viewPath,
                    $scope.control.editor.config.previewViewPath,
                    !!editorState.current.publishDate)
                    .then(function (response) {
                        var htmlResult = response.data;
                        if (htmlResult.trim().length > 0) {
                            $scope.preview = htmlResult;
                        }
                    });
            }
        };

        function init() {
            $timeout(function () {
                if ($scope.control.$initializing) {
                    $scope.setDocType();
                } else if ($scope.control.value) {
                    $scope.setPreview($scope.control.value);
                }
            }, 200);
        }

        if ($scope.control.value) {
            if (!$scope.control.value.dtgeContentTypeAlias && $scope.control.value.docType) {
                $scope.control.value.dtgeContentTypeAlias = $scope.control.value.docType;
            }
            if ($scope.control.value.docType) {
                delete $scope.control.value.docType;
            }
            if (isGuid($scope.control.value.dtgeContentTypeAlias)) {
                dtgeResources.getContentTypeAliasByGuid($scope.control.value.dtgeContentTypeAlias).then(function (data1) {
                    $scope.control.value.dtgeContentTypeAlias = data1.alias;
                    $scope.setValue($scope.control.value, init);
                });
            } else {
                $scope.setValue($scope.control.value, init);
            }
        } else {
            $scope.setValue({
                id: guid(),
                dtgeContentTypeAlias: "",
                value: {}
            }, init);
        }

        // Load preview css / js files
        if ($scope.control.editor.config && "enablePreview" in $scope.control.editor.config && $scope.control.editor.config.enablePreview) {
            if ("previewCssFilePath" in $scope.control.editor.config && $scope.control.editor.config.previewCssFilePath) {
                assetsService.loadCss($scope.control.editor.config.previewCssFilePath, $scope);
            };

            if ("previewJsFilePath" in $scope.control.editor.config && $scope.control.editor.config.previewJsFilePath) {
                assetsService.loadJs($scope.control.editor.config.previewJsFilePath, $scope);
            }
        }

        function guid() {
            function s4() {
                return Math.floor((1 + Math.random()) * 0x10000)
                    .toString(16)
                    .substring(1);
            }
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
                s4() + '-' + s4() + s4() + s4();
        }

        function isGuid(input) {
            return new RegExp("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$", "i").test(input.toString());
        }

    }
]);

angular.module("umbraco").controller("Our.Umbraco.DocTypeGridEditor.Dialogs.DocTypeGridEditorDialog",
    [
        "$scope",
        "$interpolate",
        "formHelper",
        "contentResource",
        "Our.Umbraco.DocTypeGridEditor.Resources.DocTypeGridEditorResources",
        "Our.Umbraco.DocTypeGridEditor.Services.DocTypeGridEditorUtilityService",
        "blueprintConfig",

        function ($scope, $interpolate, formHelper, contentResource, dtgeResources, dtgeUtilityService, blueprintConfig) {

            var vm = this;
            vm.submit = submit;
            vm.close = close;
            vm.loading = true;
            vm.blueprintConfig = blueprintConfig;

            function submit() {
                if ($scope.model.submit) {
                    $scope.$broadcast('formSubmitting', { scope: $scope });
                    $scope.model.submit($scope.model);
                }
            }
            function close() {
                if ($scope.model.close) {
                    $scope.model.close();
                }
            }

            $scope.docTypes = [];
            $scope.dialogMode = null;
            $scope.selectedDocType = null;
            $scope.model.node = null;

            var nameExp = !!$scope.model.nameTemplate
                ? $interpolate($scope.model.nameTemplate)
                : undefined;

            $scope.model.nameExp = nameExp;

            function createBlank() {
                $scope.dialogMode = "edit";
                loadNode();
            };

            function createOrSelectBlueprintIfAny(docType) {

                $scope.model.dialogData.docTypeAlias = docType.alias;
                var blueprintIds = _.keys(docType.blueprints || {});
                $scope.selectedDocType = docType;

                if (blueprintIds.length) {
                    if (blueprintConfig.skipSelect) {
                        createFromBlueprint(blueprintIds[0]);
                    } else {
                        $scope.dialogMode = "selectBlueprint";
                    }
                } else {
                    createBlank();
                }
            };

            function createFromBlueprint(blueprintId) {
                contentResource.getBlueprintScaffold(-20, blueprintId).then(function (data) {
                    // Assign the model to scope
                    $scope.nodeContext = $scope.model.node = data;
                    $scope.dialogMode = "edit";
                    vm.content = $scope.nodeContext.variants[0];
                    vm.loading = false;
                });
            };

            $scope.createBlank = createBlank;
            $scope.createOrSelectBlueprintIfAny = createOrSelectBlueprintIfAny;
            $scope.createFromBlueprint = createFromBlueprint;

            function loadNode() {
                vm.loading = true;
                contentResource.getScaffold(-20, $scope.model.dialogData.docTypeAlias).then(function (data) {

                    // Merge current value
                    if ($scope.model.dialogData.value) {
                        for (var v = 0; v < data.variants.length; v++) {
                            var variant = data.variants[v];
                            for (var t = 0; t < variant.tabs.length; t++) {
                                var tab = variant.tabs[t];
                                for (var p = 0; p < tab.properties.length; p++) {
                                    var prop = tab.properties[p];
                                    if ($scope.model.dialogData.value[prop.alias]) {
                                        prop.value = $scope.model.dialogData.value[prop.alias];
                                    }
                                }
                            }
                        }
                    };

                    // Assign the model to scope
                    $scope.nodeContext = $scope.model.node = data;
                    vm.content = $scope.nodeContext.variants[0];
                    vm.loading = false;
                });
            }

            if ($scope.model.dialogData.docTypeAlias) {
                $scope.dialogMode = "edit";
                loadNode();
            } else {
                $scope.dialogMode = "selectDocType";
                // No data type, so load a list to choose from
                dtgeResources.getContentTypes($scope.model.allowedDocTypes).then(function (docTypes) {
                    $scope.docTypes = docTypes;
                    if ($scope.docTypes.length == 1) {
                        createOrSelectBlueprintIfAny($scope.docTypes[0]);
                    }
                    else {
                        vm.loading = false;
                    }
                });
            }

        }

    ]);
