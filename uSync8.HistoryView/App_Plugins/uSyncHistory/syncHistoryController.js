(function () {
    'use strict';

    function historyController($scope, editorService, uSyncHistoryService) {

        var vm = this;
        vm.viewDetails = viewDetails;

        loadHistories();

        function loadHistories() {

            uSyncHistoryService.getHistory()
                .then(function (result) {
                    vm.histories = result.data;
                });
        }

        function viewDetails(history) {

            editorService.open({
                history: history,
                title: 'Sync History',
                view: Umbraco.Sys.ServerVariables.application.applicationPath + "App_Plugins/uSyncHistory/historyDialog.html",
                close: function () {
                    editorService.close();
                }
            });

        }

    }

    angular.module('umbraco')
        .controller('uSyncHistoryController', historyController);

})();