// EventDetailsModal.js
import React, { useState, useEffect, useRef } from 'react';
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

const EventDetailsModal = ({ show, onHide, eventId, currentUserId, onEventDeleted, onRegistrationChange }) => {
    // Diagnostyczny console.log - możesz go usunąć, gdy wszystko będzie działać
    console.log('EventDetailsModal received props:', { show, eventId, currentUserId });

    const [event, setEvent] = useState(null);
    const [participants, setParticipants] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [isRegistered, setIsRegistered] = useState(false);
    const [isRegistering, setIsRegistering] = useState(false);
    const mapRef = useRef(null);
    const mapInstanceRef = useRef(null);

    const isUpcoming = event && new Date(event.startDate) > new Date();
    const canEdit = currentUserId && event && event.organizerId === currentUserId;
    const canRegister = currentUserId && isUpcoming && event && (!event.participantsLimit || (participants && participants.length < event.participantsLimit));
    const isFull = event && event.participantsLimit && participants && (participants.length >= event.participantsLimit);

    useEffect(() => {
        if (show && eventId) {
            console.log('EventDetailsModal: show is true and eventId is present. Fetching details for eventId:', eventId);
            fetchEventDetails();
            fetchParticipants();
        } else {
            console.log('EventDetailsModal: Not fetching details. Show:', show, 'EventId:', eventId);
        }

        // Cleanup map only when modal is explicitly hidden, or eventId changes while shown (might be rare)
        return () => {
            if (mapInstanceRef.current) {
                console.log('EventDetailsModal: Cleaning up map instance on hide or eventId change.');
                mapInstanceRef.current.remove();
                mapInstanceRef.current = null;
            }
        };
    }, [show, eventId]); // Dependency array

    useEffect(() => {
        if (currentUserId && participants && participants.length > 0) {
            const isUserRegistered = participants.some(p => p.id === currentUserId);
            setIsRegistered(isUserRegistered);
        } else if (!currentUserId) {
            setIsRegistered(false);
        } else if (participants && participants.length === 0 && isRegistered) {
            // Case: user was registered, but participants list became empty (e.g., event reset)
            setIsRegistered(false);
        }
    }, [participants, currentUserId, isRegistered]); // Added isRegistered to avoid potential stale closure issues if logic becomes more complex


    const fetchEventDetails = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await ApiService.get(`/api/events/${eventId}`);
            console.log('EventDetailsModal: Fetched event details data:', data);
            setEvent(data);
        } catch (err) {
            console.error('Error fetching event details:', err);
            setError('Nie udało się załadować szczegółów wydarzenia.');
            setEvent(null); // Clear event data on error
        } finally {
            setLoading(false);
        }
    };

    const fetchParticipants = async () => {
        try {
            const data = await ApiService.get(`/api/events/${eventId}/participants`);
            console.log('EventDetailsModal: Fetched participants data:', data);
            setParticipants(data || []);
        } catch (err) {
            console.error('Error fetching participants:', err);
            setParticipants([]); // Clear participants on error
        }
    };

    useEffect(() => {
        // Initialize map
        if (show && event && event.address && event.address.latitude && event.address.longitude && mapRef.current && !mapInstanceRef.current) {
            console.log('EventDetailsModal: Initializing map for:', event.name);
            const { latitude, longitude } = event.address;
            mapInstanceRef.current = L.map(mapRef.current).setView([latitude, longitude], 15);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            }).addTo(mapInstanceRef.current);

            L.marker([latitude, longitude])
                .addTo(mapInstanceRef.current)
                .bindPopup(`<strong>${event.name}</strong><br/>${event.address.street || ''} ${event.address.city || ''}`)
                .openPopup();
        } else if (show && event && mapRef.current && mapInstanceRef.current && event.address && event.address.latitude && event.address.longitude) {
            // Update map view if event data (like coordinates) changes while modal is open
            console.log('EventDetailsModal: Updating map view for:', event.name);
            mapInstanceRef.current.setView([event.address.latitude, event.address.longitude], 15);
            // Potentially update marker if it could change, though less common for just coordinates
        }

    }, [show, event]); // Rerun when show or event changes

    const handleDeleteEvent = async () => {
        if (!confirm('Czy na pewno chcesz usunąć to wydarzenie?')) return;
        try {
            await ApiService.delete(`/api/events/${eventId}`);
            if (onEventDeleted) onEventDeleted(eventId); // Przekaż eventId do funkcji zwrotnej
            onHide(); // Zamknij modal po usunięciu
        } catch (err) {
            console.error('Error deleting event:', err);
            alert('Nie udało się usunąć wydarzenia.');
        }
    };

    const formatDate = (dateString) => {
        if (!dateString) return 'Data nieznana';
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            weekday: 'long', day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit'
        });
    };

    const handleRegister = async () => {
        if (!currentUserId) {
            alert('Musisz się zalogować, aby zapisać się na wydarzenie.');
            return;
        }
        setIsRegistering(true);
        try {
            if (isRegistered) {
                await ApiService.delete(`/api/events/${eventId}/register`);
            } else {
                await ApiService.post(`/api/events/${eventId}/register`, {});
            }
            // Po udanej operacji API, odśwież uczestników, co zaktualizuje isRegistered
            await fetchParticipants();
            if (onRegistrationChange) {
                onRegistrationChange(); // Powiadom rodzica o zmianie (np. aby odświeżył listę wydarzeń)
            }
        } catch (error) {
            console.error('Error registering/unregistering for event:', error);
            const errorMessage = error.response?.data?.message ||
                (isRegistered ? 'Wystąpił błąd podczas wypisywania z wydarzenia.' : 'Wystąpił błąd podczas zapisywania na wydarzenie.');
            alert(errorMessage);
        } finally {
            setIsRegistering(false);
        }
    };

    // JSX
    // Jeśli modal nie ma być pokazany, zwróć null (powinno być na początku komponentu)
    if (!show) {
        return null;
    }

    // Jeśli ładuje, pokaż spinner
    if (loading) {
        return (
            <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1050 }}>
                <div className="modal-dialog modal-lg modal-dialog-centered">
                    <div className="modal-content">
                        <div className="modal-body text-center py-5">
                            <div className="spinner-border text-success" style={{width: '3rem', height: '3rem'}}></div>
                            <p className="mt-3">Ładowanie szczegółów wydarzenia...</p>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    // Jeśli jest błąd, pokaż komunikat błędu
    if (error && !event) { // Pokaż tylko jeśli nie ma danych eventu, aby uniknąć zasłaniania starych danych nowym błędem
        return (
            <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1050 }}>
                <div className="modal-dialog modal-lg">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title">Błąd</h5>
                            <button type="button" className="btn-close" onClick={onHide}></button>
                        </div>
                        <div className="modal-body">
                            <div className="alert alert-danger">{error}</div>
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" onClick={onHide}>Zamknij</button>
                            <button type="button" className="btn btn-primary" onClick={fetchEventDetails}>Spróbuj ponownie</button>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    // Jeśli nie ma danych wydarzenia (np. po błędzie, ale error został zresetowany)
    if (!event) {
        return ( // Można też zwrócić null lub inny placeholder
            <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1050 }}>
                <div className="modal-dialog modal-lg">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title">Informacja</h5>
                            <button type="button" className="btn-close" onClick={onHide}></button>
                        </div>
                        <div className="modal-body">
                            <p>Nie można załadować danych wydarzenia lub wydarzenie nie istnieje.</p>
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" onClick={onHide}>Zamknij</button>
                        </div>
                    </div>
                </div>
            </div>
        );
    }


    // Główny JSX modala
    return (
        <div
            className="modal show d-block"
            tabIndex="-1" // Dla dostępności i obsługi klawiatury (np. Esc)
            style={{
                backgroundColor: 'rgba(0,0,0,0.5)',
                zIndex: 1050, // Upewnij się, że jest nad innymi elementami
                position: 'fixed',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                overflowX: 'hidden', // Zapobiegaj poziomemu scrollowaniu
                overflowY: 'auto'    // Pozwól na pionowe scrollowanie, jeśli treść jest długa
            }}
            onClick={(e) => { if (e.target === e.currentTarget) onHide(); }} // Zamykanie po kliknięciu tła
        >
            <div className="modal-dialog modal-dialog-scrollable modal-lg" style={{margin: '1.75rem auto', maxWidth: '800px'}}>
                <div className="modal-content">
                    <div className="modal-header">
                        <h5 className="modal-title">
                            <i className="bi bi-calendar-event me-2 text-success"></i>
                            Szczegóły wydarzenia
                        </h5>
                        <button type="button" className="btn-close" onClick={onHide} aria-label="Close"></button>
                    </div>

                    <div className="modal-body">
                        {/* Komunikat błędu, jeśli wystąpił podczas ładowania, ale event jest nadal wyświetlany (stare dane) */}
                        {error && (
                            <div className="alert alert-warning d-flex align-items-center">
                                <i className="bi bi-exclamation-triangle me-2"></i>
                                {error}
                                <button className="btn btn-sm btn-outline-warning ms-auto" onClick={fetchEventDetails}>Odśwież dane</button>
                            </div>
                        )}
                        {/* Event Info */}
                        <div className="row">
                            <div className="col-12">
                                <h3 className="text-success mb-3">{event.name}</h3>

                                {event.description && (
                                    <div className="mb-4">
                                        <h6><i className="bi bi-info-circle me-2"></i>Opis</h6>
                                        <p className="text-muted" style={{whiteSpace: 'pre-wrap'}}>{event.description}</p>
                                    </div>
                                )}

                                {/* Date and Time */}
                                <div className="row mb-4">
                                    <div className="col-md-6 mb-2 mb-md-0">
                                        <h6><i className="bi bi-calendar-plus me-2 text-success"></i>Rozpoczęcie</h6>
                                        <p className="mb-0">{formatDate(event.startDate)}</p>
                                    </div>
                                    <div className="col-md-6">
                                        <h6><i className="bi bi-calendar-check me-2 text-success"></i>Zakończenie</h6>
                                        <p className="mb-0">{formatDate(event.endDate)}</p>
                                    </div>
                                </div>

                                {/* Organizer and Participants */}
                                <div className="row mb-4">
                                    <div className="col-md-6 mb-2 mb-md-0">
                                        <h6><i className="bi bi-person-badge me-2 text-success"></i>Organizator</h6>
                                        <p className="mb-0">{event.organizerName || 'Nieznany'}</p>
                                    </div>
                                    <div className="col-md-6">
                                        <h6><i className="bi bi-people-fill me-2 text-success"></i>Uczestnicy</h6>
                                        <p className="mb-0">
                                            {participants ? participants.length : 0}
                                            {event.participantsLimit > 0 && ` / ${event.participantsLimit}`} osób
                                        </p>
                                        {event.participantsLimit > 0 && (
                                            <div className="progress mt-1" style={{height: '8px', borderRadius: '4px'}}>
                                                <div
                                                    className="progress-bar bg-success"
                                                    role="progressbar"
                                                    style={{width: `${Math.min(((participants?.length || 0) / event.participantsLimit) * 100, 100)}%`}}
                                                    aria-valuenow={(participants?.length || 0)}
                                                    aria-valuemin="0"
                                                    aria-valuemax={event.participantsLimit}
                                                ></div>
                                            </div>
                                        )}
                                    </div>
                                </div>

                                {/* Participants List */}
                                {participants && participants.length > 0 && (
                                    <div className="mb-4">
                                        <h6><i className="bi bi-people me-2 text-success"></i>Lista uczestników (pierwszych 10)</h6>
                                        <div className="d-flex flex-wrap" style={{gap: '0.5rem'}}>
                                            {participants.slice(0, 10).map(participant => (
                                                <div key={participant.id} className="d-flex align-items-center bg-light rounded p-2" title={`${participant.firstName} ${participant.lastName}`}>
                                                    <img
                                                        src={ApiService.getImageUrl(participant.avatarPath) || '/img/default-avatar.png'}
                                                        alt={`${participant.firstName} ${participant.lastName}`}
                                                        className="rounded-circle me-2"
                                                        style={{width: '30px', height: '30px', objectFit: 'cover', border: '1px solid #eee'}}
                                                        onError={(e) => {
                                                            e.target.onerror = null; // Prevent infinite loop if default also fails
                                                            e.target.src = '/img/default-avatar.png';
                                                        }}
                                                    />
                                                    <small className="text-truncate" style={{maxWidth: '100px'}}>{participant.firstName} {participant.lastName.charAt(0)}.</small>
                                                </div>
                                            ))}
                                            {participants.length > 10 && (
                                                <div className="d-flex align-items-center bg-light rounded p-2">
                                                    <small className="text-muted">+{participants.length - 10} więcej</small>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                )}

                                {/* Location and Map */}
                                {event.address && (event.address.street || event.address.city) && ( // Pokaż sekcję adresu jeśli jest ulica LUB miasto
                                    <div className="mb-4">
                                        <h6><i className="bi bi-geo-alt me-2 text-success"></i>Lokalizacja</h6>
                                        <address className="mb-2" style={{fontStyle: 'normal', lineHeight: '1.6'}}>
                                            {event.address.street && <>{event.address.street}<br /></>}
                                            {event.address.postalCode && <>{event.address.postalCode} </>}
                                            {event.address.city && <>{event.address.city}</>}
                                            {event.address.country && <><br />{event.address.country}</>}
                                        </address>

                                        {event.address.latitude && event.address.longitude && (
                                            <div
                                                ref={mapRef}
                                                style={{height: '300px', borderRadius: '8px', border: '1px solid #dee2e6'}}
                                                className="event-map-container" // Dodana klasa dla potencjalnych styli
                                            ></div>
                                        )}
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>

                    <div className="modal-footer">
                        <button type="button" className="btn btn-secondary" onClick={onHide}>
                            <i className="bi bi-x-lg me-1"></i>Zamknij
                        </button>
                        {canEdit && isUpcoming && ( // Tylko właściciel może edytować/usuwać nadchodzące
                            <>
                                <button type="button" className="btn btn-outline-primary"> {/* TODO: Dodać onClick dla edycji */}
                                    <i className="bi bi-pencil me-1"></i>
                                    Edytuj
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-outline-danger"
                                    onClick={handleDeleteEvent}
                                    disabled={isRegistering} // Nie pozwól usuwać podczas operacji rejestracji
                                >
                                    <i className="bi bi-trash me-1"></i>
                                    Usuń
                                </button>
                            </>
                        )}
                        {/* Przycisk rejestracji/wypisania */}
                        {isUpcoming && !canEdit && currentUserId && ( // Tylko jeśli nie jest właścicielem i zalogowany
                            <button
                                type="button"
                                className={`btn ${isRegistered ? 'btn-warning' : 'btn-success'}`}
                                onClick={handleRegister}
                                disabled={isRegistering || (isFull && !isRegistered)} // Wyłącz jeśli pełne i nie jest zapisany
                            >
                                {isRegistering ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                                        Przetwarzanie...
                                    </>
                                ) : (isRegistered ?
                                        <><i className="bi bi-calendar-x me-1"></i>Wypisz się</> :
                                        (isFull ? <><i className="bi bi-slash-circle me-1"></i>Pełne</> : <><i className="bi bi-calendar-plus me-1"></i>Zapisz się</>)
                                )}
                            </button>
                        )}
                        {isUpcoming && !currentUserId && !isFull && ( // Jeśli nie zalogowany, a są miejsca
                            <button type="button" className="btn btn-success" onClick={handleRegister}>
                                <i className="bi bi-calendar-plus me-1"></i>Zapisz się (zaloguj)
                            </button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default EventDetailsModal;