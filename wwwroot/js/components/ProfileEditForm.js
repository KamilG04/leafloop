// Ścieżka: wwwroot/js/components/ProfileEditForm.js
import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';

const ProfileEditForm = ({ userId }) => {
    // Stany dla danych użytkownika i adresu
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [address, setAddress] = useState({ street: '', buildingNumber: '', apartmentNumber: '', postalCode: '', city: '', province: '', country: '' });

    // Stany dla awatara
    const [initialAvatarPath, setInitialAvatarPath] = useState(null);
    const [avatarFile, setAvatarFile] = useState(null);
    const [avatarPreview, setAvatarPreview] = useState(null);

    // Stany UI
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);

    // Funkcja do ładowania początkowych danych
    const fetchInitialData = useCallback(async () => {
        if (!userId || userId <= 0) { setError("Błąd: Nieprawidłowe ID użytkownika dla formularza."); setLoading(false); return; }
        setLoading(true); setError(null); setAvatarFile(null);
        console.log(`ProfileEditForm: Fetching initial data for UserID: ${userId}`);
        try {
            const userData = await ApiService.get(`/api/users/${userId}`);
            if (!userData) throw new Error("Nie znaleziono danych użytkownika.");

            console.log("ProfileEditForm: Received initial data:", userData);
            setFirstName(userData.firstName || '');
            setLastName(userData.lastName || '');
            setAddress({
                street: userData.address?.street || '',
                buildingNumber: userData.address?.buildingNumber || '',
                apartmentNumber: userData.address?.apartmentNumber || '',
                postalCode: userData.address?.postalCode || '',
                city: userData.address?.city || '',
                province: userData.address?.province || '',
                country: userData.address?.country || ''
            });
            setInitialAvatarPath(userData.avatarPath || null);
            setAvatarPreview(ApiService.getImageUrl(userData.avatarPath));

        } catch (err) {
            console.error("ProfileEditForm: Error fetching initial data:", err);
            setError(`Nie udało się załadować danych do edycji: ${err.message}`);
        } finally { setLoading(false); }
    }, [userId]);

    useEffect(() => { fetchInitialData(); }, [fetchInitialData]);

    // Efekt czyszczący dla URL podglądu Blob
    useEffect(() => {
        return () => {
            if (avatarPreview && avatarPreview.startsWith('blob:')) {
                URL.revokeObjectURL(avatarPreview);
                console.log("ProfileEditForm: Revoked blob URL:", avatarPreview);
            }
        };
    }, [avatarPreview]);

    // Handler dla zmiany awatara
    const handleAvatarChange = (event) => {
        const file = event.target.files?.[0];
        if (!file) { setAvatarFile(null); setAvatarPreview(ApiService.getImageUrl(initialAvatarPath)); setError(null); return; }

        const MAX_SIZE_MB = 2; const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024;
        const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
        if (!ALLOWED_TYPES.includes(file.type)) { setError(`Nieprawidłowy typ pliku. Dozwolone: JPG, PNG, WEBP.`); event.target.value = null; setAvatarFile(null); setAvatarPreview(ApiService.getImageUrl(initialAvatarPath)); return; }
        if (file.size > MAX_SIZE_BYTES) { setError(`Plik jest za duży (max ${MAX_SIZE_MB}MB).`); event.target.value = null; setAvatarFile(null); setAvatarPreview(ApiService.getImageUrl(initialAvatarPath)); return; }

        setError(null); setAvatarFile(file);
        if (avatarPreview && avatarPreview.startsWith('blob:')) { URL.revokeObjectURL(avatarPreview); }
        const newPreviewUrl = URL.createObjectURL(file);
        setAvatarPreview(newPreviewUrl);
        console.log("ProfileEditForm: Set new avatar preview (blob):", newPreviewUrl);
    };

    // ===>>> DODANA FUNKCJA handleAddressChange <<<===
    const handleAddressChange = useCallback((e) => {
        const { name, value } = e.target;
        // Używamy funkcji zwrotnej w setAddress, aby zapewnić dostęp do poprzedniego stanu
        // i aktualizujemy tylko zmienione pole dzięki [name]
        setAddress(prevAddress => ({
            ...prevAddress, // Skopiuj istniejące pola adresu
            [name]: value    // Zaktualizuj pole o nazwie pasującej do 'name' inputa
        }));
    }, []); // Pusta tablica zależności, bo używa tylko setAddress, które jest stabilne
    // ===>>> KONIEC DODANEJ FUNKCJI <<<===

    // Handler wysłania formularza (bez zmian od ostatniej wersji)
    const handleSubmit = useCallback(async (e) => {
        e.preventDefault();
        if (!firstName.trim() || !lastName.trim()) { setError("Imię i Nazwisko są wymagane."); return; }

        setSubmitting(true); setError(null); setSuccess(null);
        const userUpdateDto = { id: userId, firstName: firstName.trim(), lastName: lastName.trim() };
        const addressDto = {
            street: address.street.trim(), buildingNumber: address.buildingNumber.trim(), apartmentNumber: address.apartmentNumber.trim(),
            postalCode: address.postalCode.trim(), city: address.city.trim(), province: address.province.trim(), country: address.country.trim()
        };

        try {
            console.log("ProfileEditForm: Updating text data...");
            const updateProfilePromise = ApiService.put(`/api/users/${userId}`, userUpdateDto);
            const updateAddressPromise = ApiService.put(`/api/users/${userId}/address`, addressDto);
            await Promise.all([updateProfilePromise, updateAddressPromise]);
            console.log("ProfileEditForm: Text data updated successfully.");

            if (avatarFile) {
                console.log(`ProfileEditForm: Uploading new avatar: ${avatarFile.name}`);
                const formData = new FormData();
                formData.append('avatar', avatarFile, avatarFile.name);
                await ApiService.postFormData(`/api/users/${userId}/avatar`, formData);
                console.log("ProfileEditForm: Avatar uploaded successfully.");
                setAvatarFile(null);
            }

            setSuccess("Profil został pomyślnie zaktualizowany!");
            setTimeout(() => { window.location.href = `/Profile/Index`; }, 1500);

        } catch (err) {
            console.error("ProfileEditForm: Error updating profile/avatar:", err);
            setError(`Błąd aktualizacji: ${err.message}`);
            setSubmitting(false);
        }
    }, [userId, firstName, lastName, address, avatarFile]);

    // --- Renderowanie ---

    if (loading) { /* ... spinner ... */
        return (
            <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status"><span className="visually-hidden">Ładowanie...</span></div>
            </div>
        );
    }
    if (error && !submitting && !firstName && !lastName) { /* ... błąd ładowania ... */
        return <div className="alert alert-danger">Błąd ładowania danych: {error}</div>;
    }

    return (
        <div className="card shadow-sm">
            <div className="card-header"><h4 className="mb-0">Edytuj Profil</h4></div>
            <div className="card-body">
                {success && <div className="alert alert-success">{success}</div>}
                {error && !success && <div className="alert alert-danger">{error}</div>}

                <form onSubmit={handleSubmit} noValidate>
                    {/* Sekcja Awatara (bez zmian) */}
                    <div className="mb-4 text-center">
                        <label htmlFor="avatarUpload" className="form-label d-block mb-2 fw-bold">Zdjęcie profilowe</label>
                        <div>
                            <img src={avatarPreview || ApiService.getImageUrl(null)} alt="Podgląd awatara" className="img-thumbnail rounded-circle mb-2 shadow-sm" style={{ width: '150px', height: '150px', objectFit: 'cover', cursor: 'pointer', border: '2px solid #dee2e6' }} onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} onClick={() => document.getElementById('avatarUpload')?.click()} title="Kliknij, aby zmienić awatar"/>
                        </div>
                        <input type="file" className="form-control d-none" id="avatarUpload" accept="image/png, image/jpeg, image/webp" onChange={handleAvatarChange} disabled={submitting}/>
                        <small className="form-text text-muted d-block">Kliknij na obrazek, aby wybrać nowy (max 2MB, JPG/PNG/WEBP).</small>
                    </div>

                    {/* Dane podstawowe (bez zmian) */}
                    <h5 className="mb-3 border-bottom pb-2">Dane Podstawowe</h5>
                    <div className="row g-3 mb-3">
                        <div className="col-md-6">
                            <label htmlFor="firstName" className="form-label">Imię <span className="text-danger">*</span></label>
                            <input type="text" className="form-control" id="firstName" value={firstName} onChange={e => setFirstName(e.target.value)} required disabled={submitting} />
                        </div>
                        <div className="col-md-6">
                            <label htmlFor="lastName" className="form-label">Nazwisko <span className="text-danger">*</span></label>
                            <input type="text" className="form-control" id="lastName" value={lastName} onChange={e => setLastName(e.target.value)} required disabled={submitting} />
                        </div>
                    </div>

                    {/* Adres (teraz używa poprawnego handlera) */}
                    <h5 className="mb-3 border-bottom pb-2">Adres</h5>
                    <div className="row g-3">
                        <div className="col-md-8"><label htmlFor="street" className="form-label">Ulica</label><input type="text" className="form-control" id="street" name="street" value={address.street} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-4"><label htmlFor="buildingNumber" className="form-label">Nr budynku</label><input type="text" className="form-control" id="buildingNumber" name="buildingNumber" value={address.buildingNumber} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-4"><label htmlFor="apartmentNumber" className="form-label">Nr mieszkania</label><input type="text" className="form-control" id="apartmentNumber" name="apartmentNumber" value={address.apartmentNumber} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-4"><label htmlFor="postalCode" className="form-label">Kod pocztowy</label><input type="text" className="form-control" id="postalCode" name="postalCode" value={address.postalCode} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-4"><label htmlFor="city" className="form-label">Miejscowość</label><input type="text" className="form-control" id="city" name="city" value={address.city} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-6"><label htmlFor="province" className="form-label">Województwo</label><input type="text" className="form-control" id="province" name="province" value={address.province} onChange={handleAddressChange} disabled={submitting}/></div>
                        <div className="col-md-6"><label htmlFor="country" className="form-label">Kraj</label><input type="text" className="form-control" id="country" name="country" value={address.country} onChange={handleAddressChange} disabled={submitting}/></div>
                    </div>

                    {/* Przyciski (bez zmian) */}
                    <div className="d-flex justify-content-end mt-4 gap-2">
                        <a href="/Profile/Index" className="btn btn-secondary" disabled={submitting}>Anuluj</a>
                        <button type="submit" className="btn btn-success" disabled={submitting}>
                            {submitting ? ( <><span className="spinner-border spinner-border-sm me-2"></span>Zapisywanie...</> ) : ( <><i className="bi bi-check-lg me-1"></i> Zapisz zmiany</> )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};
ProfileEditForm.displayName = 'ProfileEditForm';

// --- Inicjalizacja Komponentu (bez zmian) ---
const container = document.getElementById('react-profile-edit-form-container');
if (container) { /* ... reszta kodu inicjalizacji ... */
    const userIdString = container.getAttribute('data-user-id');
    const userId = parseInt(userIdString, 10);
    if (!isNaN(userId) && userId > 0) {
        const root = ReactDOM.createRoot(container);
        root.render(<StrictMode><ProfileEditForm userId={userId} /></StrictMode>);
        console.log(`ProfileEditForm component initialized for UserID: ${userId}`);
    } else {
        console.error(`ProfileEditForm: Invalid or missing user ID... Value found: "${userIdString}". Parsed as: ${userId}.`);
        container.innerHTML = '<div class="alert alert-danger">Błąd krytyczny... Brak ID użytkownika.</div>';
    }
} else { /* ... console.warn ... */ }