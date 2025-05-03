// Ścieżka: wwwroot/js/components/TransactionDetails.js

import React, { useState, useEffect, useCallback, StrictMode, useRef } from 'react'; // Dodano useRef
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';
import { getCurrentUserId } from '../utils/auth.js';

// Komponent do formatowania daty
const FormattedDate = ({ date }) => {
    if (!date) return <span className="text-muted">--</span>;
    try {
        // Dodano opcje regionalne dla pewności
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
        <div className={`d-flex mb-2 ${isCurrentUserSender ? 'justify-content-end ps-5' : 'justify-content-start pe-5'}`}> {/* Dodano paddingi dla lepszego wyglądu */}
            <div className={`card shadow-sm ${isCurrentUserSender ? 'bg-success bg-opacity-10 text-dark' : 'bg-light'}`} style={{ maxWidth: '80%', width: 'fit-content' }}> {/* Dopasuj szerokość */}
                <div className="card-body p-2">
                    {/* Zezwól na łamanie linii z \n */}
                    <p className="card-text mb-1" style={{ whiteSpace: 'pre-wrap' }}>{message.content}</p>
                    <small className="text-muted d-block text-end" style={{ fontSize: '0.75rem' }}>
                        {message.senderName || `Użytkownik ${message.senderId}`} - <FormattedDate date={message.sentDate} />
                    </small>
                </div>
            </div>
        </div>
    );
});
MessageItem.displayName = 'MessageItem'; // Nazwa dla DevTools


// Komponent do wyświetlania oceny
const RatingItem = React.memo(({ rating }) => (
    <li className="list-group-item">
        <div>
            {[...Array(5)].map((_, i) => (
                <i key={i} className={`bi ${i < rating.value ? 'bi-star-fill text-warning' : 'bi-star text-muted'}`}></i>
            ))}
            <span className="ms-2 fw-bold">{rating.value}/5</span>
            {/* Dodano informację kto ocenił */}
            <span className="ms-2 small text-muted">(ocena od: {rating.raterName || `Użytkownik ${rating.raterId}`})</span>
        </div>
        {rating.comment && <p className="mt-1 mb-0 fst-italic">"{rating.comment}"</p>}
        <small className="text-muted">Oceniono: <FormattedDate date={rating.createdDate} /></small>
    </li>
));
RatingItem.displayName = 'RatingItem'; // Nazwa dla DevTools

