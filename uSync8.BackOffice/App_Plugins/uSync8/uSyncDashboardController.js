/**
 *@ngdoc
 *@name uSync8DashboardController
 *@requires uSync8DashboardService
 * 
 *@description controller for the uSync dashboard
 */

(function () {
    'use strict';

    function uSyncDashboardController($scope, uSync8DashboardService) {

        var vm = this;
        vm.loading = true;

        vm.working = false;
        vm.reported = false;
        vm.syncing = false;

        vm.showAll = false;

        vm.settings = {};
        vm.handlers = [];

        // functions 
        vm.report = report;
        vm.exportItems = exportItems;

        vm.toggleDetails = toggleDetails;
        vm.getTypeName = getTypeName;
        vm.toggleAll = toggleAll;
        vm.countChanges = countChanges;

        // kick it all off
        init();

        ////// public 

        function report() {
            vm.reported = false;
            vm.working = true;

            uSync8DashboardService.report()
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                    vm.reported = true;
                });
        }

        function exportItems() {
            vm.reported = false;
            vm.working = true;

            uSync8DashboardService.exportItems()
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                    vm.reported = true;
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

        function countChanges(changes) {
            var count = 0;
            angular.forEach(changes, function (val, key) {
                if (val.Change !== 'NoChange') {
                    count++;
                }
            });

            return count;
        }

        ////// private 

        function init() {
            getSettings();
            getHandlers();
        }

        function getSettings() {

            uSync8DashboardService.getSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loading = false;

                });
        }

        function getHandlers() {
            uSync8DashboardService.getHandlers()
                .then(function (result) {
                    vm.handlers = result.data;
                });
        }

    }

    angular.module('umbraco')
        .controller('uSync8DashboardController', uSyncDashboardController);

})();