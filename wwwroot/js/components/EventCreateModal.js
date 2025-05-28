// EventCreateModal.js - Modal for creating new events
import React, { useState, useRef, useEffect } from 'react';
import ApiService from '../services/api.js';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Fix for Leaflet default markers
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: markerIcon2x,
    iconUrl: markerIcon,
    shadowUrl: markerShadow,
});

const EventCreateModal = ({ show, onHide, onEventCreated }) => {
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        startDate: '',
        endDate: '',
        participantsLimit: '',
        address: {
            street: '',
            buildingNumber: '',
            city: '',
            postalCode: '',
            country: 'Polska',
            latitude: null,
            longitude: null
        }
    });
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const mapRef = useRef(null);
    const mapInstanceRef = useRef(null);
    const markerRef = useRef(null);

    useEffect(() => {
        if (show && mapRef.current && !mapInstanceRef.current) {
            // Initialize map centered on Poland
            mapInstanceRef.current = L.map(mapRef.current).setView([52.0693, 19.4803], 6);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            }).addTo(mapInstanceRef.current);

            mapInstanceRef.current.on('click', (e) => {
                const { lat, lng } = e.latlng;

                // Remove existing marker
                if (markerRef.current) {
                    mapInstanceRef.current.removeLayer(markerRef.current);
                }

                // Add new marker
                markerRef.current = L.marker([lat, lng]).addTo(mapInstanceRef.current);

                // Update coordinates in form
                setFormData(prev => ({
                    ...prev,
                    address: {
                        ...prev.address,
                        latitude: lat,
                        longitude: lng
                    }
                }));
            });
        }

        return () => {
            if (!show && mapInstanceRef.current) {
                mapInstanceRef.current.remove();
                mapInstanceRef.current = null;
                markerRef.current = null;
            }
        };
    }, [show]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setIsSubmitting(true);

        try {
            // Validate dates
            const startDate = new Date(formData.startDate);
            const endDate = new Date(formData.endDate);

            if (startDate >= endDate) {
                throw new Error('Data zakończenia musi być późniejsza niż data rozpoczęcia.');
            }

            if (startDate < new Date()) {
                throw new Error('Data rozpoczęcia nie może być w przeszłości.');
            }

            // Prepare event data - FIX dla BuildingNumber i DateTime UTC
            const eventData = {
                name: formData.name,
                description: formData.description,
                startDate: new Date(formData.startDate).toISOString(), // ← KONWERSJA NA UTC
                endDate: new Date(formData.endDate).toISOString(),     // ← KONWERSJA NA UTC
                participantsLimit: formData.participantsLimit ? parseInt(formData.participantsLimit) : 0,
                address: formData.address.latitude ? {
                    street: formData.address.street || '',
                    buildingNumber: formData.address.buildingNumber || '1',
                    apartmentNumber: '',
                    city: formData.address.city || '',
                    postalCode: formData.address.postalCode || '',
                    province: '',
                    country: formData.address.country || 'Polska',
                    latitude: formData.address.latitude,
                    longitude: formData.address.longitude
                } : null
            };

            console.log('Sending event data:', eventData);

            await ApiService.post('/api/events', eventData);

            onEventCreated();
            resetForm();
        } catch (err) {
            console.error('Error creating event:', err);
            setError(err.message || 'Wystąpił błąd podczas tworzenia wydarzenia.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        if (name.startsWith('address.')) {
            const addressField = name.split('.')[1];
            setFormData(prev => ({
                ...prev,
                address: {
                    ...prev.address,
                    [addressField]: value
                }
            }));
        } else {
            setFormData(prev => ({
                ...prev,
                [name]: value
            }));
        }
    };

    const resetForm = () => {
        setFormData({
            name: '',
            description: '',
            startDate: '',
            endDate: '',
            participantsLimit: '',
            address: {
                street: '',
                buildingNumber: '',
                city: '',
                postalCode: '',
                country: 'Polska',
                latitude: null,
                longitude: null
            }
        });
        setError('');
        if (markerRef.current && mapInstanceRef.current) {
            mapInstanceRef.current.removeLayer(markerRef.current);
            markerRef.current = null;
        }
    };

    const handleClose = () => {
        resetForm();
        onHide();
    };

    if (!show) return null;

    return (
        <div
            className="modal show d-block"
            style={{
                backgroundColor: 'rgba(0,0,0,0.5)',
                zIndex: 1050,
                position: 'fixed',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                overflowY: 'auto'
            }}
        >
            <div className="modal-dialog modal-lg" style={{margin: '20px auto', maxWidth: '800px'}}>
                <div className="modal-content">
                    <div className="modal-header">
                        <h5 className="modal-title">
                            <i className="bi bi-plus-circle me-2 text-success"></i>
                            Dodaj nowe wydarzenie
                        </h5>
                        <button type="button" className="btn-close" onClick={handleClose}></button>
                    </div>

                    <form onSubmit={handleSubmit}>
                        <div className="modal-body">
                            {error && (
                                <div className="alert alert-danger">
                                    <i className="bi bi-exclamation-triangle me-2"></i>
                                    {error}
                                </div>
                            )}

                            {/* Basic Information */}
                            <div className="row mb-3">
                                <div className="col-12">
                                    <label htmlFor="eventName" className="form-label">
                                        <i className="bi bi-card-text me-1"></i>
                                        Nazwa wydarzenia *
                                    </label>
                                    <input
                                        type="text"
                                        className="form-control"
                                        id="eventName"
                                        name="name"
                                        value={formData.name}
                                        onChange={handleInputChange}
                                        required
                                        placeholder="Np. Sprzątanie parku miejskiego"
                                    />
                                </div>
                            </div>

                            <div className="row mb-3">
                                <div className="col-12">
                                    <label htmlFor="eventDescription" className="form-label">
                                        <i className="bi bi-textarea-t me-1"></i>
                                        Opis wydarzenia
                                    </label>
                                    <textarea
                                        className="form-control"
                                        id="eventDescription"
                                        name="description"
                                        rows="4"
                                        value={formData.description}
                                        onChange={handleInputChange}
                                        placeholder="Opisz szczegóły wydarzenia, co będzie się działo, co uczestnicy powinni ze sobą zabrać..."
                                    ></textarea>
                                </div>
                            </div>

                            {/* Date and Time */}
                            <div className="row mb-3">
                                <div className="col-md-6">
                                    <label htmlFor="startDate" className="form-label">
                                        <i className="bi bi-calendar-plus me-1"></i>
                                        Data rozpoczęcia *
                                    </label>
                                    <input
                                        type="datetime-local"
                                        className="form-control"
                                        id="startDate"
                                        name="startDate"
                                        value={formData.startDate}
                                        onChange={handleInputChange}
                                        required
                                    />
                                </div>
                                <div className="col-md-6">
                                    <label htmlFor="endDate" className="form-label">
                                        <i className="bi bi-calendar-check me-1"></i>
                                        Data zakończenia *
                                    </label>
                                    <input
                                        type="datetime-local"
                                        className="form-control"
                                        id="endDate"
                                        name="endDate"
                                        value={formData.endDate}
                                        onChange={handleInputChange}
                                        required
                                    />
                                </div>
                            </div>

                            {/* Participants Limit */}
                            <div className="row mb-4">
                                <div className="col-md-6">
                                    <label htmlFor="participantsLimit" className="form-label">
                                        <i className="bi bi-people me-1"></i>
                                        Limit uczestników
                                    </label>
                                    <input
                                        type="number"
                                        className="form-control"
                                        id="participantsLimit"
                                        name="participantsLimit"
                                        value={formData.participantsLimit}
                                        onChange={handleInputChange}
                                        min="1"
                                        placeholder="Pozostaw puste dla braku limitu"
                                    />
                                    <div className="form-text">
                                        Zostaw puste jeśli nie chcesz ograniczać liczby uczestników
                                    </div>
                                </div>
                            </div>

                            {/* Location */}
                            <h6 className="mb-3">
                                <i className="bi bi-geo-alt me-2 text-success"></i>
                                Lokalizacja wydarzenia
                            </h6>

                            <div className="row mb-3">
                                <div className="col-md-8">
                                    <label htmlFor="street" className="form-label">Ulica</label>
                                    <input
                                        type="text"
                                        className="form-control"
                                        id="street"
                                        name="address.street"
                                        value={formData.address.street}
                                        onChange={handleInputChange}
                                        placeholder="Nazwa ulicy i numer"
                                    />
                                </div>
                                <div className="col-md-4">
                                    <label htmlFor="postalCode" className="form-label">Kod pocztowy</label>
                                    <input
                                        type="text"
                                        className="form-control"
                                        id="postalCode"
                                        name="address.postalCode"
                                        value={formData.address.postalCode}
                                        onChange={handleInputChange}
                                        placeholder="00-000"
                                    />
                                </div>
                            </div>

                            <div className="row mb-3">
                                <div className="col-md-6">
                                    <label htmlFor="city" className="form-label">Miasto *</label>
                                    <input
                                        type="text"
                                        className="form-control"
                                        id="city"
                                        name="address.city"
                                        value={formData.address.city}
                                        onChange={handleInputChange}
                                        required
                                        placeholder="Nazwa miasta"
                                    />
                                </div>
                                <div className="col-md-6">
                                    <label htmlFor="country" className="form-label">Kraj</label>
                                    <input
                                        type="text"
                                        className="form-control"
                                        id="country"
                                        name="address.country"
                                        value={formData.address.country}
                                        onChange={handleInputChange}
                                        placeholder="Kraj"
                                    />
                                </div>
                            </div>

                            {/* Map */}
                            <div className="mb-3">
                                <label className="form-label">
                                    <i className="bi bi-map me-1"></i>
                                    Dokładna lokalizacja (kliknij na mapie)
                                </label>
                                <div
                                    ref={mapRef}
                                    style={{height: '300px', borderRadius: '8px'}}
                                    className="border"
                                ></div>
                                <div className="form-text">
                                    Kliknij na mapie, aby zaznaczyć dokładną lokalizację wydarzenia
                                    {formData.address.latitude && (
                                        <span className="text-success ms-2">
                                            <i className="bi bi-check-circle me-1"></i>
                                            Lokalizacja wybrana
                                        </span>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" onClick={handleClose}>
                                Anuluj
                            </button>
                            <button
                                type="submit"
                                className="btn btn-success"
                                disabled={isSubmitting}
                            >
                                {isSubmitting ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2"></span>
                                        Tworzenie...
                                    </>
                                ) : (
                                    <>
                                        <i className="bi bi-plus-circle me-2"></i>
                                        Utwórz wydarzenie
                                    </>
                                )}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default EventCreateModal;