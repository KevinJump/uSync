/**
 *@ngdoc
 *@name uSync8DashboardController
 *@requires uSync8DashboardService
 * 
 *@description controller for the uSync dashboard
 */

(function () {
    'use strict';

    function uSyncDashboardController($scope,
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
        vm.settingsView = false;

        vm.settings = {};
        vm.handlers = [];
        vm.status = {};

        vm.reportAction = '';

        // buttons 

        vm.importButton = {
            state: 'init',
            defaultButton: {
                labelKey: "usync_import",
                handler: importItems
            },
            subButtons: [{
                labelKey: "usync_importforce",
                handler: importForce
            }]
        };
            

        // functions 
        vm.report = report;
        vm.exportItems = exportItems;
        vm.importForce = importForce;
        vm.importItems = importItems;

        vm.saveSettings = saveSettings;

        vm.toggleDetails = toggleDetails;
        vm.getTypeName = getTypeName;
        vm.toggleAll = toggleAll;
        vm.countChanges = countChanges;
        vm.calcPercentage = calcPercentage;

        vm.toggleSettings = toggleSettings;
        vm.toggle = toggle;

        vm.showChange = showChange;

        vm.openDetail = openDetail;

        // kick it all off
        init();

        ////// public 

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

        function saveSettings() {
            vm.working = false;
            uSync8DashboardService.saveSettings(vm.settings)
                .then(function (result) {
                    vm.working = false;
                    notificationsService.success('Saved', 'Settings updated');
                }, function (error) {
                    notificationsService.error('Saving', error.data.Message);
                });
        }

        function toggleDetails(result) {
            result.showDetails = !result.showDetails;
        }


        function getTypeName(typeName) {
            var umbType = typeName.substring(0, typeName.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        function toggleAll() {
            vm.showAll = !vm.showAll;
        }

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
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

        function toggle(item) {
            item = !item;
        }

        function toggleSettings() {
            vm.settingsView = !vm.settingsView;
        }

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

        ////// private 

        function init() {

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

            getSettings();
            getHandlers();
        }

        function getSettings() {

            uSync8DashboardService.getSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loading = false;
                });

            uSync8DashboardService.getAddOnString()
                .then(function (result) {
                    vm.addOnString = result.data;
                });
        }

        function getHandlers() {
            uSync8DashboardService.getHandlers()
                .then(function (result) {
                    vm.status.Handlers = result.data;
                });
        }

        function resetStatus(mode) {
            vm.reported = false;
            vm.working = true;
            vm.runmode = mode;
            vm.showAll = false;

            vm.status = {
                Count: 0,
                Total: 1,
                Message: "Initializing",
                Handlers: []
            };  

            vm.update = {
                Message: "",
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

        function getClientId() {
            if ($.connection !== undefined && $.connection.hub !== undefined) {
               return $.connection.hub.id;
            }

            return "";
        }

    }

    angular.module('umbraco')
        .controller('uSync8DashboardController', uSyncDashboardController);

})();