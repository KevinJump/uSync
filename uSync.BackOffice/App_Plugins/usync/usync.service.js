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

    function uSyncServiceController($http) {

        var serviceRoot = Umbraco.Sys.ServerVariables.uSync.uSyncService;

        var service = {
            getSettings: getSettings,
            getHandlers: getHandlers,
            getHandlerSetSettings: getHandlerSetSettings,

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

            checkVersion: checkVersion
        };

        return service;

        /////////////////////

        function getSettings() {
            return $http.get(serviceRoot + 'GetSettings');
        }

        function getHandlerSetSettings(set) {
            return $http.get(serviceRoot + 'GetHandlerSetSettings?id=' + set);
        }

        function getHandlers() {
            return $http.get(serviceRoot + 'GetHandlers');
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

        function importItems(force, group, clientId) {
            return $http.put(serviceRoot + 'import',
                {
                    force: force,
                    group: group,
                    clientId: clientId
                });
        }

        function getSyncWarnings(action, group) {
            return $http.post(serviceRoot + 'GetSyncWarnings?action=' + action, { group: group });
        }
        

        function importItem(item) {
            return $http.put(serviceRoot + 'importItem', item);
        }

        function saveSettings(settings) {
            return $http.post(serviceRoot + 'savesettings', settings);
        }

        function getHandlerGroups() {
            return $http.get(serviceRoot + 'GetHandlerGroups');
        }

        function checkVersion() {
            return $http.get(serviceRoot + 'CheckVersion');
        }


        function getActionHandlers(options) {
            return $http.post(serviceRoot + 'GetActionHandlers?action=' + options.action,
                {
                    group: options.group
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
                force: options.force
            });
        }

        function importPost(actions, options, clientId) {
            return $http.post(serviceRoot + 'ImportPost', {
                actions: actions,
                clientId: clientId
            });
        }

        function exportHandler(handler, options, clientId) {
            return $http.post(serviceRoot + 'ExportHandler', {
                handler: handler,
                clientId: clientId
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
    
    }

    angular.module('umbraco.services')
        .factory('uSync8DashboardService', uSyncServiceController);

})();