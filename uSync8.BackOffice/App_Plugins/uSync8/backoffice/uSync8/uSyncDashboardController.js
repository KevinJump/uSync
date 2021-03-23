(function () {
    'use strict';

    function dashboardController($controller,
        $scope, $timeout, navigationService, eventsService, uSync8DashboardService) {

        var vm = this;

        vm.selectNavigationItem = function (item) {
            eventsService.emit('usync-dashboard.tab.change', item);
        }

        vm.page = {
            title: 'uSync 8',
            description: '...',
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
                } 
            ]
        };

        $timeout(function () {
            navigationService.syncTree({ tree: "uSync8", path: "-1" });
        });

        uSync8DashboardService.getAddOns()
            .then(function (result) {

                vm.version = 'v' + result.data.Version;
                if (result.data.AddOnString.length > 0) {
                    vm.version += ' + ' + result.data.AddOnString;
                }

                vm.page.description = vm.version;
                vm.addOns = result.data.AddOns;

                var insertOffset = 1;
                if (vm.version.indexOf('Complete') == -1) {
                    insertOffset = 2;
                    vm.page.navigation.push(
                        {
                            'name': 'Add ons',
                            'alias': 'expansion',
                            'icon': 'icon-box',
                            'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync8/settings/expansion.html'
                        });
                }

                vm.addOns.forEach(function (value, key) {
                    if (value.View !== '') {
                        vm.page.navigation.splice(vm.page.navigation.length - insertOffset, 0,
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