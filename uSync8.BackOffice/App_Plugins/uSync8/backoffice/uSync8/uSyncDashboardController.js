(function () {
    'use strict';

    function dashboardController(
        $scope, $timeout, navigationService, notificationsService, uSync8DashboardService) {

        var vm = this;

        vm.page = {
            title: 'uSync 8',
            description: '8.1.x',
            navigation: [
                {
                    'name': 'uSync',
                    'alias': 'uSync',
                    'icon': 'icon-infinity',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync8/settings/default.html',
                    'active': true
                },
                {
                    'name': 'Settings',
                    'alias': 'settings',
                    'icon': 'icon-settings',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync8/settings/settings.html'
                },
                {
                    'name': 'Add ons',
                    'alias': 'expansion',
                    'icon': 'icon-box',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync8/settings/expansion.html'
                } 
            ]
        };

        $timeout(function () {
            navigationService.syncTree({ tree: "uSync8", path: "-1" });
        });


        uSync8DashboardService.getAddOns()
            .then(function (result) {
                vm.page.description = 'v' + result.data.Version;
                if (result.data.AddOnString.length > 0) {
                    vm.page.description += ' + ' + result.data.AddOnString;
                }
                vm.addOns = result.data.AddOns;

                vm.addOns.forEach(function (value, key) {
                    if (value.View !== '') {
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