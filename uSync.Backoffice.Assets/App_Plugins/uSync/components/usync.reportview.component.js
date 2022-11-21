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
            hideToggle: '<',
            allowSelect: '<',
            selection: '='
        },
        controllerAs: 'vm',
        controller: uSyncReportViewController
    };

    function uSyncReportViewController($scope, editorService,
        overlayService, uSync8DashboardService) {

        var vm = this;

        vm.showChange = showChange;
        vm.getChangeClass = getChangeClass;
        vm.openDetail = openDetail;
        vm.showAll = vm.showAll || false;

        vm.groups = [];
        vm.totals = {};

        vm.$onInit = function () {
            vm.hideLink = vm.hideLink ? true : false;
            vm.hideAction = vm.hideAction ? true : false;

            vm.groups = groupCounts(vm.results);
            vm.totals = getCounts(vm.results, true);
        };

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
                return !detail.success;
            })
        }

        function getChangeClass(result) {

            var classString = '';
            if (vm.allowSelect || result.exception != null) {
                classString = '-usync-can-select ';
            }

            if (result.__selected) {
                classString += '-selected '
            }

            if (!result.success) {
                return classString + 'usync-change-row-Fail';
            }
            else if (hasFailedDetail(result.details)) {
                return classString + ' usync-change-row-Warn';
            }

            return classString + ' usync-change-row-' + result.change;
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
                case 'Information':
                    return 'icon-info color-blue';
                default:
                    return 'icon-flag color-red';
            }
        }

        function getTypeName(typeName) {
            if (typeName !== undefined) {
                return typeName.substring(1);
            }
            return "??";
        }

        function openDetail(item) {

            var options = {
                item: item,
                title: 'uSync Change',
                showApply: !vm.hideAction,
                view: Umbraco.Sys.ServerVariables.application.applicationPath + "App_Plugins/uSync/changedialog.html",
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);
        }

        vm.select = select;

        function select(item, $event) {

            $event.stopPropagation();

            if (vm.allowSelect && vm.selection !== undefined) {
                var index = _.findIndex(vm.selection,
                    (x) => (x.key == item.key && x.name == item.name)
                );
                if (index === -1) {
                    vm.selection.push(item);
                    item.__selected = true;
                }
                else {
                    vm.selection.splice(index, 1);
                    item.__selected = false; 
                }
            }

            if (item.exception != null) {

                overlayService.open({
                    view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync/itemdialog.html',
                    title: item.name,
                    item: item,
                    exception: item.exception,
                    size: 'usync-error',
                    disableBackdropClick: true,
                    disableEscKey: true,
                    disableSubmitButton: true,
                    closeButtonLabelKey: 'general_close',
                    close: function () {
                        overlayService.close();
                    }
                });
            }
        }

        function status(item) {
            if (item.applyState === undefined) return 'init';
            return item.applyState;
        }

        // group changes. 
        function groupCounts(results) {

            vm.groupInfo = [];

            var grouped = _.groupBy(results, function (result) {
                return result.itemType;
            });

            _.forEach(grouped, function (group, key) {
                vm.groupInfo[key] = {
                    visible: false,
                    icon: getGroupIcon(group),
                    counts: getCounts(group),
                    name = key.substring(1) + "s"
                };
            });

            return grouped;
        } 

        function getCounts(group, addItemInfo) {

            var counts = {
                total: group.length,
                changes: 0,
                updates: 0,
                deletes: 0,
                imports: 0,
                exports: 0,
                infos: 0,
                errors: 0,
                noChange: 0
            };

            _.forEach(group, function (item) {


                switch (item.change) {
                    case 'NoChange':
                        counts.noChange++;
                        break;
                    case 'Update':
                        counts.updates++;
                        break;
                    case 'Delete':
                        counts.deletes++;
                        break;
                    case 'Import':
                        counts.imports++;
                        break;
                    case 'Export':
                        counts.exports++;
                        break;
                    case 'Information':
                        counts.infos++;
                        break;
                    default:
                        counts.errors++;
                }

                if (addItemInfo) {
                    item._icon = getIcon(item);
                    item._typename = getTypeName(item.itemType);
                }
            });

            // or changes.changes = changes.total - changes.noChange; // ??
            counts.changes =
                counts.updates +
                counts.deletes +
                counts.imports +
                counts.exports +
                counts.infos +
                counts.exports;

            return counts;                
        }

        function getGroupIcon(group) {
            _.forEach(group, function (item) {
                if (!item.success) {
                    return "icon-delete color-red";
                }
                else if (hasFailedDetail(item.details)) {
                    return "icon-alert color-yellow";
                }
            });

            return "icon-check color-green";
        }

    }

    angular.module('umbraco')
        .component('usyncReportView', uSyncReportViewComponent);
})();