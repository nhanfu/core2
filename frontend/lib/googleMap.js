import { EditableComponent } from './editableComponent.js';
import { Html } from './utils/html.js';
import { Component, EventType } from './models/';
import { Spinner } from './spinner.js';
import { Client } from './clients/client.js';
import { Utils } from './utils/utils.js';

export class GoogleMap extends EditableComponent {
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.DefaultValue = '';
        this.map = null;
        this.marker = null;
        this.infoWindow = null;
        this.geocoder = null;
        this.defaultZoom = this.Meta.Precision || 15;
        this.defaultCenter = { lat: 10.8231, lng: 106.6297 }; // Default to Ho Chi Minh City
        this.mapHeight = this.Meta.Height || '400px';
        this.mapWidth = this.Meta.Width || '100%';
        this.scriptLoaded = false;
        this.searchBox = null;
        this.latField = this.Meta.LatField || 'Latitude';
        this.lngField = this.Meta.LngField || 'Longitude';
        this.addressField = this.Meta.AddressField || 'Address';
        this.mapType = this.Meta.MapType || 'roadmap';
    }

    /**
     * Get Google Maps API key from environment or component settings
     * Falls back to anonymous access if no key is available
     * @returns {Promise<string>} The API key or empty string for anonymous access
     */
    async getApiKey() {
        // First try to get API key from the component metadata
        if (this.Meta.ApiKey && this.Meta.ApiKey.trim() !== '') {
            return this.Meta.ApiKey;
        }

        if (localStorage.getItem('GOOGLE_MAPS_API_KEY') != null) {
            return localStorage.getItem('GOOGLE_MAPS_API_KEY');
        }
        
        // Try to get API key from environment settings
        try {
            const response = await Client.Instance.GetConfig('GOOGLE_MAPS_API_KEY');
            if (response && response.value) {
                return response.value;
            }
        } catch (error) {
            console.warn('Could not retrieve Google Maps API key from server config:', error);
        }
        
        // Return empty string for anonymous access
        return '';
    }

    Render() {
        this.SetDefaultVal();
        
        // Create container for map
        Html.Take(this.ParentElement).Div.ClassName("google-map-wrapper").Style(`height: ${this.mapHeight}; width: ${this.mapWidth}; position: relative;`);
        this.Element = Html.Context;
        
        // Create search box
        if (this.Meta.ShowSearch !== false) {
            Html.Instance.Div.ClassName("map-search-box").Style("position: absolute; top: 10px; left: 10px; z-index: 1; width: 70%; max-width: 400px;")
                .Input.ClassName("form-control").PlaceHolder("Search location...").Style("width: 100%; padding: 8px; border-radius: 2px; box-shadow: 0 2px 6px rgba(0,0,0,0.3);");
            this.searchInput = Html.Context;
            Html.Instance.End.End.Render();
        }
        
        // Create map container
        Html.Instance.Div.Id(`map-${this.Meta.Id}`).Style(`height: 100%; width: 100%;`);
        this.mapContainer = Html.Context;
        Html.Instance.End.Render();
        
        // Load Google Maps API
        this.loadGoogleMapsScript();
    }

    async loadGoogleMapsScript() {
        if (window.google && window.google.maps) {
            this.scriptLoaded = true;
            this.initMap();
            return;
        }

        // Check if script is already being loaded
        const existingScript = document.getElementById('google-maps-script');
        if (existingScript) {
            existingScript.addEventListener('load', () => {
                this.scriptLoaded = true;
                this.initMap();
            });
            return;
        }

        Spinner.AppendTo();
        
        try {
            // Get API key from environment or component settings
            const apiKey = await this.getApiKey();
            const keyParam = apiKey ? `key=${apiKey}&` : '';
            
            const script = document.createElement('script');
            script.id = 'google-maps-script';
            script.src = `https://maps.googleapis.com/maps/api/js?${keyParam}libraries=places`;
            script.defer = true;
            script.async = true;
            
            script.addEventListener('load', () => {
                this.scriptLoaded = true;
                this.initMap();
                Spinner.Hide();
            });
            
            script.addEventListener('error', (e) => {
                console.error('Failed to load Google Maps API:', e);
                Spinner.Hide();
                
                // Show user-friendly error message
                if (apiKey) {
                    Html.Take(this.mapContainer).Clear().Div.ClassName("alert alert-danger").Text("Failed to load Google Maps API. Please check your API key configuration.");
                } else {
                    Html.Take(this.mapContainer).Clear().Div.ClassName("alert alert-warning").Text("Failed to load Google Maps API in anonymous mode. Usage limits may have been exceeded.");
                }
            });
            
            document.head.appendChild(script);
        } catch (error) {
            console.error('Error loading Google Maps API:', error);
            Spinner.Hide();
            Html.Take(this.mapContainer).Clear().Div.ClassName("alert alert-danger").Text("Error loading Google Maps API: " + error.message);
        }
    }

    initMap() {
        if (!this.scriptLoaded || !window.google || !window.google.maps) {
            console.warn('Google Maps API not loaded yet');
            return;
        }

        // Initialize geocoder
        this.geocoder = new google.maps.Geocoder();
        
        // Get initial coordinates
        let initialLat = this.Entity[this.latField] !== undefined ? parseFloat(this.Entity[this.latField]) : null;
        let initialLng = this.Entity[this.lngField] !== undefined ? parseFloat(this.Entity[this.lngField]) : null;
        
        // Set initial center
        const center = (initialLat && initialLng && !isNaN(initialLat) && !isNaN(initialLng))
            ? { lat: initialLat, lng: initialLng }
            : this.defaultCenter;
        
        // Set map type
        let mapTypeId;
        switch (this.mapType.toLowerCase()) {
            case 'satellite':
                mapTypeId = google.maps.MapTypeId.SATELLITE;
                break;
            case 'hybrid':
                mapTypeId = google.maps.MapTypeId.HYBRID;
                break;
            case 'terrain':
                mapTypeId = google.maps.MapTypeId.TERRAIN;
                break;
            default:
                mapTypeId = google.maps.MapTypeId.ROADMAP;
        }
        
        // Create map
        this.map = new google.maps.Map(this.mapContainer, {
            center: center,
            zoom: this.defaultZoom,
            mapTypeId: mapTypeId,
            mapTypeControl: true,
            streetViewControl: true,
            fullscreenControl: true
        });

        // Add marker if we have coordinates
        if (initialLat && initialLng && !isNaN(initialLat) && !isNaN(initialLng)) {
            this.addMarker(center);
            
            // Try to get address for initial coordinates if not already set
            if (!this.Entity[this.addressField] && this.geocoder) {
                this.geocoder.geocode({ location: center }, (results, status) => {
                    if (status === 'OK' && results[0]) {
                        const address = results[0].formatted_address;
                        this.Entity[this.addressField] = address;
                        this.addMarker(center, address);
                    }
                });
            }
        }

        // Initialize search box if it exists
        if (this.searchInput) {
            this.initSearchBox();
        }

        // Set up click event on map
        if (!this.Disabled) {
            this.map.addListener('click', (event) => {
                const latLng = event.latLng;
                this.updateLocationData(latLng);
            });
        }

        // Dispatch map initialized event
        this.DispatchEvent(this.Meta.Events, 'mapinitialized', this, this.map);
    }

    initSearchBox() {
        if (!this.scriptLoaded || !window.google || !window.google.maps || !this.map) {
            return;
        }

        try {
            this.searchBox = new google.maps.places.SearchBox(this.searchInput);
            
            // Bias search results to current map view
            this.map.addListener('bounds_changed', () => {
                this.searchBox.setBounds(this.map.getBounds());
            });

            // Listen for search box selection
            this.searchBox.addListener('places_changed', () => {
                const places = this.searchBox.getPlaces();
                if (places.length === 0) return;

                const place = places[0];
                if (!place.geometry || !place.geometry.location) return;

                // Set the map view to the selected location
                if (place.geometry.viewport) {
                    this.map.fitBounds(place.geometry.viewport);
                } else {
                    this.map.setCenter(place.geometry.location);
                    this.map.setZoom(this.defaultZoom);
                }

                // Update marker and location data
                this.updateLocationData(place.geometry.location, place.formatted_address);
            });
        } catch (error) {
            console.error('Error initializing search box:', error);
            // Hide search box if Places API is not available
            if (this.searchInput && this.searchInput.parentElement) {
                this.searchInput.parentElement.style.display = 'none';
            }
        }
    }

    addMarker(position, address) {
        if (!this.map) return;
        
        // Remove existing marker
        if (this.marker) {
            this.marker.setMap(null);
        }
        
        // Create new marker
        this.marker = new google.maps.Marker({
            position: position,
            map: this.map,
            draggable: !this.Disabled,
            animation: google.maps.Animation.DROP
        });

        // Add info window if address is provided
        if (address) {
            if (this.infoWindow) {
                this.infoWindow.close();
            }
            
            this.infoWindow = new google.maps.InfoWindow({
                content: `<div>${Utils.EncodeHtml(address)}</div>`
            });
            
            this.infoWindow.open(this.map, this.marker);
            this.marker.addListener('click', () => {
                this.infoWindow.open(this.map, this.marker);
            });
        }

        // Add drag event listener for marker
        if (!this.Disabled) {
            this.marker.addListener('dragend', (event) => {
                const latLng = event.latLng;
                this.updateLocationData(latLng);
            });
        }

        return this.marker;
    }

    updateLocationData(latLng, address) {
        if (!latLng) return;
        
        const lat = latLng.lat();
        const lng = latLng.lng();
        
        // Update entity with new coordinates
        if (this.Entity) {
            this.Entity[this.latField] = lat;
            this.Entity[this.lngField] = lng;
            
            // If address is provided, update it directly
            if (address) {
                this.Entity[this.addressField] = address;
                this.addMarker({ lat, lng }, address);
                this.Dirty = true;
                this.DispatchEvent(this.Meta.Events, EventType.Change, this, this.Entity);
                return;
            }
            
            // Otherwise geocode the coordinates to get address
            if (this.geocoder) {
                this.geocoder.geocode({ location: { lat, lng } }, (results, status) => {
                    if (status === 'OK' && results[0]) {
                        const address = results[0].formatted_address;
                        this.Entity[this.addressField] = address;
                        this.addMarker({ lat, lng }, address);
                    } else {
                        this.addMarker({ lat, lng });
                    }
                    
                    this.Dirty = true;
                    this.DispatchEvent(this.Meta.Events, EventType.Change, this, this.Entity);
                });
            } else {
                this.addMarker({ lat, lng });
                this.Dirty = true;
                this.DispatchEvent(this.Meta.Events, EventType.Change, this, this.Entity);
            }
        }
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        if (!this.map) {
            if (this.scriptLoaded) {
                this.initMap();
            }
            return;
        }
        
        let lat = this.Entity[this.latField] !== undefined ? parseFloat(this.Entity[this.latField]) : null;
        let lng = this.Entity[this.lngField] !== undefined ? parseFloat(this.Entity[this.lngField]) : null;
        let address = this.Entity[this.addressField];
        
        if (lat && lng && !isNaN(lat) && !isNaN(lng)) {
            const position = { lat, lng };
            this.map.setCenter(position);
            this.addMarker(position, address);
        }
    }

    SetDisableUI(value) {
        // If map is already initialized
        if (this.map && this.marker) {
            // Make marker draggable or not based on disabled state
            this.marker.setDraggable(!value);
            
            // Disable/enable click event
            if (value) {
                google.maps.event.clearListeners(this.map, 'click');
            } else {
                this.map.addListener('click', (event) => {
                    const latLng = event.latLng;
                    this.updateLocationData(latLng);
                });
            }
        }
        
        // Disable/enable search input
        if (this.searchInput) {
            this.searchInput.disabled = value;
        }
    }

    GetValueText() {
        const lat = this.Entity[this.latField];
        const lng = this.Entity[this.lngField];
        const address = this.Entity[this.addressField];
        
        if (address) {
            return address;
        } else if (lat && lng) {
            return `${lat}, ${lng}`;
        }
        
        return '';
    }
}