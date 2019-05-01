(function () {
    'use strict';

    function dashboardController(
        $scope, notificationsService, uSync8DashboardService) {

        var vm = this;

        vm.page = {
            title: 'uSync 8',
            description: 'beta6',
            navigation: [
                {
                    'name': 'uSync',
                    'alias': 'uSync',
                    'icon': 'icon-infinity',
                    'view': '/App_plugins/usync8/settings/default.html',
                    'active': true
                },
                {
                    'name': 'Settings',
                    'alias': 'settings',
                    'icon': 'icon-settings',
                    'view': '/App_Plugins/uSync8/settings/settings.html'
                },
/*
                {
                    'name': 'Add ons',
                    'alias': 'expansion',
                    'icon': 'icon-box',
                    'view': '/App_plugins/usync8/settings/expansion.html'
                } */
            ]
        };


        uSync8DashboardService.getAddOns()
            .then(function (result) {
                vm.page.description += ' + ' + result.data.AddOnString;
                vm.addOns = result.data.AddOns;

                vm.addOns.forEach(function (value, key) {
                    if (value.View !== '') {
                        console.log(value);

                        vm.page.navigation.splice(vm.page.navigation.length-2, 0, 
                        {
                            'name': value.DisplayName,
                            'alias': value.Alias,
                            'icon': value.Icon,
                            'view': value.View
                        });
                    }
                });
            });

    }

    angular.module('umbraco')
        .controller('uSyncSettingsDashboardController', dashboardController);
})();