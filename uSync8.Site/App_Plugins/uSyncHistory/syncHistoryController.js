(function () {
    'use strict';

    function historyController($scope, eventsService, overlayService, editorService, uSyncHistoryService) {

        var vm = this;
        vm.loaded = false;
        vm.viewDetails = viewDetails;
        vm.showPrompt = false;
        vm.clearHistory = clearHistory;

        eventsService.on('usync-dashboard.tab.change', function (event, args) {
            if (args.alias === 'history' && vm.loaded === false) {
                loadHistories();
                vm.loaded = true;
            }
        });

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

        function clearHistory() {

            var overlay = {
                "view": "default",
                "title": "Confirm delete",
                "content": "Do you want to remove all histroy ?",
                "disableBackdropClick": true,
                "disableEscKey": true,
                "submitButtonLabel": "Keep History",
                "closeButtonLabel": "Remove History",
                submit: function () {
                    overlayService.close();
                },
                close: function (model) {

                    model.submitButtonState = "busy";

                    uSyncHistoryService.clearHistory()
                        .then(function () {
                            loadHistories();
                            notificationsService.success('Removed', 'Sync history has been cleared');
                        });

                    overlayService.close();

                }
            };

            overlayService.open(overlay);

        }

    }

    angular.module('umbraco')
        .controller('uSyncHistoryController', historyController);

})();