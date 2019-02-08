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

        var serviceRoot = 'backoffice/uSync/uSyncDashboardApi/';

        var service = {
            getSettings: getSettings,
            getHandlers: getHandlers,

            report: report,
            exportItems: exportItems,
            importItems: importItems,
            saveSettings: saveSettings,
            getLoadedHandlers: getLoadedHandlers,
            getAddOnString: getAddOnString
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

        function getAddOnString() {
            return $http.get(serviceRoot + 'GetAddOnString');
        }

        function report(clientId) {
            return $http.post(serviceRoot + 'report', { clientId: clientId });
        }

        function exportItems (clientId) {
            return $http.post(serviceRoot + 'export', { clientId: clientId });
        }

        function importItems(force, clientId) {
            return $http.put(serviceRoot + 'import', { force: force, clientId: clientId });
        }

        function saveSettings(settings) {
            return $http.post(serviceRoot + 'savesettings', settings);
        }
    }

    angular.module('umbraco.services')
        .factory('uSync8DashboardService', uSyncServiceController);

})();