// Główny komponent TransactionDetails
const TransactionDetails = ({ transactionId }) => {
    const [transaction, setTransaction] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const currentUserId = getCurrentUserId();

    // Stany dla wysyłania wiadomości
    const [newMessageContent, setNewMessageContent] = useState('');
    const [isSendingMessage, setIsSendingMessage] = useState(false);
    const [sendMessageError, setSendMessageError] = useState(null);

    // Ref dla kontenera wiadomości (do przewijania)
    const messagesEndRef = useRef(null);

    // Funkcja do przewijania na dół listy wiadomości
    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    // Funkcja do pobierania danych transakcji
    const fetchTransactionData = useCallback(async (scroll = false) => { // Dodano argument scroll
        if (!transactionId || transactionId <= 0) {
            setError('Błąd: Nieprawidłowe ID transakcji.'); setLoading(false); return;
        }
        // Nie ustawiaj setLoading(true) jeśli tylko odświeżamy wiadomości po wysłaniu
        if (!scroll) setLoading(true);
        setError(null); // Resetuj główny błąd
        setSendMessageError(null); // Resetuj błąd wysyłania wiadomości
        console.log(`TransactionDetails: Fetching data for transaction ID: ${transactionId}`);
        try {
            const data = await ApiService.get(`/api/transactions/${transactionId}`);
            if (!data) throw new Error("Nie znaleziono transakcji lub otrzymano pustą odpowiedź.");
            console.log("TransactionDetails: Received transaction data:", data);
            setTransaction(data);
            // Jeśli zażądano przewinięcia (po wysłaniu wiadomości), zrób to po ustawieniu stanu
            if (scroll) {
                // Użyj setTimeout, aby dać czas na renderowanie DOM
                setTimeout(scrollToBottom, 100);
            }
        } catch (err) { /* ... obsługa błędów fetch ... */
            console.error("TransactionDetails: Error fetching transaction data:", err);
            if (err.message?.toLowerCase().includes('forbidden')) { setError("Nie masz uprawnień do wyświetlenia tej transakcji."); }
            else if (err.message?.toLowerCase().includes('not found')) { setError("Nie znaleziono transakcji o podanym ID."); }
            else { setError(err.message || "Nie udało się załadować szczegółów transakcji."); }
            setTransaction(null);
        } finally { setLoading(false); }
    }, [transactionId]);

    useEffect(() => {
        fetchTransactionData();
    }, [fetchTransactionData]); // Uruchom przy pierwszym renderowaniu

    // Funkcja do wysyłania wiadomości
    const handleSendMessage = useCallback(async (e) => {
        if (e) e.preventDefault();
        const content = newMessageContent.trim();
        if (!content) { setSendMessageError("Wiadomość nie może być pusta."); return; }
        if (!transactionId) { setSendMessageError("Brak ID transakcji."); return; }

        setIsSendingMessage(true); setSendMessageError(null);
        try {
            const messageData = { content: content };
            await ApiService.post(`/api/transactions/${transactionId}/messages`, messageData);
            console.log("TransactionDetails: Message sent successfully.");
            setNewMessageContent(''); // Wyczyść input
            fetchTransactionData(true); // Odśwież dane i zażądaj przewinięcia
        } catch (err) {
            console.error("TransactionDetails: Error sending message:", err);
            setSendMessageError(err.message || "Nie udało się wysłać wiadomości.");
        } finally { setIsSendingMessage(false); }
    }, [transactionId, newMessageContent, fetchTransactionData]);

    // --- Miejsce na inne handlery (handleAddRating, handleChangeStatus) ---


    // --- Renderowanie ---
    if (loading && !transaction) { // Pokaż spinner tylko przy pierwszym ładowaniu
        return ( <div className="text-center py-5"> <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}><span className="visually-hidden">Ładowanie...</span></div> </div> );
    }
    if (error && !transaction) { // Pokaż błąd tylko jeśli nie udało się załadować transakcji w ogóle
        return <div className="alert alert-danger">{error}</div>;
    }
    if (!transaction) { // Jeśli po załadowaniu nadal brak transakcji
        return <div className="alert alert-warning">Nie znaleziono danych dla tej transakcji.</div>;
    }

    // Destrukturyzacja i helpery (bez zmian)
    const { id, startDate, endDate, status, type, seller, buyer, item, messages = [], ratings = [] } = transaction;
    const isUserSeller = seller?.id === currentUserId;
    const isUserBuyer = buyer?.id === currentUserId;
    const canInteract = status === 'Pending' || status === 'InProgress';
    const getStatusBadge = (s) => { /* ... jak poprzednio ... */
        switch (s) { case 'Pending': return <span className="badge bg-warning text-dark">Oczekująca</span>; case 'InProgress': return <span className="badge bg-info text-dark">W toku</span>; case 'Completed': return <span className="badge bg-success">Zakończona</span>; case 'Cancelled': return <span className="badge bg-danger">Anulowana</span>; default: return <span className="badge bg-secondary">{s}</span>; }
    };
    const getTypeDisplay = (t) => { /* ... jak poprzednio ... */
        switch (t) { case 'Exchange': return <span className="badge bg-primary">Wymiana</span>; case 'Donation': return <span className="badge bg-secondary">Darowizna</span>; case 'Sale': return <span className="badge bg-success">Sprzedaż</span>; default: return <span className="badge bg-light text-dark">{t}</span>; }
    };

    return (
        <div className="transaction-details">
            {/* Wyświetlaj błędy operacji (np. wysyłania wiadomości) jeśli wystąpiły */}
            {error && <div className="alert alert-danger">{error}</div>}

            <div className="row g-4">
                {/* Kolumna Lewa (Przedmiot, Uczestnicy) - bez zmian */}
                <div className="col-md-4">
                    <div className="card mb-4 shadow-sm">
                        <img src={ApiService.getImageUrl(item?.itemPhotoPath || item?.mainPhotoPath)} alt={item?.name || 'Przedmiot'} className="card-img-top" style={{ height: '200px', objectFit: 'cover' }} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }}/>
                        <div className="card-body"> <h5 className="card-title">{item?.name || 'Brak nazwy'}</h5> <p className="card-text small text-muted">ID Przedmiotu: {item?.id || 'N/A'}</p> <a href={`/Items/Details/${item?.id}`} className="btn btn-sm btn-outline-secondary"> <i className="bi bi-box-arrow-up-right me-1"></i> Zobacz przedmiot </a> </div>
                    </div>
                    {seller && <div className="card mb-3 shadow-sm"> <div className="card-header">Sprzedający {isUserSeller && "(Ty)"}</div> <div className="card-body d-flex align-items-center"> <img src={ApiService.getImageUrl(seller.avatarPath)} className="rounded-circle me-3" alt={seller.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} /> <div> <a href={`/Profile/Index/${seller.id}`} className="fw-bold text-decoration-none">{seller.firstName} {seller.lastName}</a> <div className="small text-muted">EcoScore: {seller.ecoScore ?? 0}</div> </div> </div> </div>}
                    {buyer && <div className="card mb-3 shadow-sm"> <div className="card-header">Kupujący {isUserBuyer && "(Ty)"}</div> <div className="card-body d-flex align-items-center"> <img src={ApiService.getImageUrl(buyer.avatarPath)} className="rounded-circle me-3" alt={buyer.firstName} style={{width: '50px', height: '50px', objectFit: 'cover'}} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }}/> <div> <a href={`/Profile/Index/${buyer.id}`} className="fw-bold text-decoration-none">{buyer.firstName} {buyer.lastName}</a> <div className="small text-muted">EcoScore: {buyer.ecoScore ?? 0}</div> </div> </div> </div>}
                </div>

                {/* Kolumna Prawa */}
                <div className="col-md-8">
                    {/* Status i Daty (bez zmian) */}
                    <div className="card mb-4 shadow-sm"> <div className="card-header d-flex justify-content-between align-items-center"> <span>Status Transakcji</span> {getStatusBadge(status)} </div> <div className="card-body"> <p><strong>Typ:</strong> {getTypeDisplay(type)}</p> <p><strong>Rozpoczęto:</strong> <FormattedDate date={startDate} /></p> {endDate && <p><strong>Zakończono/Anulowano:</strong> <FormattedDate date={endDate} /></p>} </div> </div>

                    {/* TODO: Akcje */}
                    {/* <div className="card mb-4 shadow-sm"> ... </div> */}

                    {/* Wiadomości */}
                    <div className="card mb-4 shadow-sm">
                        <div className="card-header">Wiadomości</div>
                        {/* Kontener wiadomości z przewijaniem */}
                        <div className="card-body messages-container" style={{ height: '400px', overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
                            {/* Użyj flex-grow-1 aby wypełnić przestrzeń i pushować formularz w dół */}
                            <div style={{ flexGrow: 1 }}></div>
                            {/* Wyświetlanie wiadomości */}
                            {messages.length === 0 ? ( <p className="text-muted text-center mb-0">Brak wiadomości.</p> )
                                : ( [...messages].sort((a, b) => new Date(a.sentDate) - new Date(b.sentDate)) // Sortuj od najstarszych
                                    .map(msg => <MessageItem key={msg.id} message={msg} currentUserId={currentUserId} />) )}
                            {/* Pusty div jako punkt odniesienia do przewijania na dół */}
                            <div ref={messagesEndRef} />
                        </div>
                        {/* Formularz wysyłania wiadomości */}
                        {canInteract ? (
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

                    {/* Oceny (bez zmian) */}
                    <div className="card shadow-sm">
                        <div className="card-header">Oceny</div>
                        <div className="card-body">
                            {ratings.length === 0 ? ( <p className="text-muted text-center">Brak ocen.</p> )
                                : ( <ul className="list-group list-group-flush">{ratings.map(r => <RatingItem key={r.id} rating={r} />)}</ul> )}
                            {/* TODO: Formularz dodawania oceny */}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
TransactionDetails.displayName = 'TransactionDetails';

// --- Inicjalizacja Komponentu (bez zmian) ---
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-transaction-details-container');
    if (container) { /* ... reszta kodu inicjalizacji ... */
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
    } else { /* ... console.warn ... */ }
});