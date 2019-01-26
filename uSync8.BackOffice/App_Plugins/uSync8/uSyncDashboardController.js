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

        init();


        ////// public 


        ////// private 

        function init() {
            getSettings();
        }

        function getSettings() {

            uSync8DashboardService.getSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loading = false;

                });
        }

    }

    angular.module('umbraco')
        .controller('uSync8DashboardController', uSyncDashboardController);

})();