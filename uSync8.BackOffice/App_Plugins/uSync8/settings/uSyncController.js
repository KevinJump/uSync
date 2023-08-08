﻿(function () {
    'use strict';

    function uSyncController($scope, $q, $controller,
        eventsService,
        overlayService,
        notificationsService,
        editorService,
        uSync8DashboardService,
        uSyncHub) {

        var vm = this;
        vm.loading = true;
        vm.working = false;
        vm.reported = false;
        vm.syncing = false;
        vm.hideLink = false;
        vm.showSpinner = false;
        vm.fresh = true;

        vm.isLoadbalanced = Umbraco.Sys.ServerVariables.uSync.isLoadBalanced;

        vm.groups = [];
        vm.perf = 0;

        vm.showAdvanced = false;

        vm.hasuSyncForms = false; 

        vm.canHaveForms = false;

        var modes = {
            NONE: 0,
            REPORT: 1,
            IMPORT: 2,
            EXPORT: 3
        };

        vm.runmode = modes.NONE;

        vm.showAll = false;
        vm.status = {};
        vm.reportAction = '';

        vm.importButton = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_import',
                handler: importItems
            },
            subButtons: [{
                labelKey: 'usync_importforce',
                handler: importForce
            }]
        };

        vm.reportButton = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_report',
                handler: function () {
                    report('');
                }
            },
            subButtons: []
        };

        vm.exportButton = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_export',
                handler: function () {
                    exportItems(false);
                }
            },
            subButtons: [{
                labelKey: 'usync_exportClean',
                handler: function () {
                    cleanExport();
                }
            }]
        }

        vm.report = report;
        vm.versionInfo = {
            IsCurrent: true
        };

        vm.exportItems = exportItems;
        vm.importForce = importForce;
        vm.importItems = importItems;

        vm.getTypeName = getTypeName;

        vm.showChange = showChange;
        vm.countChanges = countChanges;
        vm.calcPercentage = calcPercentage;
        vm.openDetail = openDetail;

        vm.savings = { show: false, title: "", message: "" };
        vm.godo = [
            { time: 0, message: "but its always worth checking." },
            { time: 0, message: "maybe add a cool new feature to your site. 🌟" },
            { time: 0, message: "if that is what you were expecting ... then yeah 🎉" },
            { time: 0, message: "not everything has to change. 🤷‍♂️" },
            { time: 180, message: "Go make a cup of tea" },
            { time: 300, message: "Go have a quick chat" },
            { time: 900, message: "Go for a nice walk outside 🚶‍♀️" },
            { time: 3600, message: "You deserve a break, go be with the trees 🌲🌳🌴" }
        ];

        init();

        function init() {
            InitHub();
            getHandlerGroups();

            // just so there is something there when you start 
            uSync8DashboardService.getHandlers()
                .then(function (result) {
                    vm.handlers = result.data;
                    vm.status.Handlers = vm.handlers;
                });

            uSync8DashboardService.checkVersion()
                .then(function (result) {
                    vm.versionInfo = result.data;
                });
        }

        function performAction(options, actionMethod, cb) {

            return $q(function (resolve, reject) {
                uSync8DashboardService.getActionHandlers(options)
                    .then(function (result) {
                        vm.status.Handlers = result.data;
                        performHandlerAction(vm.status.Handlers, actionMethod, options, cb)
                            .then(function () {
                                resolve();
                            }, function (error) {
                                reject(error)
                            })
                    });
            });
        }

        function performHandlerAction(handlers, actionMethod, options, cb) {

            return $q(function (resolve, reject) {

                var index = 0;
                vm.status.Message = 'Starting ' + options.action;

                uSync8DashboardService.startProcess(options.action)
                    .then(function () {
                        runHandlerAction(handlers[index])
                    });

                function runHandlerAction(handler) {

                    vm.status.Message = handler.Name;

                    handler.Status = 1;
                    actionMethod(handler.Alias, options, getClientId())
                        .then(function (result) {

                            vm.results = vm.results.concat(result.data.Actions);

                            handler.Status = 2;
                            handler.Changes = countChanges(result.data.Actions);

                            index++;
                            if (index < handlers.length) {
                                runHandlerAction(handlers[index]);
                            }
                            else {

                                vm.status.Message = 'Finishing ' + options.action;

                                uSync8DashboardService.finishProcess(options.action, vm.results)
                                    .then(function () {
                                        resolve();
                                    });
                            }
                        }, function (error) {
                            // error in this handler ? 
                            // do we want to carry on with the other ones or just stop?
                            reject(error);
                        });
                }
            });
        } 

        function report(group) {

            vm.results = [];

            resetStatus(modes.REPORT);
            getWarnings('report');
            vm.reportButton.state = 'busy';

            var options = {
                action: 'report',
                group: group
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.reportHandler)
                .then(function (results) {
                    vm.working = false;
                    vm.reported = true;
                    vm.perf = performance.now() - start;
                    vm.status.Message = 'Report complete';
                    vm.reportButton.state = 'success';
                }, function (error) {
                    vm.reportButton.state = 'error';
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }

        function importForce() {
            importItems(true);
        }

        function importItems(force, group) {
            vm.results = [];
            resetStatus(modes.IMPORT);
            getWarnings('import');

            vm.importButton.state = 'busy';

            var options = {
                action: 'import',
                group: group,
                force: force
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.importHandler)
                .then(function (results) {

                    vm.status.Message = 'Post import actions';

                    uSync8DashboardService.importPost(vm.results, getClientId())
                        .then(function (results) {
                            vm.working = false;
                            vm.reported = true;
                            vm.perf = performance.now() - start;
                            vm.importButton.state = 'success';
                            eventsService.emit('usync-dashboard.import.complete');
                            calculateTimeSaved(vm.results);
                            vm.status.Message = 'Complete';
                        });
                }, function (error) {
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }

        function exportItems() {

            vm.results = [];
            resetStatus(modes.EXPORT);
            vm.exportButton.state = 'busy';

            var options = {
                action: 'export',
                group: ''
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.exportHandler)
                .then(function (results) {
                    vm.status.Message = 'Export complete';
                    vm.working = false;
                    vm.reported = true;
                    vm.perf = performance.now() - start;

                    vm.exportButton.state = 'success';
                    vm.savings.show = true;
                    vm.savings.title = 'All items exported.';
                    vm.savings.message = 'Now go wash your hands 🧼!';
                    eventsService.emit('usync-dashboard.export.complete');
                }, function (error) {
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }
     
        function cleanExport() {

            overlayService.open({
                title: 'Clean Export',
                content: 'Are you sure ? A clean export will delete all the contents of the uSync folder. You will loose any stored delete or rename actions.',
                disableBackdropClick: true,
                disableEscKey: true,
                submitButtonLabel: 'Yes run a clean export',
                closeButtonLabel: 'No, close',
                submit: function () {
                    overlayService.close();

                    uSync8DashboardService.cleanExport()
                        .then(function () {
                            exportItems();
                        });
                },
                close: function () {
                    overlayService.close();
                }
            })
        }

        // add a little joy to the process.
        function calculateTimeSaved(results) {
            var changes = countChanges(results);
            var time = changes * 26.5;

            var duration = moment.duration(time, 'seconds');

            if (time >= 180) {
                vm.savings.show = true;
                vm.savings.title = 'You just saved ' + duration.humanize() + "!";
                vm.savings.message = '';

                for (let x = 0; x < vm.godo.length; x++) {
                    if (vm.godo[x].time <= time) {
                        vm.savings.message = vm.godo[x].message;
                    }
                    else {
                        break;
                    }
                }
            }
            else if (time == 0) {
                var r = Math.floor(Math.random() * 4);
                vm.savings.show = true;
                vm.savings.title = 'No changes this time, ';
                vm.savings.message = vm.godo[r].message;
            }
        }

        //////////////

        function getWarnings(action) {
            uSync8DashboardService.getSyncWarnings(action)
                .then(function (result) {
                    vm.warnings = result.data;
                });
        }


        function getHandlerGroups() {
            uSync8DashboardService.getHandlerGroups()
                .then(function (result) {
                    angular.forEach(result.data, function (group, key) {

                        vm.groups.push({
                            name: group.toLowerCase(),
                            icon: group.toLowerCase()
                        });

                        vm.importButton.subButtons.push({
                            handler: function () {
                                importGroup(group);
                            },
                            labelKey: 'usync_import-' + group.toLowerCase()
                        });

                        vm.reportButton.subButtons.push({
                            handler: function () {
                                report(group);
                            },
                            labelKey: 'usync_report-' + group.toLowerCase()
                        });

                        if (group.toLowerCase() === "forms") {
                            vm.hasuSyncForms = true;
                        }

                    });

                    if (!vm.hasuSyncForms) {
                        vm.canHaveForms = canHaveForms();
                    }

                    vm.loading = false;
                }, function (error) {
                    vm.loading = false;
                });
        }

        function importGroup(group) {
            importItems(false, group);
        }

        //////////////

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

        function getTypeName(typeName) {
            var umbType = typeName.substring(0, typeName.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        function countChanges(changes) {
            var count = 0;
            angular.forEach(changes, function (val, key) {
                if (val.Change !== 'NoChange' && val.Change !== 'Hidden') {
                    count++;
                }
            });

            return count;
        }

        function calcPercentage(status) {
            return (100 * status.Count) / status.Total;
        }

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
        }

        function setFilter(type) {

            if (vm.filter === type) {
                vm.filter = '';
            }
            else {
                vm.filter = type;
            }
        }

        ///////////

        /// resets all the flags, and messages to the start 
        function resetStatus(mode) {
            vm.warnings = {};

            vm.fresh = false;
            vm.reported = vm.showAll = false;
            vm.working = true;
            vm.showSpinner = false; 
            vm.runmode = mode;
            vm.hideLink = false;
            vm.savings.show = false;

            vm.status = {
                Count: 0,
                Total: 1,
                Message: 'Initializing',
                Handlers: vm.handlers
            };

            if (!vm.hub.active) {
                vm.status.Message = 'Working ';
                vm.showSpinner = true;
            }

            vm.update = {
                Message: '',
                Count: 0,
                Total: 1
            };

            // performance timer. 
            vm.perf = 0;


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

        ////// SignalR things 
        function InitHub() {
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
        }

        function getClientId() {
            if ($.connection !== undefined && $.connection.hub !== undefined) {
                return $.connection.hub.id;
            }
            return "";
        }

        function canHaveForms() {

            if (vm.hasuSyncForms) return false;

            try {

                // check to see if umbraco.forms is installed. 
                $controller('UmbracoForms.Dashboards.FormsController', { $scope: {} }, true)
                return true;
            }
            catch {
                return false;
            }
        }

    }

    angular.module('umbraco')
        .controller('uSync8Controller', uSyncController);
})();