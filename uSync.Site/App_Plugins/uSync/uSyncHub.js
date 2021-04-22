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

            console.log('initHub');

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
                                console.log('a', callbacks.length);
                                var cb = callbacks.pop();
                                hubSetup(cb);
                            }
                            starting = false;
                        });
                }
                else {
                    while (callbacks.length) {
                        console.log('x', callbacks.length);
                        var cb = callbacks.pop();
                        hubSetup(cb);
                    }
                    starting = false;
                }
            }
        }

        function hubSetup(callback) {

            console.log('setting up hub');
            $.connection = new signalR.HubConnectionBuilder()
                .withUrl(Umbraco.Sys.ServerVariables.uSync.signalRHub)
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Debug)
                .build();

            var hub = {};

            if ($.connection !== undefined) {
                hub = {
                    active: true,
                    start: function () {

                        try {
                            $.connection.start().then(function () {
                                console.log('Hub started', $.connection.connectionId);
                            }).catch(function () {
                                console.log('Failed to start hub');
                            });
                        } catch (e) {
                            console.log('Could not setup signalR connection', e);
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
                    start: function () { console.log('no hub to start - missing signalR library ?'); }
                };
            }

            return callback(hub);
        }
    }

    angular.module('umbraco.resources')
        .factory('uSyncHub', uSyncHub);
})();