angular.module('umbraco.resources').factory('Our.Umbraco.DocTypeGridEditor.Resources.DocTypeGridEditorResources',
    function ($q, $http, umbRequestHelper) {
        return {
            getContentTypeAliasByGuid: function (guid) {
                var url = umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/DocTypeGridEditorApi/DocTypeGridEditorApi/GetContentTypeAliasByGuid?guid=" + guid);
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve content type alias by guid'
                );
            },
            getContentTypes: function (allowedContentTypes) {
                var url = umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/DocTypeGridEditorApi/DocTypeGridEditorApi/GetContentTypes");
                if (allowedContentTypes) {
                    for (var i = 0; i < allowedContentTypes.length; i++) {
                        url += (i == 0 ? "?" : "&") + "allowedContentTypes=" + allowedContentTypes[i];
                    }
                }
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve content types'
                );
            },
            getContentType: function (contentTypeAlias) {
                var url = umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/DocTypeGridEditorApi/DocTypeGridEditorApi/GetContentType?contentTypeAlias=" + contentTypeAlias);
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve content type icon'
                );
            },
            getDataTypePreValues: function (dtdId) {
                var url = umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/DocTypeGridEditorApi/DocTypeGridEditorApi/GetDataTypePreValues?dtdid=" + dtdId);
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve datatypes'
                );
            },
            getEditorMarkupForDocTypePartial: function (pageId, id, editorAlias, contentTypeAlias, value, viewPath, previewViewPath, published) {
                var url = umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/DocTypeGridEditorApi/DocTypeGridEditorApi/GetPreviewMarkup?dtgePreview=1&pageId=" + pageId);
                return $http({
                    method: 'POST',
                    url: url,
                    data: $.param({
                        id: id,
                        editorAlias: editorAlias,
                        contentTypeAlias: contentTypeAlias,
                        value: JSON.stringify(value),
                        viewPath: viewPath,
                        previewViewPath: previewViewPath
                    }),
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    }
                });
            }
        };
    });