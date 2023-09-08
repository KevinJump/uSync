(function () {
    'use strict';

    function historyController($http) {
        var vm = this;
        vm.loadHistory = loadHistory;
        vm.actions = {};

        vm.$onInit = function () {
            getHistoryFiles();
        }
        function getHistoryFiles() {
            $http.get("/umbraco/backoffice/usync/usynchistory/GetHistory")
                .then(function (result) {
                    console.log(result.data);
                    vm.history = result.data;
                });
        }

        function loadHistory(filePath) {
            // call the thing - get the contents of a history file. 
            $http.get("/umbraco/backoffice/usync/usynchistory/LoadHistory?filePath="+filePath)
                .then(function (result) {
                    console.log(result.data);
                    vm.historyFile = result.data;
                });
        }
    }

    angular.module('umbraco')
        .controller('uSyncHistoryController', historyController);
})();