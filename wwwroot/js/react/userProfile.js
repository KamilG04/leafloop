// Pełna ścieżka: wwwroot/js/components/userProfile.js
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client'; // Poprawny import dla React 18+
import { getAuthHeaders, handleApiResponse, getResponseData } from '../utils/auth.js'; // Używamy helperów

const UserProfile = ({ userId }) => {
    const [user, setUser] = useState(null); // Przechowuje dane użytkownika (np. UserWithDetailsDto)
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchUserData = async () => {
            if (!userId || userId <= 0) {
                setError("Nieprawidłowe ID użytkownika.");
                setLoading(false);
                return;
            }
            console.log(`UserProfile: Rozpoczęcie pobierania danych dla userId: ${userId}`);
            setLoading(true);
            setError(null); // Resetuj błąd przy nowym ładowaniu

            try {
                // Wywołaj endpoint API do pobierania szczegółów użytkownika
                const response = await fetch(`/api/users/${userId}`, {
                    headers: getAuthHeaders(false) // Użyj helpera do nagłówków
                });

                // Użyj helpera do obsługi odpowiedzi (sprawdza status, parsuje JSON)
                const result = await handleApiResponse(response);
                console.log("UserProfile: Surowa odpowiedź API:", result);

                // Użyj helpera do wyciągnięcia danych z obiektu ApiResponse<T>
                const userData = getResponseData(result);
                console.log("UserProfile: Wyciągnięte dane użytkownika:", userData);

                if (!userData) {
                    // Jeśli getResponseData zwróciło null/undefined, mimo że nie było błędu HTTP
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
        };

        fetchUserData();
    }, [userId]); // Efekt uruchamia się ponownie, gdy zmieni się userId

    // --- Renderowanie Komponentu ---

    // Stan ładowania
    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie profilu...</span>
                </div>
            </div>
        );
    }

    // Stan błędu
    if (error) {
        return (
            <div className="alert alert-danger" role="alert">
                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                {error}
            </div>
        );
    }

    // Stan braku użytkownika (po zakończeniu ładowania i bez błędu)
    if (!user) {
        return (
            <div className="alert alert-warning" role="alert">
                <i className="bi bi-info-circle-fill me-2"></i>
                Nie można załadować danych profilu lub użytkownik nie istnieje.
            </div>
        );
    }

    // Pomyślne załadowanie - renderowanie danych profilu
    const defaultAvatar = "/img/default-avatar.png"; // Ścieżka do domyślnego avatara

    // Funkcja pomocnicza do bezpiecznego uzyskania ścieżki avatara
    const getSafeAvatarPath = (path) => {
        if (!path) return defaultAvatar;
        if (path.startsWith('http://') || path.startsWith('https://')) return path;
        return path.startsWith('/') ? path : `/${path}`;
    };

    // Destrukturyzacja danych użytkownika (zakładając strukturę UserWithDetailsDto)
    const { firstName, lastName, email, avatarPath, ecoScore, createdDate, lastActivity, address, averageRating, badges } = user;

    return (
        <div className="card shadow-sm">
            {/* Można dodać card-header, jeśli potrzeba */}
            <div className="card-body">
                <div className="row">
                    {/* Kolumna lewa - Avatar i EcoScore */}
                    <div className="col-md-4 text-center mb-3 mb-md-0">
                        <img
                            src={getSafeAvatarPath(avatarPath)}
                            alt={`${firstName || ''} ${lastName || ''} avatar`}
                            className="img-fluid rounded-circle mb-3 shadow-sm"
                            style={{ width: '150px', height: '150px', objectFit: 'cover' }}
                            onError={(e) => { if (e.target.src !== defaultAvatar) e.target.src = defaultAvatar; }}
                        />
                        {ecoScore !== undefined && (
                            <div className="mt-2">
                                 <span className="badge bg-success fs-6">
                                     <i className="bi bi-leaf-fill me-1"></i>
                                     EcoScore: {ecoScore}
                                 </span>
                            </div>
                        )}
                        {/* Można dodać średnią ocenę */}
                        {averageRating !== undefined && averageRating > 0 && (
                            <div className="mt-2">
                                 <span className="badge bg-warning text-dark fs-6">
                                     <i className="bi bi-star-fill me-1"></i>
                                     Ocena: {averageRating.toFixed(1)}/5.0
                                 </span>
                            </div>
                        )}
                    </div>

                    {/* Kolumna prawa - Informacje */}
                    <div className="col-md-8">
                        <h2>{firstName || 'Użytkownik'} {lastName || ''}</h2>
                        {email && ( // Wyświetlaj email tylko jeśli istnieje
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

                        {/* Adres */}
                        {address && (address.city || address.country) ? (
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-geo-alt-fill me-2 text-success"></i>Lokalizacja</h5>
                                <address className="mb-0">
                                    {address.street && <>{address.street}<br /></>}
                                    {address.buildingNumber && <>{address.buildingNumber}</>}
                                    {address.apartmentNumber && <>{`, m. ${address.apartmentNumber}`}<br /></>}
                                    {(!address.buildingNumber && address.apartmentNumber) && <><br /></>}
                                    {address.postalCode && <>{address.postalCode}{` `}</>}
                                    {address.city && <>{address.city}<br /></>}
                                    {address.province && <>{address.province}<br /></>}
                                    {address.country && <>{address.country}</>}
                                </address>
                            </div>
                        ) : (
                            <p className="text-muted mt-3 pt-3 border-top">Brak informacji o lokalizacji.</p>
                        )}

                        {/* Odznaki (jeśli są w DTO) */}
                        {badges && badges.length > 0 && (
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-trophy-fill me-2 text-success"></i>Odznaki</h5>
                                <div className="d-flex flex-wrap gap-2">
                                    {badges.map(badge => (
                                        <span key={badge.id} className="badge bg-secondary p-2 d-inline-flex align-items-center" title={badge.description}>
                                            {badge.iconPath ? (
                                                <img src={badge.iconPath} alt={badge.name} className="me-1" style={{ height: '1.1em', width: 'auto', verticalAlign: 'middle' }} />
                                            ) : (
                                                <i className="bi bi-patch-check-fill me-1"></i> // Domyślna ikona
                                            )}
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

// --- Inicjalizacja Komponentu ---
// Ta część kodu jest teraz bardziej standardowa dla React 18
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
        console.error("UserProfile: Invalid or missing user ID in data-user-id attribute:", userIdString);
        container.innerHTML = '<div class="alert alert-danger">Błąd: Nie można załadować profilu z powodu nieprawidłowego ID użytkownika.</div>';
    }
} else {
    // Ten komunikat może pojawić się na stronach, gdzie ten kontener nie istnieje - to normalne.
    // console.warn("Container element '#react-user-profile' not found on this page.");
}

