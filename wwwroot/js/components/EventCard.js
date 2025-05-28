// EventCard.js - Individual event card component
import React, { useState, useEffect, useCallback } from 'react';
import ApiService from '../services/api.js';
import EventDetailsModal from './EventDetailsModal.js';
import { getCurrentUserId } from '../utils/auth.js'; // ← DODANY IMPORT

const EventCard = ({ event, currentUserId, onEventDeleted }) => {
    const [showDetails, setShowDetails] = useState(false);
    const [isRegistering, setIsRegistering] = useState(false);
    const [isRegistered, setIsRegistered] = useState(false);
    const [participantsCount, setParticipantsCount] = useState(event.currentParticipantsCount || 0);

    const checkRegistrationStatus = useCallback(async () => {
        if (!currentUserId || !event.id) {
            console.log('EventCard: Skipping registration check - no user or event ID');
            return;
        }

        console.log('EventCard: Checking registration for user', currentUserId, 'event', event.id); // DEBUG

        try {
            const participants = await ApiService.get(`/api/events/${event.id}/participants`);
            const isUserRegistered = participants.some(p => p.id === currentUserId);
            console.log('EventCard: Registration status:', isUserRegistered); // DEBUG
            setIsRegistered(isUserRegistered);
            setParticipantsCount(participants.length);
        } catch (error) {
            console.error('Error checking registration status:', error);
        }
    }, [currentUserId, event.id]);

    // Check registration status only once when component mounts
    useEffect(() => {
        checkRegistrationStatus();
    }, [checkRegistrationStatus]);

    const formatDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const isUpcoming = new Date(event.startDate) > new Date();
    const isPast = new Date(event.endDate) < new Date();
    const isOngoing = new Date() >= new Date(event.startDate) && new Date() <= new Date(event.endDate);

    const handleRegister = async (e) => {
        e.stopPropagation();
        if (!currentUserId) {
            alert('Musisz się zalogować, aby zapisać się na wydarzenie.');
            return;
        }

        setIsRegistering(true);
        try {
            if (isRegistered) {
                await ApiService.delete(`/api/events/${event.id}/register`);
                setIsRegistered(false);
                setParticipantsCount(prev => prev - 1);
            } else {
                await ApiService.post(`/api/events/${event.id}/register`, {});
                setIsRegistered(true);
                setParticipantsCount(prev => prev + 1);
            }
        } catch (error) {
            console.error('Error registering for event:', error);
            if (error.response?.status === 400) {
                alert('Nie można się zapisać na to wydarzenie. Może być już wypełnione lub zakończone.');
            } else {
                alert('Wystąpił błąd podczas zapisywania na wydarzenie.');
            }
        } finally {
            setIsRegistering(false);
        }
    };

    const canRegister = currentUserId && isUpcoming && (!event.participantsLimit || participantsCount < event.participantsLimit);
    const isFull = event.participantsLimit && participantsCount >= event.participantsLimit;

    // Callback to refresh data when modal is closed
    const handleModalClose = () => {
        setShowDetails(false);
        // Refresh registration status when modal closes
        if (currentUserId) {
            checkRegistrationStatus();
        }
    };

    return (
        <>
            <div
                className="card h-100 event-card shadow-sm"
                style={{cursor: 'pointer'}}
                onClick={() => setShowDetails(true)}
            >
                {/* Status Badge */}
                <div className="position-absolute top-0 end-0 m-3" style={{zIndex: 5}}>
                    {isOngoing && (
                        <span className="badge bg-success">
                            <i className="bi bi-broadcast me-1"></i>
                            Trwa teraz
                        </span>
                    )}
                    {isPast && (
                        <span className="badge bg-secondary">
                            <i className="bi bi-check-circle me-1"></i>
                            Zakończone
                        </span>
                    )}
                    {isFull && isUpcoming && (
                        <span className="badge bg-warning text-dark">
                            <i className="bi bi-people-fill me-1"></i>
                            Wypełnione
                        </span>
                    )}
                    {isRegistered && (
                        <span className="badge bg-info text-dark ms-1">
                            <i className="bi bi-check2-circle me-1"></i>
                            Zapisany
                        </span>
                    )}
                </div>

                <div className="card-body">
                    <h5 className="card-title text-success fw-bold">{event.name}</h5>

                    <p className="card-text text-muted small mb-3" style={{
                        display: '-webkit-box',
                        WebkitLineClamp: 3,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden'
                    }}>
                        {event.description || 'Brak opisu wydarzenia.'}
                    </p>

                    {/* Date and Time */}
                    <div className="mb-3">
                        <div className="d-flex align-items-center mb-1">
                            <i className="bi bi-calendar-event text-success me-2"></i>
                            <small className="text-muted">
                                <strong>Rozpoczęcie:</strong> {formatDate(event.startDate)}
                            </small>
                        </div>
                        <div className="d-flex align-items-center">
                            <i className="bi bi-calendar-check text-success me-2"></i>
                            <small className="text-muted">
                                <strong>Zakończenie:</strong> {formatDate(event.endDate)}
                            </small>
                        </div>
                    </div>

                    {/* Participants Info */}
                    <div className="d-flex align-items-center justify-content-between mb-3">
                        <div className="d-flex align-items-center">
                            <i className="bi bi-people-fill text-success me-2"></i>
                            <span className="text-muted small">
                                {participantsCount}
                                {event.participantsLimit && ` / ${event.participantsLimit}`} uczestników
                            </span>
                        </div>
                        {event.participantsLimit && (
                            <div className="progress" style={{width: '60px', height: '6px'}}>
                                <div
                                    className="progress-bar bg-success"
                                    style={{width: `${Math.min((participantsCount / event.participantsLimit) * 100, 100)}%`}}
                                ></div>
                            </div>
                        )}
                    </div>

                    {/* Organizer */}
                    <div className="d-flex align-items-center mb-3">
                        <i className="bi bi-person-badge text-success me-2"></i>
                        <small className="text-muted">
                            Organizator: <strong>{event.organizerName || 'Nieznany'}</strong>
                        </small>
                    </div>
                </div>

                {/* Action Buttons */}
                <div className="card-footer bg-transparent border-0 pt-0">
                    <div className="d-grid gap-2">
                        {canRegister && (
                            <button
                                className={`btn ${isRegistered ? 'btn-outline-danger' : 'btn-success'}`}
                                onClick={handleRegister}
                                disabled={isRegistering}
                            >
                                {isRegistering ? (
                                    <span className="spinner-border spinner-border-sm me-2"></span>
                                ) : (
                                    <i className={`bi ${isRegistered ? 'bi-x-circle' : 'bi-check-circle'} me-2`}></i>
                                )}
                                {isRegistered ? 'Wypisz się' : 'Zapisz się'}
                            </button>
                        )}
                        {!canRegister && currentUserId && isUpcoming && isFull && (
                            <button className="btn btn-outline-secondary" disabled>
                                <i className="bi bi-people-fill me-2"></i>
                                Brak miejsc
                            </button>
                        )}
                        {!currentUserId && isUpcoming && (
                            <button className="btn btn-outline-primary" disabled>
                                <i className="bi bi-box-arrow-in-right me-2"></i>
                                Zaloguj się aby się zapisać
                            </button>
                        )}
                        <button
                            className="btn btn-outline-success btn-sm"
                            onClick={(e) => {
                                e.stopPropagation();
                                setShowDetails(true);
                            }}
                        >
                            <i className="bi bi-eye me-1"></i>
                            Zobacz szczegóły
                        </button>
                    </div>
                </div>
            </div>

            <EventDetailsModal
                show={showDetails}
                onHide={handleModalClose}
                eventId={event.id}
                currentUserId={currentUserId}
                onEventDeleted={onEventDeleted}
                onRegistrationChange={checkRegistrationStatus}
            />
        </>
    );
};

export default EventCard;