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

            report: report,
            exportItems: exportItems,
            importItems: importItems,
            importItem: importItem,
            saveSettings: saveSettings,

            getLoadedHandlers: getLoadedHandlers,
            getAddOns: getAddOns,
            getAddOnSplash: getAddOnSplash,

            getHandlerGroups: getHandlerGroups,

            checkVersion: checkVersion
        };

        return service;

        /////////////////////

        function getSettings() {
            return $http.get(serviceRoot + 'GetSettings');
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

        function exportItems (clientId) {
            return $http.post(serviceRoot + 'export', { clientId: clientId });
        }

        function importItems(force, group, clientId) {
            return $http.put(serviceRoot + 'import',
                {
                    force: force,
                    group: group,
                    clientId: clientId
                });
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
    }

    angular.module('umbraco.services')
        .factory('uSync8DashboardService', uSyncServiceController);

})();