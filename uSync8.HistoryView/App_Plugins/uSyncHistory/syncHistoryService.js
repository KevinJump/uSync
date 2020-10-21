(function () {
    'use strict';

    function historyService($http) {

        var serviceRoot = Umbraco.Sys.ServerVariables.uSync.historyService;

        return {
            getHistory: getHistory,
            clearHistory: clearHistory
        };

        //////////////

        function getHistory() {
            return $http.get(serviceRoot + 'GetHistory');
        }

        function clearHistory() {
            return $http.post(serviceRoot + "ClearHistory");
        }
    }

    angular.module('umbraco')
        .factory('uSyncHistoryService', historyService);

})();