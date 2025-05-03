// Ścieżka: wwwroot/js/components/TransactionDetails.js
import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';
import { getCurrentUserId } from '../utils/auth.js'; // Potrzebne do identyfikacji usera

// Mały komponent do formatowania daty
const FormattedDate = ({ date }) => {
    if (!date) return <span className="text-muted">--</span>;
    return <>{new Date(date).toLocaleString('pl-PL', { dateStyle: 'medium', timeStyle: 'short' })}</>;
};

// Mały komponent do wyświetlania pojedynczej wiadomości
const MessageItem = ({ message, currentUserId }) => {
    const isCurrentUserSender = message.senderId === currentUserId;
    return (
        <div className={`d-flex mb-2 ${isCurrentUserSender ? 'justify-content-end' : ''}`}>
            <div className={`card shadow-sm ${isCurrentUserSender ? 'bg-success bg-opacity-10' : 'bg-light'}`} style={{ maxWidth: '75%' }}>
                <div className="card-body p-2">
                    <p className="card-text mb-1">{message.content}</p>
                    <small className="text-muted d-block text-end">
                        {message.senderName || `User ${message.senderId}`} - <FormattedDate date={message.sentDate} />
                    </small>
                </div>
            </div>
        </div>
    );
};

// Mały komponent do wyświetlania pojedynczej oceny
const RatingItem = ({ rating }) => (
    <li className="list-group-item">
        <div>
            {[...Array(5)].map((_, i) => (
                <i key={i} className={`bi ${i < rating.value ? 'bi-star-fill text-warning' : 'bi-star text-muted'}`}></i>
            ))}
            <span className="ms-2 fw-bold">{rating.value}/5</span>
        </div>
        {rating.comment && <p className="mt-1 mb-0 fst-italic">"{rating.comment}"</p>}
        <small className="text-muted">Oceniono przez: {rating.raterName || `User ${rating.raterId}`} - <FormattedDate date={rating.createdDate} /></small>
    </li>
);


