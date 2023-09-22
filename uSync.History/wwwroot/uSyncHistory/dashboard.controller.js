(function () {
    'use strict';

    function historyController($http, editorService) {
        var vm = this;
        vm.loadHistory = loadHistory;
        vm.actions = {};
        vm.clearHistory = clearHistory;

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

        function clearHistory() {
            $http.get("/umbraco/backoffice/usync/usynchistory/ClearHistory")
                .then(function (result) {
                    console.log(result.data);
                    getHistoryFiles();
                });
        }

        function loadHistory(item) {
            var niceDate = moment(item.date).format("LLLL");
            var options = {
                title: "History - "+item.method,
                description: item.username+" on "+niceDate,
                view: "/App_Plugins/uSyncHistory/history.html",
                actions: item.actions,
                submit: function (model) {
                    editorService.close();
                },
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);

            //console.log(item);
            //item.selected = true;
            //vm.historyFile = [];
            //// call the thing - get the contents of a history file. 
            //$http.get("/umbraco/backoffice/usync/usynchistory/LoadHistory?filePath="+item.filePath)
            //    .then(function (result) {
            //        console.log(result.data);
            //        vm.historyFile = result.data;
            //  });
        }
    }

    angular.module('umbraco')
        .controller('uSyncHistoryController', historyController);
})();