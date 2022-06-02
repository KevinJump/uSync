angular.module("umbraco")
    .controller("Bergmania.OpenStreetMap.Controller", ["$scope", "$element", "$timeout", "userService", function ($scope, $element, $timeout, userService) {

        const vm = this;

        vm.inputId = "osm_search_" + String.CreateGuid();

        vm.currentMarker = null;
        vm.showSearch = $scope.model.config.showSearch ? Object.toBoolean($scope.model.config.showSearch) : false;
        vm.showCoordinates = $scope.model.config.showCoordinates ? Object.toBoolean($scope.model.config.showCoordinates) : false;
        vm.allowClear = $scope.model.config.allowClear ? Object.toBoolean($scope.model.config.allowClear) : true;

        vm.clearMarker = clearMarker;

        function onInit() {

            const initValue = $scope.model.value || $scope.model.config.defaultPosition || { marker : { latitude: 54.975556, longitude : -1.621667}, "boundingBox":{"southWestCorner":{"latitude":54.970495269313204,"longitude":-1.6278648376464846},"northEastCorner":{"latitude":54.97911600936982,"longitude":-1.609625816345215}}, zoom: 16};
            const tileLayer = $scope.model.config.tileLayer || 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
            const tileLayerOptions = { attribution: $scope.model.config.tileLayerAttribution };

            vm.map = L.map($element.find("[data-openstreetmap]")[0])
                .fitBounds(L.latLngBounds(L.latLng(initValue.boundingBox.southWestCorner.latitude, initValue.boundingBox.southWestCorner.longitude),
                    L.latLng(initValue.boundingBox.northEastCorner.latitude, initValue.boundingBox.northEastCorner.longitude)));

            L.tileLayer(tileLayer, tileLayerOptions).addTo(vm.map);

            vm.map.on('click', onMapClick);
            vm.map.on('moveend', updateModel);
            vm.map.on('zoomend', updateModel);

            vm.map.on('contextmenu', function() {
                if(vm.allowClear !== true && Object.toBoolean($scope.model.config.allowClear) !== true){
                    return;
                }
                clearMarker();
            });

            if (initValue.marker) {
                vm.currentMarker = L.marker(L.latLng(initValue.marker.latitude, initValue.marker.longitude), { draggable: true }).addTo(vm.map);
            }

            if (vm.currentMarker) {
                vm.currentMarker.on('dragend', updateModel);
            }

            if (vm.showSearch) {
                // Ensure DOM is ready
                userService.getCurrentUser().then(function (currentUser) {
                    $timeout(function () {
                        initAutocompleteSearch(currentUser.locale);
                    });
                });
            }
        }

        function clearMarker(e, skipUpdate) {
            if (vm.currentMarker){
                vm.currentMarker.remove(vm.map);
                vm.currentMarker = null;
            }

            if(!skipUpdate){
                updateModel();
            }


        }

        function onMapClick(e) {
            clearMarker(e, true);

            vm.map.setView(e.latlng);
            vm.currentMarker = L.marker(e.latlng, { draggable: true }).addTo(vm.map);

            if (vm.currentMarker) {
                vm.currentMarker.on('dragend', updateModel);
            }

            //updateModel();
        }

        function updateModel() {
            $timeout(function() {

                $scope.model.value = {};
                $scope.model.value.marker = {};

                $scope.model.value.zoom = vm.map.getZoom();

                if (!$scope.model.value.boundingBox) {
                    $scope.model.value.boundingBox = {};
                }
                if (!$scope.model.value.boundingBox.southWestCorner) {
                    $scope.model.value.boundingBox.southWestCorner = {};
                }
                if (!$scope.model.value.boundingBox.northEastCorner) {
                    $scope.model.value.boundingBox.northEastCorner = {};
                }

                const northEastCorner = vm.map.getBounds().getNorthEast();
                const southWestCorner = vm.map.getBounds().getSouthWest();

                $scope.model.value.boundingBox.northEastCorner.latitude = northEastCorner.lat;
                $scope.model.value.boundingBox.northEastCorner.longitude = northEastCorner.lng;
                $scope.model.value.boundingBox.southWestCorner.latitude = southWestCorner.lat;
                $scope.model.value.boundingBox.southWestCorner.longitude = southWestCorner.lng;

                if (vm.currentMarker) {
                    const marker = vm.currentMarker.getLatLng();

                    if (!$scope.model.value.marker) {
                        $scope.model.value.marker = {};
                    }

                    $scope.model.value.marker.latitude = marker.lat;
                    $scope.model.value.marker.longitude = marker.lng;

                } else {
                    $scope.model.value.marker = null;
                }
            }, 0);
        }

        function initAutocompleteSearch(language) {

            new Autocomplete(vm.inputId, {
                selectFirst: true,
                howManyCharacters: 2,

                // onSearch
                onSearch: ({ currentValue }) => {

                    const limit = 5;
                    const api = `https://nominatim.openstreetmap.org/search?format=geojson&limit=${limit}&q=${encodeURI(currentValue)}&accept-language=${language}`;

                    return new Promise((resolve) => {
                        fetch(api)
                            .then(response => response.json())
                            .then(data => {
                                resolve(data.features)
                            })
                            .catch(error => {
                                console.error(error);
                            })
                    })
                },
                // nominatim GeoJSON format parse this part turns json into the list of
                // records that appears when you type.
                onResults: ({ currentValue, matches, template }) => {
                    const regex = new RegExp(currentValue, 'gi');

                    // if the result returns 0 we
                    // show the no results element
                    return matches === 0 ? template : matches
                        .map((element) => {
                            return `
                              <li class="loupe">
                                <p>
                                  ${element.properties.display_name.replace(regex, (str) => `<b>${str}</b>`)}
                                </p>
                              </li> `;
                        }).join('');
                },

                // we add an action to enter or click
                onSubmit: ({ object }) => {

                    const { display_name, category } = object.properties;
                    const coords = object.geometry.coordinates;

                    // custom id for marker
                    const customId = Math.random();

                    // create marker and add to map
                    const marker = L.marker([coords[1], coords[0]], {
                        title: display_name,
                        id: customId,
                        draggable: true
                    })
                        .addTo(vm.map)
                        .bindPopup(display_name);

                    const bbox = object.bbox;
                    vm.map.fitBounds(L.latLngBounds(L.latLng(bbox[1], bbox[0]), L.latLng(bbox[3], bbox[2])));

                    // removing the previous marker
                    vm.map.eachLayer(function (layer) {
                        if (layer.options && layer.options.pane === "markerPane") {
                            if (layer.options.id !== customId) {
                                vm.map.removeLayer(layer);
                            }
                        }
                    });

                    vm.currentMarker = marker;

                    if (vm.currentMarker) {
                        vm.currentMarker.on('dragend', updateModel);
                    }

                    updateModel();
                },

                // get index and data from li element after
                // hovering over li with the mouse or using
                // arrow keys ↓ | ↑
                //onSelectedItem: ({ index, element, object }) => {
                //    console.log('onSelectedItem:', index, element, object);
                //},

                // the method presents no results element
                noResults: ({ currentValue, template }) => template(`<li>No results found: "${currentValue}"</li>`),
            });
        }

        onInit();
    }]);