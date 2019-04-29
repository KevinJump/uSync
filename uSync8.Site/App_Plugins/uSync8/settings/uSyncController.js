(function () {
    'use strict';

    function uSyncController($scope,
        notificationsService,
        editorService,
        uSync8DashboardService,
        uSyncHub) {

        var vm = this;
        vm.loading = true;
        vm.working = false;
        vm.reported = false;
        vm.syncing = false; 

        var modes = {
            NONE: 0,
            REPORT: 1,
            IMPORT: 2,
            EXPORT: 3
        };

        vm.runmode = modes.NONE;

        vm.showAll = false; 
        vm.status = {};
        vm.reportAction = '';

        vm.importButton = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_import',
                handler: importItems
            },
            subButtons: [{
                labelKey: 'usync_importforce',
                handler: importForce
            }]
        };

        vm.report = report;
        vm.exportItems = exportItems;
        vm.importForce = importForce;
        vm.importItems = importItems;

        vm.getTypeName = getTypeName;

        vm.showChange = showChange;
        vm.countChanges = countChanges;
        vm.calcPercentage = calcPercentage;
        vm.openDetail = openDetail;

        init();

        function init() {
            InitHub();

            // just so there is something there when you start 
            uSync8DashboardService.getHandlers()
                .then(function (result) {
                    vm.handlers = result.data;
                    vm.status.Handlers = vm.handlers;
                });
        }

        ///////////
        function report() {
            resetStatus(modes.REPORT);

            uSync8DashboardService.report(getClientId())
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                    vm.reported = true;
                }, function (error) {
                    notificationsService.error('Reporting', error.data.Message);
                });
        }

        function exportItems() {
            resetStatus(modes.EXPORT);

            uSync8DashboardService.exportItems(getClientId())
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                    vm.reported = true;
                }, function (error) {
                    notificationsService.error('Exporting', error.data.Message);
                });
        }

        function importForce() {
            importItems(true);
        }

        function importItems(force) {
            resetStatus(modes.IMPORT);
            vm.importButton.state = 'busy';

            uSync8DashboardService.importItems(force, getClientId())
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                    vm.reported = true;
                    vm.importButton.state = 'success';
                }, function (error) {
                    vm.importButton.state = 'error';
                    notificationsService.error('Failed', error.data.ExceptionMessage);

                    vm.working = false;
                    vm.reported = true;
                });
        }

        //////////////

        function openDetail(item) {

            var options = {
                item: item,
                title: 'uSync Change',
                view: "/App_Plugins/uSync8/changeDialog.html",
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);
        }

        function getTypeName(typeName) {
            var umbType = typeName.substring(0, typeName.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        function countChanges(changes) {
            var count = 0;
            angular.forEach(changes, function (val, key) {
                if (val.Change !== 'NoChange') {
                    count++;
                }
            });

            return count;
        }

        function calcPercentage(status) {
            return (100 * status.Count) / status.Total;
        }

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
        }


        ///////////

        /// resets all the flags, and messages to the start 
        function resetStatus(mode) {
            vm.reported = vm.showAll = false;
            vm.working = true;
            vm.runmode = mode;

            vm.status = {
                Count: 0,
                Total: 1,
                Message: 'Initializing',
                Handlers: vm.handlers
            };

            vm.update = {
                Message: '',
                Count: 0,
                Total: 1
            };

            switch (mode) {
                case modes.IMPORT:
                    vm.action = 'Import';
                    break;
                case mode.REPORT:
                    vm.action = 'Report';
                    break;
                case mode.EXPORT:
                    vm.action = 'Export';
                    break;
            }
        }

        ////// SignalR things 
        function InitHub() {
            uSyncHub.initHub(function (hub) {

                vm.hub = hub;

                vm.hub.on('add', function (data) {
                    vm.status = data;
                });

                vm.hub.on('update', function (update) {
                    vm.update = update;
                });

                vm.hub.start();
            });
        }

        function getClientId() {
            if ($.connection !== undefined && $.connection.hub !== undefined) {
                return $.connection.hub.id;
            }
            return "";
        }
    }

    angular.module('umbraco')
        .controller('uSync8Controller', uSyncController);
})();