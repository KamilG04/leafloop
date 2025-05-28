// EventsList.js
import React, { useState, useEffect, useCallback } from 'react';
import ApiService from '../services/api.js';
import EventCard from './EventCard.js';
import EventCreateModal from './EventCreateModal.js';
import EventDetailsModal from './EventDetailsModal.js'; // Zaimportuj EventDetailsModal

// Funkcja pomocnicza do pobierania ID użytkownika z tokenu - BEZ HOOKÓW!
const getUserIdFromToken = () => {
    try {
        const token = localStorage.getItem('jwt_token') ||
            document.cookie.split('; ').find(row => row.startsWith('jwt_token='))?.split('=')[1];

        if (!token) {
            // console.log('Helper: No JWT token found'); // Logowanie może być w komponencie
            return null;
        }
        const payload = JSON.parse(atob(token.split('.')[1]));
        const userIdStr = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
        const result = userIdStr ? parseInt(userIdStr, 10) : null;
        return result;
    } catch (error) {
        console.error('Helper: Error decoding token or getting user ID:', error);
        return null; // Lub rzuć błąd, aby komponent go obsłużył
    }
};

const EventsList = () => {
    const [events, setEvents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [activeTab, setActiveTab] = useState('upcoming');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [currentUserId, setCurrentUserId] = useState(null); // ID zalogowanego użytkownika

    // Stany do zarządzania EventDetailsModal
    const [showDetailsModal, setShowDetailsModal] = useState(false);
    const [selectedEventId, setSelectedEventId] = useState(null);

    useEffect(() => {
        console.log('EventsList: Component mounted, attempting to get user ID...');
        try {
            const userId = getUserIdFromToken(); // Wywołaj poprawioną funkcję
            console.log('EventsList: User ID from token:', userId);
            setCurrentUserId(userId);
        } catch (err) {
            console.error('EventsList: Error processing user ID on mount:', err);
            setCurrentUserId(null);
        }
        // Początkowe pobranie wydarzeń, niezależnie od userId.
        // Można dostosować, jeśli wydarzenia zależą od obecności userId.
        fetchEvents('upcoming');
    }, []); // Pusta tablica zależności - uruchamia się raz po zamontowaniu

    const fetchEvents = useCallback(async (type = 'upcoming') => {
        console.log('EventsList: Fetching events of type:', type);
        setLoading(true);
        setError(null);
        try {
            let endpoint = '/api/events';
            if (type === 'upcoming') {
                endpoint = '/api/events/upcoming?count=20';
            } else if (type === 'past') {
                endpoint = '/api/events/past?count=20';
            } // Jeśli type === 'all', użyje '/api/events'

            const data = await ApiService.get(endpoint);
            console.log('EventsList: Fetched events:', data?.length || 0);
            setEvents(data || []);
        } catch (err) {
            console.error('Error fetching events:', err);
            setError('Nie udało się załadować wydarzeń. Spróbuj ponownie.');
        } finally {
            setLoading(false);
        }
    }, []);

    const handleTabChange = (type) => {
        setActiveTab(type);
        fetchEvents(type);
    };

    const handleEventCreated = () => {
        setShowCreateModal(false);
        fetchEvents(activeTab); // Odśwież aktywną zakładkę
    };

    const handleEventDeleted = (deletedEventId) => {
        setEvents(prevEvents => prevEvents.filter(event => event.id !== deletedEventId));
        // Opcjonalnie zamknij modal szczegółów, jeśli usunięte wydarzenie było wyświetlane
        if (selectedEventId === deletedEventId) {
            closeDetailsModal();
        }
    };

    const handleRegistrationChange = () => {
        // Ta funkcja jest wywoływana przez EventDetailsModal po rejestracji/wyrejestrowaniu.
        // Możemy chcieć ponownie pobrać szczegóły konkretnego wydarzenia, listę uczestników,
        // lub całą listę wydarzeń, jeśli np. licznik uczestników jest na kartach.
        // Dla uproszczenia, pobierzmy ponownie aktualną listę.
        console.log('EventsList: Registration changed, re-fetching events for tab:', activeTab);
        fetchEvents(activeTab);
    };

    // Funkcje do kontrolowania EventDetailsModal
    const openDetailsModal = (eventId) => {
        console.log('EventsList: Opening details modal for event ID:', eventId);
        setSelectedEventId(eventId);
        setShowDetailsModal(true);
    };

    const closeDetailsModal = () => {
        console.log('EventsList: Closing details modal.');
        setShowDetailsModal(false);
        setSelectedEventId(null);
    };

    if (loading && events.length === 0) {
        return (
            <div className="container mt-4">
                <div className="d-flex justify-content-center py-5">
                    <div className="spinner-border text-success" role="status" style={{width: "3rem", height: "3rem"}}>
                        <span className="visually-hidden">Ładowanie wydarzeń...</span>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="container mt-4">
            {/* Nagłówek */}
            <div className="row mb-4">
                <div className="col">
                    <div className="d-flex justify-content-between align-items-center">
                        <div>
                            <h1 className="mb-1">
                                <i className="bi bi-calendar-event me-2 text-success"></i>
                                Wydarzenia
                            </h1>
                            <p className="text-muted">Odkryj lokalne wydarzenia ekologiczne w Twojej okolicy</p>
                        </div>
                        {currentUserId && (
                            <button
                                className="btn btn-success btn-lg"
                                onClick={() => setShowCreateModal(true)}
                            >
                                <i className="bi bi-plus-circle me-2"></i>
                                Dodaj Wydarzenie
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Zakładki nawigacyjne */}
            <div className="row mb-4">
                <div className="col">
                    <ul className="nav nav-pills justify-content-center">
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'upcoming' ? 'active' : ''}`}
                                onClick={() => handleTabChange('upcoming')}
                            >
                                <i className="bi bi-calendar-plus me-1"></i>
                                Nadchodzące
                            </button>
                        </li>
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'all' ? 'active' : ''}`}
                                onClick={() => handleTabChange('all')}
                            >
                                <i className="bi bi-calendar3 me-1"></i>
                                Wszystkie
                            </button>
                        </li>
                        <li className="nav-item">
                            <button
                                className={`nav-link ${activeTab === 'past' ? 'active' : ''}`}
                                onClick={() => handleTabChange('past')}
                            >
                                <i className="bi bi-calendar-check me-1"></i>
                                Przeszłe
                            </button>
                        </li>
                    </ul>
                </div>
            </div>

            {/* Komunikat błędu */}
            {error && (
                <div className="alert alert-danger d-flex align-items-center mb-4">
                    <i className="bi bi-exclamation-triangle-fill me-2"></i>
                    {error}
                    <button className="btn btn-outline-danger btn-sm ms-auto" onClick={() => fetchEvents(activeTab)}>
                        <i className="bi bi-arrow-clockwise me-1"></i>
                        Spróbuj ponownie
                    </button>
                </div>
            )}

            {/* Siatka wydarzeń */}
            {events.length > 0 ? (
                <div className="row g-4">
                    {events.map(event => (
                        <div key={event.id} className="col-md-6 col-lg-4">
                            <EventCard
                                event={event}
                                currentUserId={currentUserId}
                                // Przekaż funkcję do otwierania modala do EventCard
                                onOpenDetails={() => openDetailsModal(event.id)}
                            />
                        </div>
                    ))}
                </div>
            ) : (
                <div className="text-center py-5">
                    <i className="bi bi-calendar-x text-muted" style={{fontSize: '4rem'}}></i>
                    <h3 className="mt-3 text-muted">Brak wydarzeń</h3>
                    <p className="text-muted">
                        {activeTab === 'upcoming' && 'Nie ma nadchodzących wydarzeń.'}
                        {activeTab === 'past' && 'Nie ma przeszłych wydarzeń.'}
                        {activeTab === 'all' && 'Nie ma żadnych wydarzeń.'}
                    </p>
                    {currentUserId && (
                        <button
                            className="btn btn-success mt-3"
                            onClick={() => setShowCreateModal(true)}
                        >
                            <i className="bi bi-plus-circle me-2"></i>
                            Stwórz pierwsze wydarzenie
                        </button>
                    )}
                </div>
            )}

            {/* Nakładka ładowania podczas odświeżania */}
            {loading && events.length > 0 && (
                <div className="position-fixed top-50 start-50 translate-middle" style={{zIndex: 1050}}>
                    <div className="spinner-border text-success" role="status">
                        <span className="visually-hidden">Odświeżanie...</span>
                    </div>
                </div>
            )}

            {/* Modal tworzenia wydarzenia */}
            <EventCreateModal
                show={showCreateModal}
                onHide={() => setShowCreateModal(false)}
                onEventCreated={handleEventCreated}
            />

            {/* Modal szczegółów wydarzenia - teraz renderowany i kontrolowany przez EventsList */}
            {showDetailsModal && selectedEventId && (
                <EventDetailsModal
                    show={showDetailsModal}
                    onHide={closeDetailsModal}
                    eventId={selectedEventId}
                    currentUserId={currentUserId}
                    onEventDeleted={() => {
                        // Po usunięciu wydarzenia z modala, aktualizujemy listę i zamykamy modal
                        handleEventDeleted(selectedEventId); // selectedEventId to ID aktualnie wyświetlanego wydarzenia
                        // closeDetailsModal(); // handleEventDeleted może już to robić, jeśli jest taka potrzeba
                    }}
                    onRegistrationChange={handleRegistrationChange} // Przekaż tę funkcję
                />
            )}
        </div>
    );
};

export default EventsList;