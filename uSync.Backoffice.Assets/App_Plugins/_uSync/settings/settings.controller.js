(function () {
    'use strict';

    function settingsController($scope,
        uSync8DashboardService,
        overlayService,
        notificationsService) {

        var vm = this;
        vm.working = false; 
        vm.loading = true;
        vm.readonly = true;

        vm.docslink = "https://docs.jumoo.co.uk/uSync/v9/settings/";

        vm.umbracoVersion = Umbraco.Sys.ServerVariables.application.version;

        vm.saveSettings = saveSettings;
        vm.openAppSettingsOverlay = openAppSettingsOverlay;

        init();

        ///////////

        function init() {
            getSettings();
        }

        ///////////
        function getSettings() {

            uSync8DashboardService.getSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loading = false;
                    getHandlerSetSettings(vm.settings.defaultSet);
                });
        }

        function getHandlerSetSettings(setname) {

            uSync8DashboardService.getHandlerSetSettings(setname)
                .then(function (result) {
                    vm.handlerSet = result.data;
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



        function openAppSettingsOverlay() {

            var appSetting = {
                "uSync": {
                    "Settings": vm.settings,
                    "Sets": {
                        "Default": vm.handlerSet
                    }
                }
            };

            var options = {
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync/settings/settings.overlay.html',
                title: 'appsettings.json snipped',
                content: JSON.stringify(appSetting, null, 4),
                docslink: vm.docslink,
                disableBackdropClick: true,
                disableEscKey: true,
                hideSubmitButton: true,
                submit: function () {
                    overlayService.close();
                }
            };

            overlayService.confirm(options);

        }


    }

    angular.module('umbraco')
        .controller('uSyncSettingsController', settingsController);
})();