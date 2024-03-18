(function () {
    'use strict';

    function historyController($http, $scope, editorService, eventsService, localizationService, overlayService) {
        var vm = this;
        var evts = [];
        vm.loadHistory = loadHistory;
        vm.actions = {};
        vm.clearHistory = clearHistory;
        vm.enabled = Umbraco.Sys.ServerVariables.uSyncHistory.Enabled;

        vm.$onInit = function () {  
            evts.push(
            eventsService.on("usync-dashboard.tab.change", function (name, item) {
                if (item.alias == "uSyncHistory")
                {
                    getHistoryFiles();
                }
            }));
        }
        $scope.$on('$destroy', function () {
            for (var e in evts) { eventsService.unsubscribe(evts[e]); }
        });
        function getHistoryFiles() {
            $http.get(Umbraco.Sys.ServerVariables.uSyncHistory.Service+"GetHistory")
                .then(function (result) {
                    vm.history = result.data;
                });
        }

        function clearHistory() {
            localizationService.localizeMany([
                "uSyncHistory_clearTitle",
                "uSyncHistory_clearMessage"])
                .then(function (data) {
                    var options = {
                        title: data[0],
                        content: data[1],
                        disableBackdropClick: true,
                        disableEscKey: true,
                        confirmType: 'delete',
                        submit: function () {
                            doClear();
                            overlayService.close();
                        }
                    };
                    overlayService.confirm(options);
                });
        }
        function doClear(){
            $http.get("/umbraco/backoffice/usync/usynchistory/ClearHistory")
                .then(function (result) {
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