// Główny komponent TransactionDetails
const TransactionDetails = ({ transactionId }) => {
    const [transaction, setTransaction] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const currentUserId = getCurrentUserId(); // Pobierz ID zalogowanego użytkownika

    // Pobieranie danych transakcji
    const fetchTransactionData = useCallback(async () => {
        if (!transactionId || transactionId <= 0) {
            setError('Błąd: Nieprawidłowe ID transakcji.');
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        console.log(`TransactionDetails: Fetching data for transaction ID: ${transactionId}`);

        try {
            // Użyj ApiService do pobrania TransactionWithDetailsDto
            const data = await ApiService.get(`/api/transactions/${transactionId}`);
            if (!data) throw new Error("Nie znaleziono transakcji lub otrzymano pustą odpowiedź.");

            console.log("TransactionDetails: Received transaction data:", data);
            setTransaction(data);

        } catch (err) {
            console.error("TransactionDetails: Error fetching transaction data:", err);
            // Jeśli błąd to 403 Forbidden, pokaż specjalny komunikat
            if (err.message && err.message.toLowerCase().includes('forbidden')) {
                setError("Nie masz uprawnień do wyświetlenia tej transakcji.");
            } else if (err.message && err.message.toLowerCase().includes('not found')) {
                setError("Nie znaleziono transakcji o podanym ID.");
            } else {
                setError(err.message || "Nie udało się załadować szczegółów transakcji.");
            }
            setTransaction(null);
        } finally {
            setLoading(false);
        }
    }, [transactionId]);

    useEffect(() => {
        fetchTransactionData();
    }, [fetchTransactionData]);

    // TODO: Dodać logikę wysyłania nowej wiadomości
    const handleSendMessage = async (/* messageContent */) => {
        alert('TODO: Implement send message functionality');
        // const messageData = { content: messageContent };
        // await ApiService.post(`/api/transactions/${transactionId}/messages`, messageData);
        // fetchTransactionData(); // Odśwież dane
    };

    // TODO: Dodać logikę dodawania oceny
    const handleAddRating = async (/* ratingValue, ratingComment */) => {
        alert('TODO: Implement add rating functionality');
        // const ratingData = { value: ratingValue, comment: ratingComment };
        // await ApiService.post(`/api/transactions/${transactionId}/ratings`, ratingData);
        // fetchTransactionData(); // Odśwież dane
    };

    // TODO: Dodać logikę zmiany statusu (jeśli potrzebna w tym widoku)
    const handleChangeStatus = async (/* newStatus */) => {
        alert('TODO: Implement change status functionality');
        // const statusData = { status: newStatus };
        // await ApiService.put(`/api/transactions/${transactionId}/status`, statusData);
        // fetchTransactionData(); // Odśwież dane
    };

    // --- Renderowanie ---

    if (loading) {
        return (
            <div className="text-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return <div className="alert alert-danger">{error}</div>;
    }

    if (!transaction) {
        return <div className="alert alert-warning">Nie znaleziono danych dla tej transakcji.</div>;
    }

    // Destrukturyzacja danych transakcji
    const { id, startDate, endDate, status, type, seller, buyer, item, messages = [], ratings = [] } = transaction;
    const isUserSeller = seller?.id === currentUserId;
    const isUserBuyer = buyer?.id === currentUserId;
    const otherParty = isUserSeller ? buyer : seller;

    // Helper do wyświetlania statusu
    const getStatusBadge = (status) => {
        switch (status) {
            case 'Pending': return <span className="badge bg-warning text-dark">Oczekująca</span>;
            case 'InProgress': return <span className="badge bg-info text-dark">W toku</span>;
            case 'Completed': return <span className="badge bg-success">Zakończona</span>;
            case 'Cancelled': return <span className="badge bg-danger">Anulowana</span>;
            default: return <span className="badge bg-secondary">{status}</span>;
        }
    };
    // Helper do wyświetlania typu
    const getTypeDisplay = (type) => {
        switch (type) {
            case 'Exchange': return <span className="badge bg-primary">Wymiana</span>;
            case 'Donation': return <span className="badge bg-secondary">Darowizna</span>;
            case 'Sale': return <span className="badge bg-success">Sprzedaż</span>; // Zakładając dodanie Sale
            default: return <span className="badge bg-light text-dark">{type}</span>;
        }
    };


    return (
        <div className="transaction-details">
            <div className="row g-4">
                {/* Kolumna lewa: Przedmiot i Uczestnicy */}
                <div className="col-md-4">
                    {/* Karta Przedmiotu */}
                    <div className="card mb-4 shadow-sm">
                        <img src={ApiService.getImageUrl(item?.itemPhotoPath || item?.mainPhotoPath)} // Użyj photo path z DTO transakcji lub itemu
                             alt={item?.name || 'Przedmiot'}
                             className="card-img-top"
                             style={{ height: '200px', objectFit: 'cover' }}
                             onError={(e) => { e.target.src = ApiService.getImageUrl(null); }}/>
                        <div className="card-body">
                            <h5 className="card-title">{item?.name || 'Brak nazwy'}</h5>
                            <p className="card-text small text-muted">ID Przedmiotu: {item?.id || 'N/A'}</p>
                            <a href={`/Items/Details/${item?.id}`} className="btn btn-sm btn-outline-secondary">
                                <i className="bi bi-box-arrow-up-right me-1"></i> Zobacz przedmiot
                            </a>
                        </div>
                    </div>

                    {/* Karta Sprzedającego */}
                    {seller && (
                        <div className="card mb-3 shadow-sm">
                            <div className="card-header">Sprzedający {isUserSeller && "(Ty)"}</div>
                            <div className="card-body d-flex align-items-center">
                                <img src={ApiService.getImageUrl(seller.avatarPath)} className="rounded-circle me-3" alt={seller.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} />
                                <div>
                                    <a href={`/Profile/Index/${seller.id}`} className="fw-bold text-decoration-none">{seller.firstName} {seller.lastName}</a>
                                    <div className="small text-muted">EcoScore: {seller.ecoScore ?? 0}</div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Karta Kupującego */}
                    {buyer && (
                        <div className="card mb-3 shadow-sm">
                            <div className="card-header">Kupujący {isUserBuyer && "(Ty)"}</div>
                            <div className="card-body d-flex align-items-center">
                                <img src={ApiService.getImageUrl(buyer.avatarPath)} className="rounded-circle me-3" alt={buyer.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }}/>
                                <div>
                                    <a href={`/Profile/Index/${buyer.id}`} className="fw-bold text-decoration-none">{buyer.firstName} {buyer.lastName}</a>
                                    <div className="small text-muted">EcoScore: {buyer.ecoScore ?? 0}</div>
                                </div>
                            </div>
                        </div>
                    )}
                </div>

                {/* Kolumna prawa: Status, Akcje, Wiadomości, Oceny */}
                <div className="col-md-8">
                    {/* Status i Daty */}
                    <div className="card mb-4 shadow-sm">
                        <div className="card-header d-flex justify-content-between align-items-center">
                            <span>Status Transakcji</span>
                            {getStatusBadge(status)}
                        </div>
                        <div className="card-body">
                            <p><strong>Typ:</strong> {getTypeDisplay(type)}</p>
                            <p><strong>Rozpoczęto:</strong> <FormattedDate date={startDate} /></p>
                            {endDate && <p><strong>Zakończono/Anulowano:</strong> <FormattedDate date={endDate} /></p>}
                        </div>
                    </div>

                    {/* TODO: Akcje (Przyciski np. Anuluj, Akceptuj, Wyślij, Odbierz, Oceń) */}
                    {/* <div className="card mb-4 shadow-sm">
                        <div className="card-header">Akcje</div>
                        <div className="card-body d-flex gap-2">
                            {status === 'Pending' && isUserSeller && <button className="btn btn-sm btn-success" onClick={() => handleChangeStatus('InProgress')}>Akceptuj</button>}
                            {status === 'Pending' && <button className="btn btn-sm btn-danger" onClick={() => handleChangeStatus('Cancelled')}>Anuluj</button>}
                            {status === 'InProgress' && <button className="btn btn-sm btn-danger" onClick={() => handleChangeStatus('Cancelled')}>Anuluj</button>}
                            {status === 'InProgress' && <button className="btn btn-sm btn-primary" onClick={() => handleChangeStatus('Completed')}>Zakończ</button>}
                            {status === 'Completed' && <button className="btn btn-sm btn-warning" onClick={handleAddRating}>Oceń</button>}
                        </div>
                    </div> */}

                    {/* Sekcja Wiadomości */}
                    <div className="card mb-4 shadow-sm">
                        <div className="card-header">Wiadomości</div>
                        <div className="card-body" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                            {messages.length === 0 ? (
                                <p className="text-muted text-center">Brak wiadomości w tej transakcji.</p>
                            ) : (
                                messages.sort((a, b) => new Date(a.sentDate) - new Date(b.sentDate)) // Sortuj wg daty
                                    .map(msg => <MessageItem key={msg.id} message={msg} currentUserId={currentUserId} />)
                            )}
                        </div>
                        {/* TODO: Formularz wysyłania nowej wiadomości (jeśli status pozwala) */}
                        {/* {status === 'Pending' || status === 'InProgress' ? ( ... formularz ... ) : null} */}
                    </div>


                    {/* Sekcja Ocen */}
                    <div className="card shadow-sm">
                        <div className="card-header">Oceny</div>
                        <div className="card-body">
                            {ratings.length === 0 ? (
                                <p className="text-muted text-center">Brak ocen dla tej transakcji.</p>
                            ) : (
                                <ul className="list-group list-group-flush">
                                    {ratings.map(r => <RatingItem key={r.id} rating={r} />)}
                                </ul>
                            )}
                            {/* TODO: Formularz dodawania nowej oceny (jeśli status pozwala i user jeszcze nie ocenił) */}
                            {/* {status === 'Completed' && !didUserRate ? ( ... formularz ... ) : null} */}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
TransactionDetails.displayName = 'TransactionDetails';

// --- Inicjalizacja Komponentu ---
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-transaction-details-container');
    if (container) {
        const transactionIdString = container.getAttribute('data-transaction-id');
        const transactionId = parseInt(transactionIdString, 10);

        if (!isNaN(transactionId) && transactionId > 0) {
            const root = ReactDOM.createRoot(container);
            root.render(
                <StrictMode>
                    <TransactionDetails transactionId={transactionId} />
                </StrictMode>
            );
            console.log(`TransactionDetails component initialized for TransactionID: ${transactionId}`);
        } else {
            console.error(`TransactionDetails: Invalid or missing transaction ID in data-transaction-id attribute. Value found: "${transactionIdString}". Parsed as: ${transactionId}.`);
            container.innerHTML = '<div class="alert alert-danger">Błąd krytyczny: Nie można załadować szczegółów transakcji. Brak poprawnego ID.</div>';
        }
    } else {
        console.warn("Container element '#react-transaction-details-container' not found on this page.");
    }
});