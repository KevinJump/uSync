(function () {
    'use strict';

    function uSyncHub($rootScope, $q, assetsService) {

        var starting = false;
        var callbacks = []; 

        var scripts = [
            Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/lib/signalr/signalr.min.js']

        var resource = {
            initHub: initHub
        };

        return resource;

        //////////////

        function initHub(callback) {

            callbacks.push(callback);

            if (!starting) {
                if ($.connection === undefined) {
                    starting = true;

                    var promises = [];
                    scripts.forEach(function (script) {
                        promises.push(assetsService.loadJs(script));
                    });

                    $q.all(promises)
                        .then(function () {
                            while (callbacks.length) {
                                var cb = callbacks.pop();
                                hubSetup(cb);
                            }
                            starting = false;
                        });
                }
                else {
                    while (callbacks.length) {
                        var cb = callbacks.pop();
                        hubSetup(cb);
                    }
                    starting = false;
                }
            }
        }

        function hubSetup(callback) {

            $.connection = new signalR.HubConnectionBuilder()
                .withUrl(Umbraco.Sys.ServerVariables.uSync.signalRHub)
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            var hub = {};

            if ($.connection !== undefined) {
                hub = {
                    active: true,
                    start: function (cb) {

                        try {
                            $.connection.start().then(function () {
                                // console.info('Hub started', $.connection.connectionId);
                                if (cb) {
                                    cb(true);
                                }
                            }).catch(function () {
                                console.warn('Failed to start hub');
                                if (cb) {
                                    cb(false);
                                }
                            });
                        } catch (e) {
                            console.warn('Could not setup signalR connection', e);
                            if (cd) {
                                cb(false);
                            }
                        }

                    },
                    on: function (eventName, callback) {
                        $.connection.on(eventName, function (result) {
                            $rootScope.$apply(function () {
                                if (callback) {
                                    callback(result);
                                }
                            });
                        });
                    },
                    invoke: function (methodName, callback) {
                        $.connection.invoke(methodName)
                            .done(function (result) {
                                $rootScope.$apply(function () {
                                    if (callback) {
                                        callback(result);
                                    }
                                });
                            });
                    }
                };
            }
            else {
                hub = {
                    on: function () { },
                    invoke: function () { },
                    start: function () { console.warn('no hub to start - missing signalR library ?'); }
                };
            }

            return callback(hub);
        }
    }

    angular.module('umbraco.resources')
        .factory('uSyncHub', uSyncHub);
})();