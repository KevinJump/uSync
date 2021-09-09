(function () {
    'use strict';

    var uSyncReportViewComponent = {
        templateUrl: Umbraco.Sys.ServerVariables.application.applicationPath + 'App_Plugins/uSync/components/usync.reportview.html',
        bindings: {
            action: '<',
            results: '<',
            hideAction: '<',
            hideLink: '<',
            showAll: '<',
            hideToggle: '<'
        },
        controllerAs: 'vm',
        controller: uSyncReportViewController
    };

    function uSyncReportViewController($scope, editorService, uSync8DashboardService) {

        var vm = this;

        vm.showChange = showChange;
        vm.getIcon = getIcon;
        vm.getChangeClass = getChangeClass;
        vm.getTypeName = getTypeName;
        vm.countChanges = countChanges;
        vm.openDetail = openDetail;
        vm.showAll = vm.showAll || false;

        vm.$onInit = function () {
            vm.hideLink = vm.hideLink ? true : false;
            vm.hideAction = vm.hideAction ? true : false;
        };


        vm.apply = apply;
        vm.status = status;

        /////////

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
        }

        function hasFailedDetail(details) {
            if (details == null || details.length == 0) {
                return false;
            }

            return details.some(function (detail) {
                return !detail.Success;
            })
        }

        function getChangeClass(result) {
            if (!result.success) {
                return 'usync-change-row-Fail';
            }
            else if (hasFailedDetail(result.details)) {
                return 'usync-change-row-Warn';
            }

            return 'usync-change-row-' + result.change;
        }

        function getIcon(result) {
            if (!result.success) {
                return "icon-delete color-red";
            }
            else if (hasFailedDetail(result.details)) {
                return "icon-alert color-yellow";
            }
            switch (result.change) {
                case 'NoChange':
                    return 'icon-check color-grey';
                case 'Update':
                    return 'icon-check color-orange';
                case 'Delete':
                    return 'icon-delete color-red';
                case 'Import':
                    return 'icon-check color-green';
                case 'Export':
                    return 'icon-check color-green';
                default:
                    return 'icon-flag color-red';
            }
        }

        function getTypeName(typeName) {
            if (typeName !== undefined) {
                return typeName.substring(typeName.lastIndexOf('.') + 1);
            }
            return "??";
        }

        function countChanges(changes) {
            var count = 0;
            angular.forEach(changes, function (val, key) {
                if (val.change !== 'NoChange') {
                    count++;
                }
            });

            return count;
        }

        function openDetail(item) {

            var options = {
                item: item,
                title: 'uSync Change',
                view: Umbraco.Sys.ServerVariables.application.applicationPath + "App_Plugins/uSync/changedialog.html",
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);
        }

        function apply(item) {

            // do some application thing (apply just one item)
            item.applyState = 'busy';
            uSync8DashboardService.importItem(item)
                .then(function (result) {
                    item.applyState = 'success';
                }, function (error) {
                    console.error(error);
                    item.applyState = 'error';
                });
        }

        function status(item) {
            if (item.applyState === undefined) return 'init';
            return item.applyState;
        }

    }

    angular.module('umbraco')
        .component('usyncReportView', uSyncReportViewComponent);
})();