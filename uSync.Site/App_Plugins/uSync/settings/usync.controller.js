(function () {
    'use strict';

    function uSyncController($scope, $q, $controller,
        eventsService,
        overlayService,
        notificationsService,
        localizationService,
        editorService,
        uSync8DashboardService,
        uSyncHub) {

        var vm = this;
        vm.fresh = true;
        vm.loading = true;
        vm.versionLoaded = false;
        vm.working = false;
        vm.reported = false;
        vm.syncing = false;
        vm.hideLink = false;
        vm.showSpinner = false;
        vm.showEverything = true;

        vm.selection = [];

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

        vm.everything = {
            icon: 'icon-paper-plane-alt color-deep-orange',
            name: 'Everything',
            group: '',
            state: 'init',
            key: 'everything'
        }

        vm.everything.import = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_import',
                handler: function () { importItems(false, vm.everything); }
            },
            subButtons: [{
                labelKey: 'usync_importforce',
                handler: function () { importForce(vm.everything); }
            }]
        };

        vm.everything.export = {
            state: 'init',
            defaultButton: {
                labelKey: 'usync_export',
                handler: function () { exportGroup(vm.everything); }
            },
            subButtons: [{
                labelKey: 'usync_exportClean',
                handler: function () { cleanExport(); }
            }]
        }

        vm.report = report;
        vm.versionInfo = {
            IsCurrent: true
        };

        vm.importForce = importForce;
        vm.importItems = importItems;
        vm.importGroup = importGroup;
        vm.exportGroup = exportGroup;

        vm.getTypeName = getTypeName;

        vm.showChange = showChange;
        vm.countChanges = countChanges;
        vm.calcPercentage = calcPercentage;
        vm.openDetail = openDetail;

        vm.changeSet = changeSet; 
        
        init();

        function init() {
            InitHub();
            loadSavingsMessages();

            uSync8DashboardService.getDefaultSet()
                .then(function (result) {
                    vm.currentSet = result.data;
                    initSet(vm.currentSet);
                });

            uSync8DashboardService.getSelectableSets()
                .then(function (result) {
                    vm.sets = result.data;
                });

            uSync8DashboardService.checkVersion()
                .then(function (result) {
                    vm.versionLoaded = true;
                    vm.versionInfo = result.data;
                });

        }

        function initSet(setname) {

            vm.loading = true;

            getHandlerGroups();

            // just so there is something there when you start
            uSync8DashboardService.getHandlers(setname)
                .then(function (result) {
                    vm.handlers = result.data;
                    vm.status.handlers = vm.handlers;
                    vm.loading = false; 
                });
        }


        function performAction(options, actionMethod, cb) {

            return $q(function (resolve, reject) {
                uSync8DashboardService.getActionHandlers(options)
                    .then(function (result) {
                        vm.status.handlers = result.data;
                        performHandlerAction(vm.status.handlers, actionMethod, options, cb)
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
                vm.status.message = 'Starting ' + options.action;
                vm.status.total = handlers.length - 1;

                uSync8DashboardService.startProcess(options.action)
                    .then(function () {
                        runHandlerAction(handlers[index])
                    });

                function runHandlerAction(handler) {

                    vm.status.message = handler.name;

                    handler.status = 1;
                    actionMethod(handler.alias, options, getClientId())
                        .then(function (result) {

                            vm.results = vm.results.concat(result.data.actions);

                            handler.status = 2;
                            handler.changes = countChanges(result.data.actions);

                            index++;
                            vm.status.count = index;

                            if (index < handlers.length) {
                                runHandlerAction(handlers[index]);
                            }
                            else {

                                vm.status.message = 'Finishing ' + options.action;

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

            if (vm.working === true) return;

            vm.results = [];

            resetStatus(modes.REPORT);
            getWarnings('report');
            group.state = 'busy';

            var options = {
                action: 'report',
                group: group.group,
                set: vm.currentSet
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.reportHandler)
                .then(function (results) {
                    vm.working = false;
                    vm.reported = true;
                    vm.perf = performance.now() - start;
                    vm.status.message = 'Report complete';
                    group.state = 'success';
                }, function (error) {
                    vm.working = false;
                    group.state = 'error';
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }

        function importForce(group) {
            importItems(true, group);
        }

        function importItems(force, group) {

            if (vm.working === true) return;

            vm.results = [];
            resetStatus(modes.IMPORT);
            getWarnings('import');

            group.state = 'busy';

            var options = {
                action: 'import',
                group: group.group,
                force: force,
                set: vm.currentSet
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.importHandler)
                .then(function (results) {

                    vm.status.message = 'Post import actions';

                    uSync8DashboardService.importPost(vm.results, getClientId())
                        .then(function (results) {
                            vm.working = false;
                            vm.reported = true;
                            vm.perf = performance.now() - start;
                            group.state = 'success';
                            eventsService.emit('usync-dashboard.import.complete');
                            calculateTimeSaved(vm.results);
                            vm.status.message = 'Complete';
                        });
                }, function (error) {
                    vm.working = false;
                    vm.group.state = 'error';
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }

        function exportGroup(group) {

            if (vm.working === true) return;

            vm.results = [];
            resetStatus(modes.EXPORT);
            group.state = 'busy';

            var options = {
                action: 'export',
                group: group.group,
                set: vm.currentSet
            };

            var start = performance.now();

            performAction(options, uSync8DashboardService.exportHandler)
                .then(function (results) {
                    vm.status.message = 'Export complete';
                    vm.working = false;
                    vm.reported = true;
                    vm.perf = performance.now() - start;

                    group.state = 'success';
                    vm.savings.show = true;
                    vm.savings.title = 'All items exported.';
                    vm.savings.message = 'Now go wash your hands 🧼!';
                    eventsService.emit('usync-dashboard.export.complete');
                }, function (error) {
                    vm.working = false;
                    group.state = 'error';
                    notificationsService.error('Error', error.data.ExceptionMessage ?? error.data.exceptionMessage);
                });
        }

        function cleanExport() {

            localizationService.localizeMany(["usync_cleanTitle",
                "usync_cleanMsg", "usync_cleanSubmit", "usync_cleanClose"])
                .then(function (values) {


                    overlayService.open({
                        title: values[0],
                        content: values[1],
                        disableBackdropClick: true,
                        disableEscKey: true,
                        submitButtonLabel: values[2],
                        closeButtonLabel: values[3],
                        submit: function () {
                            overlayService.close();

                            uSync8DashboardService.cleanExport()
                                .then(function () {
                                    exportGroup(vm.everything);
                                });
                        },
                        close: function () {
                            overlayService.close();
                        }
                    });
                });
        }

        // add a little joy to the process.
        function calculateTimeSaved(results) {
            var changes = countChanges(results);
            var time = changes * 26.5;

            var duration = moment.duration(time, 'seconds');

            if (time >= 180) {
                vm.savings.show = true;

                vm.savings.title = 'You just saved about ' + duration.humanize() + "!";
                vm.savings.message = '';

                for (let x = 0; x < vm.godo.length; x++) {
                    if (vm.godo[x].time < time) {
                        vm.savings.message = vm.godo[x].message;
                    }
                    else {
                        break;
                    }
                }
            }
        }

        //////////////

        function getWarnings(action) {
            uSync8DashboardService.getSyncWarnings(action)
                .then(function (result) {
                    vm.warnings = result.data;
                });
        }

        vm.importGroup = {};
        vm.exportButtons = {};

        function getHandlerGroups() {
            vm.showEverything = false;
            vm.groups = [];

            uSync8DashboardService.getHandlerGroups(vm.currentSet)
                .then(function (result) {

                    var groups = result.data;
                    var isSingle = Object.keys(groups).length === 1;

                    _.forEach(groups, function (icon, group) {
                        if (group == '_everything') {
                            vm.showEverything = true;
                        }
                        else {

                            var groupInfo = {
                                name: group,
                                group: group,
                                icon: icon,
                                key: group.toLowerCase(),
                                state: 'init'
                            }

                            groupInfo.import = {
                                defaultButton: {
                                    labelKey: 'usync_import',
                                    handler: function () { importGroup(groupInfo) }
                                },
                                subButtons: [{
                                    labelKey: 'usync_importforce',
                                    handler: function () { importForce(groupInfo) }
                                }]
                            };

                            groupInfo.export = {
                                defaultButton: {
                                    labelKey: 'usync_export',
                                    handler: function () { exportGroup(groupInfo) }
                                }                                
                            };


                            if (isSingle) {
                                groupInfo.export.subButtons = [{
                                    labelKey: 'usync_exportClean',
                                    handler: function () {
                                        cleanExport();
                                    }
                                }];
                            }

                            vm.groups.push(groupInfo);

                            if (group.toLowerCase() === "forms") {
                                vm.hasuSyncForms = true;
                            }
                        }

                    });

                    if (vm.showEverything) {
                        vm.groups.push(vm.everything);
                    }

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
                view: "/App_Plugins/uSync/changeDialog.html",
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
                if (val.change !== 'NoChange') {
                    count++;
                }
            });

            return count;
        }

        function calcPercentage(status) {
            return (100 * status.count) / status.Total;
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

            vm.fresh = false;
            vm.warnings = {};

            vm.reported = vm.showAll = false;
            vm.working = true;
            vm.showSpinner = false;
            vm.runmode = mode;
            vm.hideLink = false;
            vm.savings.show = false;

            vm.status = {
                count: 0,
                total: 1,
                message: 'Initializing',
                handlers: vm.handlers
            };

            if (!vm.hub.active) {
                vm.status.Message = 'Working ';
                vm.showSpinner = true;
            }

            vm.update = {
                message: '',
                count: 0,
                total: 1
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

        // change the handler set
        function changeSet() {
            vm.reported = false;
            initSet(vm.currentSet);
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
            if ($.connection !== undefined) {
                return $.connection.connectionId;
            }
            return "";
        }

        function canHaveForms() {
            if (vm.hasuSyncForms) return false;
        }

        function loadSavingsMessages() {
            vm.savings = { show: false, title: "", message: "" };
            vm.godo = [
                { time: 0, message: "Worth checking" },
                { time: 180, message: "Go make a cup of tea" },
                { time: 300, message: "Go have a quick chat" },
                { time: 900, message: "Go for a nice walk outside 🚶‍♀️" },
                { time: 3600, message: "You deserve a break" }
            ];

            var keys = [];
            for (let n = 0; n < 5; n++) {
                keys.push('usync_godo' + n);
            }
            localizationService.localizeMany(keys)
                .then(function (values) {
                    for (let n = 0; n < values.length; n++) {
                        vm.godo[n].message = values[n];
                    }
                });
        }


    }

    angular.module('umbraco')
        .controller('uSync8Controller', uSyncController);
})();