(function () {
    'use strict';

    function historyService($http) {

        var serviceRoot = Umbraco.Sys.ServerVariables.uSync.historyService;

        return {
            getHistory: getHistory
        };

        //////////////

        function getHistory() {
            return $http.get(serviceRoot + 'GetHistory');
        }
    }

    angular.module('umbraco')
        .factory('uSyncHistoryService', historyService);

})();