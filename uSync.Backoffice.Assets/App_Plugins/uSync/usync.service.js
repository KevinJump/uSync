/**
 * @ngdoc
 * @name uSync8Service
 * @requires $http
 * 
 * @description provides the link to the uSync api elements
 *              required for the dashboard to function
 */

(function () {
    'use strict';

    function uSyncServiceController($http, $q) {

        var serviceRoot = Umbraco.Sys.ServerVariables.uSync.uSyncService;

        var service = {
            getSettings: getSettings,
            getChangedSettings: getChangedSettings,
            getHandlers: getHandlers,
            getHandlerSetSettings: getHandlerSetSettings,

            getDefaultSet: getDefaultSet,
            getSets: getSets,
            getSelectableSets: getSelectableSets,

            report: report,
            exportItems: exportItems,
            importItems: importItems,
            importItem: importItem,
            saveSettings: saveSettings,

            getActionHandlers: getActionHandlers,
            reportHandler: reportHandler,
            importHandler: importHandler,
            importPost: importPost,
            exportHandler: exportHandler,
            cleanExport: cleanExport,

            startProcess: startProcess,
            finishProcess: finishProcess,

            getLoadedHandlers: getLoadedHandlers,
            getAddOns: getAddOns,
            getAddOnSplash: getAddOnSplash,

            getHandlerGroups: getHandlerGroups,

            getSyncWarnings: getSyncWarnings,

            checkVersion: checkVersion,

            downloadExport: downloadExport

        };

        return service;

        /////////////////////

        function getSettings() {
            return $http.get(serviceRoot + 'GetSettings');
        }

        function getDefaultSet() {
            return $http.get(serviceRoot + 'GetDefaultSet');
        }

        function getSets() {
            return $http.get(serviceRoot + 'GetSets');
        }

        function getSelectableSets() {
            return $http.get(serviceRoot + 'GetSelectableSets');
        }

        function getChangedSettings() {
            return $http.get(serviceRoot + 'GetChangedSettings');
        }

        function getHandlerSetSettings(set) {
            return $http.get(serviceRoot + 'GetHandlerSetSettings?id=' + set);
        }

        function getHandlers(set) {
            return $http.get(serviceRoot + 'GetHandlers?set=' + set);
        }

        function getLoadedHandlers() {
            return $http.get(serviceRoot + 'GetLoadedHandlers');
        }

        function getAddOns() {
            return $http.get(serviceRoot + 'GetAddOns');
        }

        function getAddOnSplash() {
            return $http.get(serviceRoot + 'GetAddOnSplash');
        }


        function report(group, clientId) {
            return $http.post(serviceRoot + 'report', { clientId: clientId, group: group });
        }

        function exportItems (clientId, clean) {
            return $http.post(serviceRoot + 'export', { clientId: clientId, clean: clean });
        }

        function importItems(force, set, group, clientId) {
            return $http.put(serviceRoot + 'import',
                {
                    force: force,
                    set: set,
                    group: group,
                    clientId: clientId,
                });
        }

        function getSyncWarnings(action, group) {
            return $http.post(serviceRoot + 'GetSyncWarnings?action=' + action, {
                group: group
            });
        }
        

        function importItem(item) {
            return $http.put(serviceRoot + 'importItem', item);
        }

        function saveSettings(settings) {
            return $http.post(serviceRoot + 'savesettings', settings);
        }

        function getHandlerGroups(set) {
            return $http.get(serviceRoot + 'GetHandlerGroups?set=' + set);
        }

        function checkVersion() {
            return $http.get(serviceRoot + 'CheckVersion');
        }


        function getActionHandlers(options) {
            return $http.post(serviceRoot + 'GetActionHandlers?action=' + options.action,
                {
                    group: options.group,
                    set: options.set
                });
        }

        function reportHandler(handler, options, clientId) {
            return $http.post(serviceRoot + 'ReportHandler', {
                handler: handler,
                clientId: clientId
            });
        }

        function importHandler(handler, options, clientId) {
            return $http.post(serviceRoot + 'ImportHandler', {
                handler: handler,
                clientId: clientId,
                force: options.force,
                set: options.set
            });
        }

        function importPost(actions, options, clientId) {
            return $http.post(serviceRoot + 'ImportPost', {
                actions: actions,
                clientId: clientId,
                set: options.set
            });
        }

        function exportHandler(handler, options, clientId) {
            return $http.post(serviceRoot + 'ExportHandler', {
                handler: handler,
                clientId: clientId,
                set: options.set
            });
        }

        function startProcess(action) {
            return $http.post(serviceRoot + 'StartProcess?action=' + action);
        }

        function finishProcess(action, actions) {
            return $http.post(serviceRoot + 'FinishProcess?action=' + action, actions);
        }

        function cleanExport() {
            return $http.post(serviceRoot + 'cleanExport');
        }

        function downloadExport() {
            return downloadPost(serviceRoot + 'downloadExport');
        }



        /*
         * Downloads a file to the client using AJAX/XHR
         * Based on an implementation here: web.student.tuwien.ac.at/~e0427417/jsdownload.html
         * See https://stackoverflow.com/a/24129082/694494
         */
        function downloadPost(httpPath, payload) {

            // Use an arraybuffer
            return $http.post(httpPath, payload, { responseType: 'arraybuffer' })
                .then(function (response) {

                    var octetStreamMime = 'application/octet-stream';
                    var success = false;

                    // Get the headers
                    var headers = response.headers();

                    // Get the filename from the header or default to "download.bin"
                    var filename = getFileName(headers);

                    // Determine the content type from the header or default to "application/octet-stream"
                    var contentType = headers['content-type'] || octetStreamMime;

                    try {
                        // Try using msSaveBlob if supported
                        let blob = new Blob([response.data], { type: contentType });
                        if (navigator.msSaveBlob)
                            navigator.msSaveBlob(blob, filename);
                        else {
                            // Try using other saveBlob implementations, if available
                            var saveBlob = navigator.webkitSaveBlob || navigator.mozSaveBlob || navigator.saveBlob;
                            if (saveBlob === undefined) throw "Not supported";
                            saveBlob(blob, filename);
                        }
                        success = true;
                    } catch (ex) {
                        console.log("saveBlob method failed with the following exception:");
                        console.log(ex);
                    }

                    if (!success) {
                        // Get the blob url creator
                        var urlCreator = window.URL || window.webkitURL || window.mozURL || window.msURL;
                        if (urlCreator) {
                            // Try to use a download link
                            var link = document.createElement('a');
                            if ('download' in link) {
                                // Try to simulate a click
                                try {
                                    // Prepare a blob URL
                                    let blob = new Blob([response.data], { type: contentType });
                                    let url = urlCreator.createObjectURL(blob);
                                    link.setAttribute('href', url);

                                    // Set the download attribute (Supported in Chrome 14+ / Firefox 20+)
                                    link.setAttribute("download", filename);

                                    // Simulate clicking the download link
                                    var event = document.createEvent('MouseEvents');
                                    event.initMouseEvent('click', true, true, window, 1, 0, 0, 0, 0, false, false, false, false, 0, null);
                                    link.dispatchEvent(event);
                                    success = true;

                                } catch (ex) {
                                    console.log("Download link method with simulated click failed with the following exception:");
                                    console.log(ex);
                                }
                            }

                            if (!success) {
                                // Fallback to window.location method
                                try {
                                    // Prepare a blob URL
                                    // Use application/octet-stream when using window.location to force download
                                    let blob = new Blob([response.data], { type: octetStreamMime });
                                    let url = urlCreator.createObjectURL(blob);
                                    window.location = url;
                                    success = true;
                                } catch (ex) {
                                    console.log("Download link method with window.location failed with the following exception:");
                                    console.log(ex);
                                }
                            }

                        }
                    }

                    if (!success) {
                        // Fallback to window.open method
                        window.open(httpPath, '_blank', '');
                    }

                    return $q.resolve();

                }, function (response) {

                    return $q.reject({
                        errorMsg: "An error occurred downloading the file",
                        data: response.data,
                        status: response.status
                    });
                });
        }


        function getFileName(headers) {
            var disposition = headers["content-disposition"];
            if (disposition && disposition.indexOf('attachment') !== -1) {
                var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                var matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) {
                    return matches[1].replace(/['"]/g, '');
                }
            }

            return "usync_export.zip";
        }

    }

    angular.module('umbraco.services')
        .factory('uSync8DashboardService', uSyncServiceController);

})();