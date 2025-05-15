// Ścieżka: wwwroot/js/components/TransactionDetails.js

import React, { useState, useEffect, useCallback, StrictMode, useRef } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';
import { getCurrentUserId } from '../utils/auth.js';

// Komponent do formatowania daty
const FormattedDate = ({ date }) => {
    if (!date) return <span className="text-muted">--</span>;
    try {
        return <>{new Date(date).toLocaleString('pl-PL', { dateStyle: 'short', timeStyle: 'short' })}</>;
    } catch (e) {
        console.error("Error formatting date:", date, e);
        return <span className="text-danger">Błąd daty</span>;
    }
};
FormattedDate.displayName = 'FormattedDate';

// Komponent do wyświetlania wiadomości
const MessageItem = React.memo(({ message, currentUserId }) => {
    const isCurrentUserSender = message.senderId === currentUserId;
    return (
        <div className={`d-flex mb-2 ${isCurrentUserSender ? 'justify-content-end ps-5' : 'justify-content-start pe-5'}`}>
            <div className={`card shadow-sm ${isCurrentUserSender ? 'bg-success bg-opacity-10 text-dark' : 'bg-light'}`} style={{ maxWidth: '80%', width: 'fit-content' }}>
                <div className="card-body p-2">
                    <p className="card-text mb-1" style={{ whiteSpace: 'pre-wrap' }}>{message.content}</p>
                    <small className="text-muted d-block text-end" style={{ fontSize: '0.75rem' }}>
                        {message.senderName || `Użytkownik ${message.senderId}`} - <FormattedDate date={message.sentDate} />
                    </small>
                </div>
            </div>
        </div>
    );
});
MessageItem.displayName = 'MessageItem';

// Komponent do wyświetlania oceny
const RatingItem = React.memo(({ rating }) => (
    <li className="list-group-item">
        <div>
            {[...Array(5)].map((_, i) => (
                <i key={i} className={`bi ${i < rating.value ? 'bi-star-fill text-warning' : 'bi-star text-muted'}`}></i>
            ))}
            <span className="ms-2 fw-bold">{rating.value}/5</span>
            <span className="ms-2 small text-muted">(ocena od: {rating.raterName || `Użytkownik ${rating.raterId}`})</span>
        </div>
        {rating.comment && <p className="mt-1 mb-0 fst-italic">"{rating.comment}"</p>}
        <small className="text-muted">Oceniono: <FormattedDate date={rating.ratingDate || rating.createdDate} /></small> {/* Użyj ratingDate jeśli dostępne */}
    </li>
));
RatingItem.displayName = 'RatingItem';

