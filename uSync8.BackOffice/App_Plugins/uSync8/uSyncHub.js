(function () {
    'use strict';

    function uSyncHub($rootScope, $q, assetsService) {

        var starting = false;
        var callbacks = [];

        var scripts = [
            Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/lib/signalr/jquery.signalR.js',
            Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/backoffice/signalr/hubs'];

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
                    // nothing else is using signalR (yet)
                    // on this site.
                    //
                    // You should load it only after a check
                    // because if you just initialize the 
                    // scripts each time, then when something 
                    // else is using signalR the settings
                    // will get wiped. 

                    var promises = [];
                    scripts.forEach(function (script) {
                        promises.push(assetsService.loadJs(script));
                    });

                    // when everything is loaded setup the hub
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
            var proxy = $.connection.uSyncHub;

            var hub = {
                start: function () {
                    if ($.connection.hub.state !== $.connection.connectionState.disconnected) {
                        // restart the hub - if it has already been started, or our callbacks won't be registered
                        $.connection.hub.stop(true, true);
                    }

                    $.connection.hub.start();
                },
                on: function (eventName, callback) {
                    proxy.on(eventName, function (result) {
                        $rootScope.$apply(function () {
                            if (callback) {
                                callback(result);
                            }
                        });
                    });
                },
                invoke: function (methodName, callback) {
                    proxy.invoke(methodName)
                        .done(function (result) {
                            $rootScope.$apply(function () {
                                if (callback) {
                                    callback(result);
                                }
                            });
                        });
                },
                clientId: function () {
                    if ($.connection !== undefined && $.connection.hub !== undefined) {
                        return $.connection.hub.id;
                    }
                    return "";
                }

            };

            return callback(hub);
        }
    }

    angular.module('umbraco.resources')
        .factory('uSyncHub', uSyncHub);
})();