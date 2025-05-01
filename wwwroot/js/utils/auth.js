// Pełna ścieżka: wwwroot/js/utils/auth.js

/**
 * Pobiera token JWT z ciasteczka 'jwt_token'.
 * @returns {string|null} Token JWT lub null, jeśli nie znaleziono.
 */
export const getJwtToken = () => {
    const cookie = document.cookie.split('; ').find(row => row.startsWith('jwt_token='));
    return cookie ? cookie.split('=')[1] : null;
};

/**
 * Tworzy obiekt nagłówków HTTP do zapytań API, włączając token JWT.
 * @param {boolean} [includeContentType=true] Czy dołączyć nagłówek 'Content-Type: application/json'.
 * @returns {object} Obiekt nagłówków.
 */
export const getAuthHeaders = (includeContentType = true) => {
    const token = getJwtToken();
    const headers = {
        'Accept': 'application/json', // Zawsze akceptujemy JSON
    };
    if (token) {
        headers['Authorization'] = `Bearer ${token}`; // Dodaj token jeśli istnieje
    }
    if (includeContentType) {
        headers['Content-Type'] = 'application/json'; // Dodaj Content-Type jeśli potrzebny
    }
    return headers;
};

/**
 * Obsługuje odpowiedź z Fetch API, sprawdzając statusy błędów i parsując JSON.
 * Automatycznie przekierowuje na stronę logowania przy błędzie 401.
 * @param {Response} response Obiekt Response z Fetch API.
 * @returns {Promise<object|array|null|true>} Przetworzone dane z odpowiedzi lub true/null dla odpowiedzi bez treści.
 * @throws {Error} Rzuca błąd w przypadku problemów z odpowiedzią (np. status 4xx/5xx).
 */
export const handleApiResponse = async (response) => {
    if (response.status === 401) { // Unauthorized
        console.warn('Błąd 401: Brak autoryzacji. Przekierowywanie do logowania.');
        // Zachowaj obecny URL, aby wrócić po zalogowaniu
        const returnUrl = window.location.pathname + window.location.search;
        window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(returnUrl);
        // Rzuć błąd, aby zatrzymać dalsze przetwarzanie w bloku .then()
        throw new Error('Sesja wygasła lub brak autoryzacji. Zostaniesz przekierowany do strony logowania.');
    }
    if (response.status === 403) { // Forbidden
        console.warn('Błąd 403: Brak uprawnień.');
        throw new Error('Nie masz wystarczających uprawnień do wykonania tej akcji.');
    }
    if (!response.ok) { // Inne błędy (np. 400, 404, 500)
        let errorMessage = `Błąd serwera (status: ${response.status})`;
        try {
            // Spróbuj odczytać szczegóły błędu z odpowiedzi API
            const errorData = await response.json();
            if (typeof errorData === 'string') {
                errorMessage = errorData; // Jeśli błąd to prosty string
            } else {
                // Spróbuj znaleźć komunikat błędu w typowych polach
                errorMessage = errorData.message || errorData.title || errorMessage;
                // Jeśli są błędy walidacji (często w obiekcie 'errors')
                if (errorData.errors && typeof errorData.errors === 'object') {
                    const validationErrors = Object.values(errorData.errors).flat().join(' ');
                    if (validationErrors) {
                        errorMessage += `: ${validationErrors}`;
                    }
                }
            }
        } catch (e) {
            // Błąd parsowania JSON - użyj domyślnego komunikatu
            console.warn("Nie udało się sparsować odpowiedzi błędu jako JSON.", e);
        }
        console.error(`Błąd API (${response.status}): ${errorMessage}`);
        throw new Error(errorMessage); // Rzuć błąd z komunikatem
    }

    // Odpowiedź jest OK (2xx)
    if (response.status === 204) { // 204 No Content
        return null; // Sukces, ale brak danych w ciele odpowiedzi
    }

    // Spróbuj zwrócić JSON (dla 200 OK, 201 Created z ciałem)
    try {
        // Sprawdź Content-Type, aby uniknąć błędów parsowania np. plików
        const contentType = response.headers.get("content-type");
        if (contentType && contentType.indexOf("application/json") !== -1) {
            return await response.json();
        } else {
            // Jeśli to nie JSON, ale odpowiedź jest OK, zwróć true (sukces)
            // lub można zwrócić tekst: return await response.text();
            return true;
        }
    } catch (e) {
        // Błąd parsowania JSON, mimo statusu 2xx? Mało prawdopodobne, ale możliwe.
        console.warn("Błąd parsowania JSON odpowiedzi sukcesu:", e);
        // Spróbuj sprawdzić nagłówek Location dla 201 Created bez ciała
        const locationHeader = response.headers.get('Location');
        if (locationHeader && response.status === 201) {
            const parts = locationHeader.split('/');
            const id = parts[parts.length - 1];
            // Zwróć obiekt z ID, aby można było go użyć (np. do przekierowania)
            if (!isNaN(parseInt(id))) {
                return { id: parseInt(id) };
            }
        }
        return true; // Sukces, ale bez danych JSON
    }
};