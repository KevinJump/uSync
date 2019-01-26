/**
 *@ngdoc
 *@name uSync8DashboardController
 *@requires uSync8DashboardService
 * 
 *@description controller for the uSync dashboard
 */

(function () {
    'use strict';

    function uSyncDashboardController($scope, uSync8DashboardService ) {

        var vm = this;
        vm.loading = true;
        vm.settings = {};
        vm.handlers = [];

        // functions 
        vm.report = report;


        // kick it all off
        init();

        ////// public 

        function report() {
            vm.working = true;

            uSync8DashboardService.report()
                .then(function (result) {
                    vm.results = result.data;
                    vm.working = false;
                });
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