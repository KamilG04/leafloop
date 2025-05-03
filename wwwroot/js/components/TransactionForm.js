// Ścieżka: wwwroot/js/components/TransactionForm.js
// (Przeznaczony do importu w innym komponencie, np. ItemDetails.js)

import React, { useState, useCallback } from 'react';
import ApiService from '../services/api.js'; // Zaimportuj ApiService

// Komponent formularza inicjacji transakcji
const TransactionForm = ({ itemId, itemName }) => { // Odbiera itemId i itemName jako props
    const [message, setMessage] = useState(''); // Stan dla treści wiadomości
    const [submitting, setSubmitting] = useState(false); // Stan wskazujący na proces wysyłania
    const [error, setError] = useState(null); // Stan dla błędów
    const [success, setSuccess] = useState(false); // Stan wskazujący na sukces operacji

    // Funkcja obsługująca wysłanie formularza
    const handleSubmit = useCallback(async (e) => {
        e.preventDefault(); // Zapobiegaj przeładowaniu strony
        setError(null);     // Wyczyść poprzednie błędy
        setSuccess(false);  // Zresetuj stan sukcesu

        // Prosta walidacja
        if (!message.trim()) {
            setError("Wpisz treść wiadomości do właściciela.");
            return;
        }
        if (!itemId || itemId <= 0) {
            setError("Wystąpił błąd: Nieprawidłowe ID przedmiotu.");
            console.error("TransactionForm: Invalid itemId prop received:", itemId);
            return;
        }

        setSubmitting(true); // Rozpocznij wysyłanie
        let transactionId = null; // Zmienna do przechowania ID utworzonej transakcji

        try {
            // --- KROK 1: Inicjacja Transakcji ---
            console.log(`TransactionForm: Inicjowanie transakcji dla itemId: ${itemId}`);
            // Przygotuj dane dla POST /api/transactions (zakładamy, że wystarczy itemId)
            // Jeśli Twoje TransactionCreateDto wymaga więcej pól, dodaj je tutaj.
            const transactionCreateData = {
                itemId: itemId
            };
            // Wywołaj API za pomocą ApiService.post
            // ApiService zwróci obiekt TransactionDto (z pola 'data' odpowiedzi API)
            const createdTransaction = await ApiService.post('/api/transactions', transactionCreateData);

            // Sprawdź, czy otrzymano poprawne ID transakcji
            if (!createdTransaction || typeof createdTransaction.id !== 'number' || createdTransaction.id <= 0) {
                console.error("TransactionForm: Nieprawidłowa odpowiedź po inicjacji transakcji:", createdTransaction);
                throw new Error("Nie udało się zainicjować transakcji lub uzyskać jej ID.");
            }
            transactionId = createdTransaction.id;
            console.log(`TransactionForm: Transakcja zainicjowana, ID: ${transactionId}`);

            // --- KROK 2: Wysłanie Początkowej Wiadomości ---
            console.log(`TransactionForm: Wysyłanie wiadomości dla transakcji ID: ${transactionId}`);
            const messageData = {
                content: message.trim() // Wyślij treść z textarea
            };
            // Wywołaj API do wysłania wiadomości
            await ApiService.post(`/api/transactions/${transactionId}/messages`, messageData);
            console.log(`TransactionForm: Wiadomość początkowa wysłana.`);

            // --- KROK 3: Sukces ---
            setSuccess(true); // Ustaw stan sukcesu
            setMessage('');   // Wyczyść pole wiadomości

        } catch (err) {
            console.error("TransactionForm: Błąd podczas procesu transakcji/wiadomości:", err);
            // Ustaw błąd - użytkownik zobaczy go w alercie
            setError(err.message || "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
            // Można rozważyć logikę cofnięcia transakcji, jeśli wiadomość się nie powiodła, ale to komplikuje
        } finally {
            setSubmitting(false); // Zawsze zakończ stan wysyłania
        }
    }, [itemId, message]); // Zależności hooka useCallback

    // --- Renderowanie Komponentu ---
    return (
        <div className="card shadow-sm mb-4">
            <div className="card-header bg-light">
                <h5 className="mb-0 fs-5">
                    <i className="bi bi-send me-2"></i> {/* Dodana ikona */}
                    Rozpocznij transakcję dla: {itemName || `Przedmiot #${itemId}`}
                </h5>
            </div>
            <div className="card-body">
                {/* Wyświetl komunikat sukcesu i ukryj formularz */}
                {success ? (
                    <div className="alert alert-success">
                        <i className="bi bi-check-circle-fill me-2"></i>
                        Twoje zapytanie zostało wysłane! Właściciel przedmiotu został powiadomiony. Dalszą korespondencję znajdziesz w sekcji 'Moje Transakcje'.
                    </div>
                ) : (
                    // Wyświetl formularz, jeśli nie było sukcesu
                    <form onSubmit={handleSubmit} className="transaction-form">
                        {/* Wyświetl błąd, jeśli wystąpił */}
                        {error && (
                            <div className="alert alert-danger d-flex align-items-center"> {/* Dodano flex dla ikony */}
                                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                                <div>{error}</div> {/* Tekst błędu obok ikony */}
                            </div>
                        )}

                        {/* Pole wiadomości */}
                        <div className="mb-3">
                            <label htmlFor={`message-${itemId}`} className="form-label">
                                Twoja wiadomość do właściciela <span className="text-danger">*</span>
                            </label>
                            <textarea
                                id={`message-${itemId}`} // Unikalne ID na wypadek wielu formularzy
                                className="form-control"
                                rows="4" // Zwiększona liczba wierszy
                                value={message}
                                onChange={(e) => setMessage(e.target.value)}
                                placeholder="Zaproponuj wymianę, zapytaj o szczegóły, potwierdź chęć zakupu, itp."
                                required
                                disabled={submitting} // Wyłącz podczas wysyłania
                                aria-describedby={`messageHelp-${itemId}`}
                            ></textarea>
                            <div id={`messageHelp-${itemId}`} className="form-text">
                                Twoja wiadomość rozpocznie nową transakcję i zostanie wysłana do właściciela.
                            </div>
                        </div>

                        {/* Przycisk wysyłania */}
                        <div className="d-grid gap-2 d-md-flex justify-content-md-end">
                            <button
                                type="submit"
                                className="btn btn-success"
                                // Wyłącz, jeśli trwa wysyłanie lub wiadomość jest pusta (po usunięciu spacji)
                                disabled={submitting || !message.trim()}
                            >
                                {submitting ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                                        Wysyłanie...
                                    </>
                                ) : (
                                    <>
                                        <i className="bi bi-send-fill me-1"></i>
                                        Wyślij Zapytanie
                                    </>
                                )}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
};

// Eksportuj komponent, aby można go było użyć w innym pliku
export default TransactionForm;