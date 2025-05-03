// Pełna ścieżka: wwwroot/js/components/itemCreateForm.js (PEŁNY KOD - Wersja z ApiService)

import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Zaimportuj ApiService

const ItemCreateForm = ({ initialCategories = [] }) => {
    // Stany dla pól formularza
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState(''); // Cena jako string dla inputu
    const [categoryId, setCategoryId] = useState(''); // ID kategorii jako string dla selecta
    const [condition, setCondition] = useState('Used'); // Domyślna wartość
    const [isForExchange, setIsForExchange] = useState(false);
    const [photos, setPhotos] = useState([]); // Tablica obiektów File

    // Stany UI
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);
    const [categories, setCategoriesState] = useState(initialCategories);
    const [loadingCategories, setLoadingCategories] = useState(initialCategories.length === 0);

    // Pobieranie kategorii z API (jeśli nie zostały dostarczone)
    const fetchCategories = useCallback(async () => {
        console.log("ItemCreateForm: Fetching categories via API...");
        setError(null); // Czyść błędy przy próbie pobrania kategorii
        try {
            const data = await ApiService.get('/api/categories'); // Użyj ApiService
            setCategoriesState(Array.isArray(data) ? data : []); // Upewnij się, że to tablica
            console.log("ItemCreateForm: Categories loaded:", data);
        } catch (err) {
            console.error("ItemCreateForm: Error loading categories:", err);
            setError("Nie udało się załadować listy kategorii. Spróbuj odświeżyć stronę lub skontaktuj się z administratorem.");
            setCategoriesState([]); // Ustaw pustą tablicę w razie błędu
        } finally {
            setLoadingCategories(false);
        }
    }, []); // Pusta tablica zależności - funkcja się nie zmienia

    // Efekt do pobrania kategorii przy montowaniu komponentu
    useEffect(() => {
        if (initialCategories.length === 0 && loadingCategories) { // Sprawdź też loadingCategories, aby uniknąć wielokrotnego wywołania
            fetchCategories();
        }
    }, [initialCategories, loadingCategories, fetchCategories]); // Zależności useEffect

    // Obsługa zmiany plików zdjęć (z walidacją)
    const handlePhotoChange = (event) => {
        const files = Array.from(event.target.files); // Konwertuj FileList na tablicę
        const MAX_PHOTOS = 5;
        const MAX_SIZE_MB = 5;
        const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024;
        const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

        setError(null); // Wyczyść poprzednie błędy

        if (files.length === 0) { // Jeśli użytkownik anulował wybór
            setPhotos([]);
            return;
        }

        if (files.length > MAX_PHOTOS) {
            setError(`Można dodać maksymalnie ${MAX_PHOTOS} zdjęć.`);
            setPhotos([]);
            event.target.value = null; // Zresetuj input file
            return;
        }

        const oversizedFiles = files.filter(file => file.size > MAX_SIZE_BYTES);
        if (oversizedFiles.length > 0) {
            setError(`Następujące pliki są za duże (max ${MAX_SIZE_MB}MB): ${oversizedFiles.map(f => f.name).join(', ')}`);
            setPhotos([]);
            event.target.value = null;
            return;
        }

        const invalidTypeFiles = files.filter(file => !ALLOWED_TYPES.includes(file.type));
        if (invalidTypeFiles.length > 0) {
            setError(`Nieprawidłowy typ pliku dla: ${invalidTypeFiles.map(f => f.name).join(', ')}. Dozwolone: JPG, PNG, WEBP.`);
            setPhotos([]);
            event.target.value = null;
            return;
        }

        setPhotos(files); // Zapisz poprawne pliki
    };

    // Obsługa wysłania formularza (używając ApiService)
    const handleSubmit = useCallback(async (event) => {
        event.preventDefault();
        setError(null);
        setSuccess(null);

        // --- Walidacja ---
        if (!name.trim() || !description.trim() || !categoryId || !condition) {
            setError("Pola: Nazwa, Opis, Kategoria i Stan są wymagane.");
            window.scrollTo(0, 0); // Przewiń do góry, aby zobaczyć błąd
            return;
        }
        const parsedCategoryId = parseInt(categoryId, 10);
        if (isNaN(parsedCategoryId) || parsedCategoryId <= 0) {
            setError("Wybierz poprawną kategorię.");
            window.scrollTo(0, 0);
            return;
        }
        const parsedPrice = isForExchange || !price.trim() ? 0 : parseFloat(price);
        if (isNaN(parsedPrice) || parsedPrice < 0) {
            setError("Wartość/Cena musi być poprawną, nieujemną liczbą (lub pole musi być puste).");
            window.scrollTo(0, 0);
            return;
        }
        if (photos.length === 0) {
            setError("Dodaj przynajmniej jedno zdjęcie.");
            window.scrollTo(0, 0);
            return;
        }
        // --- Koniec Walidacji ---

        setSubmitting(true); // Rozpocznij proces wysyłania

        // Przygotuj DTO dla API (ItemCreateDto)
        const itemData = {
            name: name.trim(),
            description: description.trim(),
            expectedValue: parsedPrice,
            isForExchange: isForExchange,
            categoryId: parsedCategoryId,
            condition: condition,
            tagIds: [] // Załóżmy brak tagów w tym formularzu
        };

        console.log("ItemCreateForm: Submitting item data:", itemData);
        let newItemId = null; // Zmienna na ID utworzonego przedmiotu

        try {
            // KROK 1: Utwórz przedmiot (dane tekstowe) za pomocą ApiService.post
            const createdItemDto = await ApiService.post('/api/items', itemData);

            // ApiService.post zwraca już pole 'data' z odpowiedzi, które powinno być obiektem ItemDto
            if (!createdItemDto || typeof createdItemDto.id !== 'number' || createdItemDto.id <= 0) {
                console.error("ItemCreateForm: Invalid response after creating item:", createdItemDto);
                throw new Error("Nie udało się utworzyć przedmiotu lub uzyskać poprawnego ID z odpowiedzi API.");
            }
            newItemId = createdItemDto.id;
            console.log("ItemCreateForm: Item created successfully, ID:", newItemId);


            // KROK 2: Wyślij zdjęcia (używając ApiService.postFormData - upewnij się, że istnieje!)
            if (photos.length > 0) {
                console.log(`ItemCreateForm: Uploading ${photos.length} photos for item ID: ${newItemId}...`);
                let firstPhotoError = null; // Zapamiętaj pierwszy błąd

                // Lepsze podejście: wysyłaj równolegle z Promise.all dla wydajności
                const uploadPromises = photos.map(async (photoFile) => {
                    const formData = new FormData();
                    formData.append('photo', photoFile, photoFile.name); // Klucz 'photo' musi pasować do parametru API
                    const photoUploadUrl = `/api/items/${newItemId}/photos`;
                    console.log(`ItemCreateForm: Uploading ${photoFile.name} to ${photoUploadUrl}`);
                    try {
                        // Użyj ApiService.postFormData
                        await ApiService.postFormData(photoUploadUrl, formData);
                        console.log(`ItemCreateForm: Photo ${photoFile.name} uploaded successfully.`);
                    } catch(uploadErr) {
                        console.error(`ItemCreateForm: Failed to upload photo ${photoFile.name}:`, uploadErr);
                        // Zapisz pierwszy błąd, ale pozwól innym się zakończyć
                        if (!firstPhotoError) {
                            firstPhotoError = uploadErr;
                        }
                    }
                });

                // Poczekaj na zakończenie wszystkich prób wysłania
                await Promise.all(uploadPromises);

                // Jeśli wystąpił jakikolwiek błąd podczas wysyłania zdjęć, rzuć go teraz
                if (firstPhotoError) {
                    throw new Error(`Przedmiot został utworzony (ID: ${newItemId}), ale wystąpił błąd podczas wysyłania przynajmniej jednego zdjęcia: ${firstPhotoError.message}`);
                }
                console.log("ItemCreateForm: All photo uploads attempted.");
            }

            // KROK 3: Sukces całego procesu
            setSuccess(`Przedmiot "${name}" został pomyślnie dodany! Za chwilę nastąpi przekierowanie...`);
            // Wyczyść formularz
            setName('');
            setDescription('');
            setPrice('');
            setCategoryId('');
            setCondition('Used');
            setIsForExchange(false);
            setPhotos([]);
            const photoInput = document.getElementById('itemPhotos');
            if (photoInput) photoInput.value = null; // Zresetuj input file

            // Przekierowanie
            setTimeout(() => {
                if (newItemId) { // Upewnij się, że mamy ID
                    window.location.href = `/Items/Details/${newItemId}`;
                } else {
                    window.location.href = '/Items/MyItems'; // Fallback, jeśli ID z jakiegoś powodu zniknęło
                }
            }, 2500); // Dłuższy czas na przeczytanie komunikatu

        } catch (err) {
            // Obsługa błędów z kroku 1 (tworzenie itemu) lub kroku 2 (upload zdjęć)
            console.error("ItemCreateForm: Error during item creation process:", err);
            setError(err.message || "Wystąpił nieoczekiwany błąd podczas dodawania przedmiotu.");
            // Nie czyść formularza przy błędzie, aby użytkownik mógł poprawić
        } finally {
            setSubmitting(false); // Zawsze zakończ stan wysyłania
        }
    }, [name, description, price, categoryId, condition, isForExchange, photos]); // Zależności dla handleSubmit

    // --- Renderowanie JSX Formularza ---
    return (
        <div className="card shadow-sm mt-4">
            <div className="card-header bg-success text-white">
                <h4 className="mb-0"><i className="bi bi-plus-circle-fill me-2"></i>Dodaj Nowy Przedmiot</h4>
            </div>
            <div className="card-body">
                {/* Komunikaty o błędach i sukcesie */}
                {error && <div className="alert alert-danger" role="alert">{error}</div>}
                {success && <div className="alert alert-success" role="alert">{success}</div>}

                {/* Użyj onSubmit na formularzu */}
                <form onSubmit={handleSubmit} noValidate>
                    {/* Pole Nazwa */}
                    <div className="mb-3">
                        <label htmlFor="itemName" className="form-label">Nazwa przedmiotu <span className="text-danger">*</span></label>
                        <input type="text" className="form-control" id="itemName" value={name} onChange={e => setName(e.target.value)} required maxLength={100} disabled={submitting} />
                    </div>

                    {/* Pole Opis */}
                    <div className="mb-3">
                        <label htmlFor="itemDescription" className="form-label">Opis <span className="text-danger">*</span></label>
                        <textarea className="form-control" id="itemDescription" rows="4" value={description} onChange={e => setDescription(e.target.value)} required maxLength={1000} disabled={submitting}></textarea>
                    </div>

                    {/* Wiersz Kategoria i Stan */}
                    <div className="row g-3">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCategory" className="form-label">Kategoria <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCategory" value={categoryId} onChange={e => setCategoryId(e.target.value)} required disabled={loadingCategories || submitting}>
                                <option value="" disabled>{loadingCategories ? "Ładowanie..." : "-- Wybierz kategorię --"}</option>
                                {categories.map(cat => (
                                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                                ))}
                            </select>
                            {/* Mały spinner przy ładowaniu kategorii */}
                            {loadingCategories && <span className="spinner-border spinner-border-sm text-secondary ms-2 align-middle" role="status" aria-hidden="true"></span>}
                        </div>
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCondition" className="form-label">Stan <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCondition" value={condition} onChange={e => setCondition(e.target.value)} required disabled={submitting}>
                                {/* Wartości muszą pasować do backendu */}
                                <option value="New">Nowy</option>
                                <option value="LikeNew">Jak nowy</option>
                                <option value="Used">Używany</option>
                                <option value="Damaged">Uszkodzony</option>
                            </select>
                        </div>
                    </div>

                    {/* Wiersz Cena i Wymiana */}
                    <div className="row g-3 align-items-center">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemPrice" className="form-label">Oczekiwana wartość / Cena (PLN)</label>
                            <input
                                type="number" step="0.01" min="0" className="form-control" id="itemPrice"
                                value={price} onChange={e => setPrice(e.target.value)}
                                placeholder="np. 50.00 (lub puste)" disabled={isForExchange || submitting} />
                        </div>
                        <div className="col-md-6 mb-3 align-self-end pb-1"> {/* Drobna korekta wyrównania */}
                            <div className="form-check">
                                <input className="form-check-input" type="checkbox" id="isForExchange"
                                       checked={isForExchange}
                                       onChange={e => { setIsForExchange(e.target.checked); if (e.target.checked) { setPrice(''); } }}
                                       disabled={submitting}/>
                                <label className="form-check-label" htmlFor="isForExchange">Tylko na wymianę?</label>
                            </div>
                        </div>
                    </div>
                    {/* Notatka pod ceną */}
                    <small className="form-text text-muted d-block mb-3">
                        Podaj cenę lub zostaw puste (dla "za darmo"). Zaznacz 'Tylko na wymianę', jeśli nie dotyczy sprzedaży.
                    </small>

                    {/* Pole Zdjęcia */}
                    <div className="mb-3">
                        <label htmlFor="itemPhotos" className="form-label">Zdjęcia <span className="text-danger">*</span> <small className="text-muted">(min. 1, max. 5, do 5MB każde)</small></label>
                        <input className="form-control" type="file" id="itemPhotos" multiple
                               accept="image/jpeg, image/png, image/webp"
                               onChange={handlePhotoChange} required disabled={submitting} />
                        {/* Podgląd wybranych zdjęć */}
                        {photos.length > 0 && (
                            <div className="mt-2 d-flex flex-wrap gap-2 border p-2 rounded bg-light">
                                {photos.map((file, index) => (
                                    <div key={index} className="text-center bg-white p-1 border rounded shadow-sm position-relative">
                                        <img src={URL.createObjectURL(file)} alt={`Podgląd ${index + 1}`} style={{ height: '70px', width: '70px', objectFit: 'cover', display: 'block', marginBottom: '0.25rem' }} />
                                        <small className="text-muted d-block" style={{fontSize: '0.7rem', maxWidth: '70px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap'}} title={file.name}>{file.name}</small>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>

                    {/* Przycisk Submit */}
                    <div className="d-grid mt-4">
                        <button type="submit" className="btn btn-lg btn-success" disabled={submitting || loadingCategories}>
                            {submitting ? (
                                <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Trwa dodawanie...</>
                            ) : (
                                <><i className="bi bi-plus-circle me-1"></i> Dodaj przedmiot</>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

// --- Renderowanie Komponentu ---
const container = document.getElementById('react-item-create-form-container');
if (container) {
    const categoriesData = container.getAttribute('data-categories');
    let initialCategories = [];
    try {
        // Bardziej odporne parsowanie
        if (categoriesData && typeof categoriesData === 'string' && categoriesData.trim() && categoriesData.trim().toLowerCase() !== 'null') {
            initialCategories = JSON.parse(categoriesData);
            if (!Array.isArray(initialCategories)) {
                console.warn("ItemCreateForm: Parsed categories data is not an array, defaulting to empty.", initialCategories);
                initialCategories = [];
            }
        }
    } catch (e) {
        console.error("ItemCreateForm: Error parsing categories data attribute:", e, "Data received:", categoriesData);
        initialCategories = []; // Domyślnie pusta lista w razie błędu
    }

    const root = ReactDOM.createRoot(container);
    root.render(
        <StrictMode>
            <ItemCreateForm initialCategories={initialCategories} />
        </StrictMode>
    );
    console.log(`ItemCreateForm initialized. Initial categories count: ${initialCategories.length}`);

} else {
    console.warn("Container element '#react-item-create-form-container' not found on this page.");
}