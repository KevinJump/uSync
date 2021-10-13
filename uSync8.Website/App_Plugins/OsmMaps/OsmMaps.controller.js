angular.module("umbraco").controller("OsmMaps.PropertyEditorController",
    function ($rootScope, $scope, notificationsService, assetsService, $timeout) {

        var
            place,
            geocoder,
            mapCenter,

            //Getting prevalues - umbraco location by default if nothing has been set
            defaultLat = $scope.model.config ? 55.406191899999996 : $scope.model.config.lat,
            defaultLng = $scope.model.config ? 10.388192199999999 : $scope.model.config.lng;
        defaultZoomLvl = parseInt($scope.model.config ? 30 : $scope.model.config.zoomlevel);
        // key = $scope.model.config.key;
        var self = this;
        self.map = null;
        self.marker = null;

        $scope.model.uid = generateID();

        self.mapElement = $scope.model.alias + "_" + $scope.model.uid + "_map";


        //Loading the styles
        assetsService.loadCss("/app_plugins/OsmMaps/assets/css/osmmaps.css");
        assetsService.loadCss('/app_plugins/OsmMaps/assets/leaflet/leaflet.css');
        assetsService.loadJs('/app_plugins/OsmMaps/assets/leaflet/leaflet.js')
            .then(function () {
                $timeout(function () {
                    initializeMap();
                });
            });

        function generateID() {
            var d = new Date().getTime();
            var id = 'xxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = (d + Math.random() * 16) % 16 | 0;
                d = Math.floor(d / 16);
                return (c === "x" ? r : (r & 0x3 | 0x8)).toString(16);
            });
            return id;
        };

        function initializeMap() {
            self.map = L.map(self.mapElement);

            L.tileLayer('https://{s}.tile.osm.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
            }).addTo(self.map);

            // Getting text for the reset button
            $scope.resetTxt = $scope.model.config.resetTxt;

            var location = $scope.model.value ? $scope.model.value.split(',') : null;

            resetBtn = document.getElementById("umb-osmmaps-reset-" + $scope.model.uid);

            if (location !== undefined && location !== null && location.length >= 2) {
                var zoom = defaultZoomLvl;
                if (location.length === 3) {
                    zoom = location[2];
                }
                self.map.setView([location[0], location[1]], zoom);
                self.marker = L.marker(location).addTo(self.map);
            }
            else {
                self.map.setView([defaultLat, defaultLng], defaultZoomLvl);
                self.marker = L.marker([defaultLat, defaultLng]).addTo(self.map);
            }

            self.map.on('resize', function (e) {
                var center = self.map.getCenter();
                self.map.setView(center);
            });

            self.map.on('click', function (e) {
                var center = e.latlng;
                $scope.model.value = e.latlng.lat + "," + e.latlng.lng + "," + self.map.getZoom();
                self.marker.setLatLng(e.latlng); // .update(); // we coude use .update() if we update the latlng object
            });
        }
    });