// Pełna ścieżka: wwwroot/js/components/userProfile.js

import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Użyj ApiService
import { getCurrentUserId } from '../utils/auth.js'; // Zachowaj do sprawdzania właściciela

// ... reszta kodu komponentu UserProfile (bez zmian) ...
// Komponent UserProfile przepisany na ApiService
const UserProfile = ({ userId }) => {
    const [user, setUser] = useState(null); // Przechowuje dane użytkownika (np. UserWithDetailsDto)
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Użyj useCallback dla stabilności funkcji
    const fetchUserData = useCallback(async () => {
        // Walidacja ID na początku (choć inicjalizacja też to robi)
        if (!userId || userId <= 0) {
            setError("Nieprawidłowe ID użytkownika.");
            setLoading(false);
            return;
        }
        console.log(`UserProfile: Rozpoczęcie pobierania danych dla userId: ${userId}`);
        setLoading(true);
        setError(null); // Resetuj błąd

        try {
            // Użyj ApiService.get do pobrania szczegółów użytkownika
            // Zakładamy endpoint /api/users/{id} zwracający UserWithDetailsDto
            const userData = await ApiService.get(`/api/users/${userId}`);
            console.log("UserProfile: Odebrane dane użytkownika z ApiService:", userData);

            // ApiService zwraca już pole 'data', więc userData to obiekt użytkownika
            if (!userData) {
                // Może się zdarzyć, jeśli API zwróciło sukces, ale puste 'data'
                throw new Error("Nie znaleziono danych użytkownika w odpowiedzi API.");
            }

            setUser(userData); // Ustaw stan użytkownika

        } catch (err) {
            console.error("UserProfile: Błąd podczas pobierania danych użytkownika:", err);
            // Ustaw komunikat błędu z obiektu błędu lub domyślny
            setError(err.message || 'Wystąpił błąd podczas ładowania profilu.');
            setUser(null); // Wyczyść dane w razie błędu
        } finally {
            setLoading(false); // Zakończ ładowanie
        }
    }, [userId]); // Zależność od userId

    // Efekt do pobrania danych
    useEffect(() => {
        fetchUserData();
    }, [fetchUserData]); // Zależność od memoizowanej funkcji

    // --- Renderowanie Komponentu (logika renderowania bez zmian) ---

    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie profilu...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center" role="alert">
                <span><i className="bi bi-exclamation-triangle-fill me-2"></i>{error}</span>
                {/* Przycisk ponowienia */}
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchUserData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                </button>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="alert alert-warning" role="alert">
                <i className="bi bi-info-circle-fill me-2"></i>
                Nie można załadować danych profilu lub użytkownik nie istnieje.
            </div>
        );
    }

    // Pomyślne załadowanie - renderowanie danych profilu
    // Użyj ApiService.getImageUrl dla avatara i ikon odznak
    const defaultAvatar = ApiService.getImageUrl(null); // Pobierz domyślny placeholder

    const { firstName, lastName, email, avatarPath, ecoScore, createdDate, lastActivity, address, averageRating, badges } = user;

    return (
        <div className="card shadow-sm">
            <div className="card-body">
                <div className="row">
                    {/* Kolumna lewa */}
                    <div className="col-md-4 text-center mb-3 mb-md-0">
                        <img
                            src={ApiService.getImageUrl(avatarPath)} // Użyj helpera ApiService
                            alt={`${firstName || ''} ${lastName || ''} avatar`}
                            className="img-fluid rounded-circle mb-3 shadow-sm"
                            style={{ width: '150px', height: '150px', objectFit: 'cover' }}
                            onError={(e) => { if (e.target.src !== defaultAvatar) e.target.src = defaultAvatar; }}
                        />
                        {/* EcoScore i Ocena (bez zmian logiki) */}
                        {ecoScore !== undefined && (
                            <div className="mt-2">
                                 <span className="badge bg-success fs-6">
                                     <i className="bi bi-leaf-fill me-1"></i>
                                     EcoScore: {ecoScore}
                                 </span>
                            </div>
                        )}
                        {averageRating !== undefined && averageRating > 0 && (
                            <div className="mt-2">
                                 <span className="badge bg-warning text-dark fs-6">
                                     <i className="bi bi-star-fill me-1"></i>
                                     Ocena: {averageRating.toFixed(1)}/5.0
                                 </span>
                            </div>
                        )}
                    </div>

                    {/* Kolumna prawa */}
                    <div className="col-md-8">
                        <h2>{firstName || 'Użytkownik'} {lastName || ''}</h2>
                        {/* Info (bez zmian logiki) */}
                        {email && (
                            <p className="text-muted mb-2">
                                <i className="bi bi-envelope-fill me-2 text-success"></i>{email}
                            </p>
                        )}
                        {createdDate && (
                            <p className="card-text mb-1">
                                <small className="text-muted">
                                    <i className="bi bi-calendar-check me-2"></i>
                                    Dołączył(a): {new Date(createdDate).toLocaleDateString('pl-PL')}
                                </small>
                            </p>
                        )}
                        {lastActivity && (
                            <p className="card-text mb-3">
                                <small className="text-muted">
                                    <i className="bi bi-clock-history me-2"></i>
                                    Ostatnia aktywność: {new Date(lastActivity).toLocaleString('pl-PL')}
                                </small>
                            </p>
                        )}

                        {/* Adres (bez zmian logiki) */}
                        {address && (address.city || address.country) ? (
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-geo-alt-fill me-2 text-success"></i>Lokalizacja</h5>
                                <address className="mb-0" style={{ lineHeight: '1.4' }}> {/* Poprawiona czytelność adresu */}
                                    {address.street && <>{address.street}<br /></>}
                                    {address.buildingNumber && <>{address.buildingNumber}</>}
                                    {address.apartmentNumber && <>{`, m. ${address.apartmentNumber}`}{(address.postalCode || address.city) ? <br /> : ''}</>}
                                    {(!address.buildingNumber && address.apartmentNumber && (address.postalCode || address.city)) && <br />}
                                    {address.postalCode && <>{address.postalCode}{` `}</>}
                                    {address.city && <>{address.city}{(address.province || address.country) ? <br /> : ''}</>}
                                    {address.province && <>{address.province}{(address.country && address.province !== address.country) ? <br /> : ''}</>}
                                    {address.country && address.province !== address.country && <>{address.country}</>}
                                </address>
                            </div>
                        ) : (
                            <p className="text-muted mt-3 pt-3 border-top">Brak informacji o lokalizacji.</p>
                        )}

                        {/* Odznaki */}
                        {badges && badges.length > 0 && (
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-trophy-fill me-2 text-success"></i>Odznaki</h5>
                                <div className="d-flex flex-wrap gap-2">
                                    {badges.map(badge => (
                                        <span key={badge.id} className="badge bg-secondary p-2 d-inline-flex align-items-center" title={badge.description || badge.name}> {/* Dodano fallback dla title */}
                                            {/* Użyj ApiService.getImageUrl dla ikony odznaki */}
                                            <img
                                                src={ApiService.getImageUrl(badge.iconPath)}
                                                alt="" // Pusty alt, bo nazwa jest obok
                                                className="me-1"
                                                style={{ height: '1.1em', width: 'auto', verticalAlign: 'middle' }}
                                                // onError={(e) => { e.target.style.display = 'none'; e.target.nextSibling.style.display='inline'; }} // Opcjonalny fallback na ikonę fontu
                                            />
                                            {/* <i className="bi bi-patch-check-fill me-1" style={{ display: 'none' }}></i> Ikona fontu jako fallback */}
                                            <span style={{ verticalAlign: 'middle' }}>{badge.name}</span>
                                        </span>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

// --- Inicjalizacja Komponentu (logika bez zmian, ale komunikaty poprawione) ---
const container = document.getElementById('react-user-profile');
if (container) {
    const userIdString = container.getAttribute('data-user-id');
    const userId = parseInt(userIdString, 10);

    if (!isNaN(userId) && userId > 0) {
        const root = ReactDOM.createRoot(container);
        root.render(
            <StrictMode>
                <UserProfile userId={userId} />
            </StrictMode>
        );
        console.log(`UserProfile component initialized for UserID: ${userId}`);
    } else {
        // Błąd krytyczny - komponent nie może działać bez poprawnego ID
        console.error(`UserProfile: Invalid or missing user ID in data-user-id attribute. Value found: "${userIdString}". Parsed as: ${userId}`);
        container.innerHTML = '<div class="alert alert-danger">Błąd krytyczny: Nie można załadować profilu. Brak poprawnego ID użytkownika w atrybucie HTML.</div>';
    }
} else {
    // To ostrzeżenie jest normalne na stronach, gdzie profil nie jest wyświetlany
    // console.warn("Container element '#react-user-profile' not found on this page.");
}