// Główny komponent TransactionDetails
const TransactionDetails = ({ transactionId }) => {
    const [transaction, setTransaction] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null); // Główny błąd ładowania
    const [actionError, setActionError] = useState(null); // Błąd dla akcji (potwierdzenie, zmiana statusu)
    const [actionSuccess, setActionSuccess] = useState(null); // Sukces dla akcji
    const currentUserId = getCurrentUserId();

    const [newMessageContent, setNewMessageContent] = useState('');
    const [isSendingMessage, setIsSendingMessage] = useState(false);
    const [sendMessageError, setSendMessageError] = useState(null);

    const [isConfirming, setIsConfirming] = useState(false); // Stan dla przycisku potwierdzenia

    const messagesEndRef = useRef(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    const fetchTransactionData = useCallback(async (scroll = false, keepActionMessages = false) => {
        if (!transactionId || transactionId <= 0) {
            setError('Błąd: Nieprawidłowe ID transakcji.'); setLoading(false); return;
        }
        if (!scroll) setLoading(true); // Nie pokazuj głównego loadera przy odświeżaniu po akcji

        // Resetuj błędy/sukcesy tylko jeśli nie zażądano ich zachowania
        if (!keepActionMessages) {
            setError(null);
            setActionError(null);
            setActionSuccess(null);
        }
        setSendMessageError(null);
        console.log(`TransactionDetails: Fetching data for transaction ID: ${transactionId}`);
        try {
            const data = await ApiService.get(`/api/transactions/${transactionId}`);
            if (!data) throw new Error("Nie znaleziono transakcji lub otrzymano pustą odpowiedź.");
            console.log("TransactionDetails: Received transaction data:", data);
            setTransaction(data);
            if (scroll) {
                setTimeout(scrollToBottom, 100);
            }
        } catch (err) {
            console.error("TransactionDetails: Error fetching transaction data:", err);
            if (err.message?.toLowerCase().includes('forbidden')) { setError("Nie masz uprawnień do wyświetlenia tej transakcji."); }
            else if (err.message?.toLowerCase().includes('not found')) { setError("Nie znaleziono transakcji o podanym ID."); }
            else { setError(err.message || "Nie udało się załadować szczegółów transakcji."); }
            setTransaction(null);
        } finally {
            if (!scroll) setLoading(false);
        }
    }, [transactionId]);

    useEffect(() => {
        fetchTransactionData();
    }, [fetchTransactionData]);

    const handleSendMessage = useCallback(async (e) => {
        if (e) e.preventDefault();
        const content = newMessageContent.trim();
        if (!content) { setSendMessageError("Wiadomość nie może być pusta."); return; }
        if (!transactionId) { setSendMessageError("Brak ID transakcji."); return; }

        setIsSendingMessage(true); setSendMessageError(null); setActionError(null); setActionSuccess(null);
        try {
            const messageData = { content: content };
            await ApiService.post(`/api/transactions/${transactionId}/messages`, messageData);
            setNewMessageContent('');
            fetchTransactionData(true, true); // Odśwież dane, zażądaj przewinięcia, zachowaj komunikaty akcji
        } catch (err) {
            setSendMessageError(err.message || "Nie udało się wysłać wiadomości.");
        } finally {
            setIsSendingMessage(false);
        }
    }, [transactionId, newMessageContent, fetchTransactionData]);

    // NOWA FUNKCJA: Obsługa potwierdzenia transakcji
    const handleConfirmTransaction = useCallback(async () => {
        if (!transactionId) {
            setActionError("Brak ID transakcji do potwierdzenia.");
            return;
        }
        setIsConfirming(true); setActionError(null); setActionSuccess(null);
        try {
            const response = await ApiService.post(`/api/transactions/${transactionId}/confirm`, {});
            // Zakładamy, że API zwraca komunikat sukcesu w polu 'message' jeśli odpowiedź jest typu ApiResponse
            // lub response będzie bezpośrednio danymi jeśli nie jest to standardowy ApiResponse
            setActionSuccess(response?.message || response || "Potwierdzenie zostało zapisane.");
            fetchTransactionData(false, true); // Odśwież dane transakcji, zachowaj komunikat sukcesu
        } catch (err) {
            console.error("TransactionDetails: Error confirming transaction:", err);
            setActionError(err.message || "Wystąpił błąd podczas potwierdzania transakcji.");
        } finally {
            setIsConfirming(false);
        }
    }, [transactionId, fetchTransactionData]);


    // --- Renderowanie ---
    if (loading && !transaction) {
        return ( <div className="text-center py-5"> <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}><span className="visually-hidden">Ładowanie...</span></div> </div> );
    }
    if (error && !transaction) {
        return <div className="alert alert-danger">{error}</div>;
    }
    if (!transaction) {
        return <div className="alert alert-warning">Nie znaleziono danych dla tej transakcji.</div>;
    }

    const { id, startDate, endDate, status, type, seller, buyer, item, messages = [], ratings = [], buyerConfirmed, sellerConfirmed } = transaction;
    const isUserSeller = seller?.id === currentUserId;
    const isUserBuyer = buyer?.id === currentUserId;
    const canUserConfirm = (isUserBuyer && !buyerConfirmed) || (isUserSeller && !sellerConfirmed);
    const canInteractWithMessages = status === 'Pending' || status === 'InProgress';
    // Transakcję można potwierdzić tylko gdy jest 'InProgress'
    const canConfirmAction = status === 'InProgress' && (isUserBuyer || isUserSeller);


    const getStatusBadge = (s) => {
        switch (s) { case 'Pending': return <span className="badge bg-warning text-dark">Oczekująca</span>; case 'InProgress': return <span className="badge bg-info text-dark">W toku</span>; case 'Completed': return <span className="badge bg-success">Zakończona</span>; case 'Cancelled': return <span className="badge bg-danger">Anulowana</span>; default: return <span className="badge bg-secondary">{s}</span>; }
    };
    const getTypeDisplay = (t) => {
        switch (t) { case 'Exchange': return <span className="badge bg-primary">Wymiana</span>; case 'Donation': return <span className="badge bg-secondary">Darowizna</span>; case 'Sale': return <span className="badge bg-success">Sprzedaż</span>; default: return <span className="badge bg-light text-dark">{t}</span>; }
    };

    return (
        <div className="transaction-details">
            {/* Komunikaty o błędach/sukcesach akcji */}
            {actionError && <div className="alert alert-danger alert-dismissible fade show" role="alert">
                {actionError}
                <button type="button" className="btn-close" onClick={() => setActionError(null)} aria-label="Close"></button>
            </div>}
            {actionSuccess && <div className="alert alert-success alert-dismissible fade show" role="alert">
                {actionSuccess}
                <button type="button" className="btn-close" onClick={() => setActionSuccess(null)} aria-label="Close"></button>
            </div>}


            <div className="row g-4">
                {/* Kolumna Lewa (Przedmiot, Uczestnicy) */}
                <div className="col-md-4">
                    <div className="card mb-4 shadow-sm">
                        <img src={ApiService.getImageUrl(item?.itemPhotoPath || item?.mainPhotoPath)} alt={item?.name || 'Przedmiot'} className="card-img-top" style={{ height: '200px', objectFit: 'cover' }} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }}/>
                        <div className="card-body"> <h5 className="card-title">{item?.name || 'Brak nazwy'}</h5> <p className="card-text small text-muted">ID Przedmiotu: {item?.id || 'N/A'}</p> <a href={`/Items/Details/${item?.id}`} className="btn btn-sm btn-outline-secondary"> <i className="bi bi-box-arrow-up-right me-1"></i> Zobacz przedmiot </a> </div>
                    </div>
                    {seller && <div className="card mb-3 shadow-sm"> <div className="card-header d-flex justify-content-between align-items-center">Sprzedający {isUserSeller && "(Ty)"} {sellerConfirmed && status === 'InProgress' && <span className="badge bg-light text-success border border-success"><i className="bi bi-check-circle-fill me-1"></i>Potwierdzono</span>}</div> <div className="card-body d-flex align-items-center"> <img src={ApiService.getImageUrl(seller.avatarPath)} className="rounded-circle me-3" alt={seller.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} /> <div> <a href={`/Profile/Index/${seller.id}`} className="fw-bold text-decoration-none">{seller.firstName} {seller.lastName}</a> <div className="small text-muted">EcoScore: {seller.ecoScore ?? 0}</div> </div> </div> </div>}
                    {buyer && <div className="card mb-3 shadow-sm"> <div className="card-header d-flex justify-content-between align-items-center">Kupujący {isUserBuyer && "(Ty)"} {buyerConfirmed && status === 'InProgress' && <span className="badge bg-light text-success border border-success"><i className="bi bi-check-circle-fill me-1"></i>Potwierdzono</span>}</div> <div className="card-body d-flex align-items-center"> <img src={ApiService.getImageUrl(buyer.avatarPath)} className="rounded-circle me-3" alt={buyer.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} /> <div> <a href={`/Profile/Index/${buyer.id}`} className="fw-bold text-decoration-none">{buyer.firstName} {buyer.lastName}</a> <div className="small text-muted">EcoScore: {buyer.ecoScore ?? 0}</div> </div> </div> </div>}
                </div>

                {/* Kolumna Prawa */}
                <div className="col-md-8">
                    <div className="card mb-4 shadow-sm">
                        <div className="card-header d-flex justify-content-between align-items-center">
                            <span>Status Transakcji</span> {getStatusBadge(status)}
                        </div>
                        <div className="card-body">
                            <p><strong>Typ:</strong> {getTypeDisplay(type)}</p>
                            <p><strong>Rozpoczęto:</strong> <FormattedDate date={startDate} /></p>
                            {endDate && <p><strong>Zakończono/Anulowano:</strong> <FormattedDate date={endDate} /></p>}

                            {/* Informacja o potwierdzeniach - jeśli transakcja w toku */}
                            {status === 'InProgress' && (
                                <div className="mt-3 pt-3 border-top">
                                    <p className="mb-1"><strong>Status potwierdzeń:</strong></p>
                                    <ul className="list-unstyled mb-0 small">
                                        <li>
                                            Sprzedający ({seller?.firstName || 'N/A'}): {sellerConfirmed ?
                                            <span className="text-success fw-bold"><i className="bi bi-check-circle-fill"></i> Potwierdzono</span> :
                                            <span className="text-warning"><i className="bi bi-hourglass-split"></i> Oczekuje</span>}
                                        </li>
                                        <li>
                                            Kupujący ({buyer?.firstName || 'N/A'}): {buyerConfirmed ?
                                            <span className="text-success fw-bold"><i className="bi bi-check-circle-fill"></i> Potwierdzono</span> :
                                            <span className="text-warning"><i className="bi bi-hourglass-split"></i> Oczekuje</span>}
                                        </li>
                                    </ul>
                                    {(!buyerConfirmed || !sellerConfirmed) &&
                                        <p className="mt-2 mb-0 fst-italic small text-muted">
                                            Transakcja zostanie automatycznie zakończona, gdy obie strony potwierdzą, lub po 14 dniach od jednostronnego potwierdzenia.
                                        </p>
                                    }
                                </div>
                            )}
                        </div>
                    </div>

                    {/* Akcje dla transakcji */}
                    {canConfirmAction && (
                        <div className="card mb-4 shadow-sm">
                            <div className="card-header">Akcje</div>
                            <div className="card-body text-center">
                                {canUserConfirm ? (
                                    <button
                                        className="btn btn-success"
                                        onClick={handleConfirmTransaction}
                                        disabled={isConfirming}
                                    >
                                        {isConfirming ? (
                                            <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Przetwarzanie...</>
                                        ) : (
                                            <><i className="bi bi-check2-circle me-2"></i>Potwierdź odbiór / Zakończenie z Twojej strony</>
                                        )}
                                    </button>
                                ) : (
                                    <p className="text-muted">
                                        <i className="bi bi-check-circle me-1 text-success"></i> Już potwierdziłeś tę transakcję. Oczekiwanie na drugą stronę.
                                    </p>
                                )}
                                <p className="mt-2 mb-0 small text-muted">
                                    Potwierdzenie oznacza, że {isUserBuyer ? "otrzymałeś przedmiot i wszystko jest w porządku" : "przekazałeś przedmiot i transakcja przebiegła pomyślnie z Twojej strony"}.
                                </p>
                            </div>
                        </div>
                    )}


                    {/* Wiadomości */}
                    <div className="card mb-4 shadow-sm">
                        <div className="card-header">Wiadomości</div>
                        <div className="card-body messages-container" style={{ height: '400px', overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
                            <div style={{ flexGrow: 1 }}></div>
                            {messages.length === 0 ? ( <p className="text-muted text-center mb-0">Brak wiadomości.</p> )
                                : ( [...messages].sort((a, b) => new Date(a.sentDate) - new Date(b.sentDate))
                                    .map(msg => <MessageItem key={msg.id} message={msg} currentUserId={currentUserId} />) )}
                            <div ref={messagesEndRef} />
                        </div>
                        {canInteractWithMessages ? (
                            <div className="card-footer">
                                {sendMessageError && <div className="alert alert-danger small p-2 mb-2">{sendMessageError}</div>}
                                <form onSubmit={handleSendMessage}>
                                    <div className="input-group">
                                        <textarea className="form-control" rows="2" placeholder="Napisz wiadomość..." value={newMessageContent} onChange={(e) => setNewMessageContent(e.target.value)} disabled={isSendingMessage} required />
                                        <button className="btn btn-success" type="submit" disabled={isSendingMessage || !newMessageContent.trim()}>
                                            {isSendingMessage ? (<span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>) : (<i className="bi bi-send-fill"></i>)}
                                        </button>
                                    </div>
                                </form>
                            </div>
                        ) : ( <div className="card-footer text-muted small"> Wysyłanie wiadomości jest niedostępne (Status: {status}). </div> )}
                    </div>

                    {/* Oceny */}
                    <div className="card shadow-sm">
                        <div className="card-header">Oceny</div>
                        <div className="card-body">
                            {ratings.length === 0 ? ( <p className="text-muted text-center">Brak ocen.</p> )
                                : ( <ul className="list-group list-group-flush">{ratings.map(r => <RatingItem key={r.id} rating={r} />)}</ul> )}
                            {/* TODO: Formularz dodawania oceny, jeśli transakcja zakończona i użytkownik nie ocenił */}
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
            root.render( <StrictMode> <TransactionDetails transactionId={transactionId} /> </StrictMode> );
            console.log(`TransactionDetails component initialized for TransactionID: ${transactionId}`);
        } else {
            console.error(`TransactionDetails: Invalid or missing transaction ID... Value: "${transactionIdString}". Parsed: ${transactionId}.`);
            container.innerHTML = '<div class="alert alert-danger">Błąd: Nie można załadować szczegółów transakcji. Brak ID.</div>';
        }
    } else {
        // console.warn("TransactionDetails: Container '#react-transaction-details-container' not found.");
    }
});
