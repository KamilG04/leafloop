// Pełna ścieżka: wwwroot/js/components/itemCreateForm.js (PEŁNY KOD)

// ----- POCZĄTEK PLIKU -----
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client'; // Poprawny import dla React 18+
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js'; // Zaimportuj funkcje pomocnicze

const ItemCreateForm = ({ initialCategories = [] }) => { // Akceptuje początkowe kategorie z Razor
    // Stany dla pól formularza
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState(''); // Cena jako string
    const [categoryId, setCategoryId] = useState('');
    const [condition, setCondition] = useState('Used'); // Domyślna wartość, np. "Używany"
    const [isForExchange, setIsForExchange] = useState(false);
    // const [tags, setTags] = useState(''); // Tagi - implementacja dodawania tagów wymagałaby osobnego API lub modyfikacji ItemCreateDto
    const [photos, setPhotos] = useState([]); // Dla plików zdjęć (FileList)

    // Stany dla ładowania, błędów, sukcesu
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);

    // Stan dla listy kategorii
    const [categories, setCategoriesState] = useState(initialCategories); // Użyj przekazanych lub pustej
    const [loadingCategories, setLoadingCategories] = useState(initialCategories.length === 0); // Ładuj tylko jeśli nie przekazano

    // Pobieranie kategorii przy montowaniu, jeśli nie zostały przekazane z Razor
    useEffect(() => {
        // Jeśli kategorie zostały przekazane przez atrybut data-categories, nie pobieraj ich ponownie
        if (initialCategories.length > 0) {
            setLoadingCategories(false);
            return;
        }
        // Jeśli nie przekazano, pobierz z API
        const fetchCategories = async () => {
            console.log("Pobieranie kategorii z API dla formularza...");
            setLoadingCategories(true);
            try {
                // Upewnij się, że endpoint /api/categories istnieje i działa
                const response = await fetch('/api/categories', { headers: getAuthHeaders(false) });
                const data = await handleApiResponse(response); // Użyj globalnej funkcji obsługi
                setCategoriesState(data || []); // Ustaw kategorie lub pustą tablicę
                console.log("Kategorie dla formularza załadowane:", data);
            } catch (err) {
                console.error("Błąd pobierania kategorii dla formularza:", err);
                // Ustaw błąd globalny LUB po prostu zostaw pustą listę i zaloguj błąd
                setError("Nie udało się załadować listy kategorii. Spróbuj odświeżyć stronę.");
            } finally {
                setLoadingCategories(false);
            }
        };
        fetchCategories();
    }, [initialCategories]); // Uruchom tylko raz lub gdy zmienią się initialCategories

    // Obsługa zmiany plików zdjęć
    const handlePhotoChange = (event) => {
        const files = Array.from(event.target.files); // Konwertuj FileList na tablicę
        // Prosta walidacja ilości (np. max 5)
        if (files.length > 5) {
            setError("Można dodać maksymalnie 5 zdjęć.");
            setPhotos([]); // Wyczyść wybór, jeśli za dużo plików
            event.target.value = null; // Zresetuj input file, aby umożliwić ponowny wybór tych samych plików
        } else {
            setError(null); // Wyczyść poprzedni błąd
            setPhotos(files); // Zapisz wybrane pliki
        }
    };

    // Obsługa wysłania formularza
    const handleSubmit = async (event) => {
        event.preventDefault(); // Zapobiegaj domyślnemu wysłaniu formularza HTML
        setLoading(true); // Pokaż wskaźnik ładowania
        setError(null);   // Wyczyść poprzednie błędy
        setSuccess(null); // Wyczyść poprzedni komunikat sukcesu

        // --- Walidacja podstawowa po stronie klienta ---
        if (!name || !description || !categoryId || !condition) {
            setError("Pola: Nazwa, Opis, Kategoria i Stan są wymagane.");
            setLoading(false);
            return;
        }
        const parsedCategoryId = parseInt(categoryId, 10);
        if (isNaN(parsedCategoryId)) {
            setError("Wybierz poprawną kategorię.");
            setLoading(false);
            return;
        }
        const parsedPrice = price ? parseFloat(price) : null;
        if (price && isNaN(parsedPrice)) { // Sprawdź czy cena jest liczbą, jeśli została podana
            setError("Cena musi być poprawną liczbą.");
            setLoading(false);
            return;
        }
        if (parsedPrice !== null && parsedPrice < 0) { // Cena nie może być ujemna
            setError("Cena nie może być ujemna.");
            setLoading(false);
            return;
        }
        if (photos.length === 0) { // Wymagaj co najmniej jednego zdjęcia
            setError("Dodaj przynajmniej jedno zdjęcie.");
            setLoading(false);
            return;
        }
        // --- Koniec Walidacji ---


        // Przygotuj dane dla DTO ItemCreateDto (zgodnie z definicją C#)
        const itemData = {
            name: name,
            description: description,
            // W DTO jest ExpectedValue, które mapujemy na cenę
            expectedValue: parsedPrice !== null ? parsedPrice : 0, // Jeśli cena jest null/pusta, wartość to 0
            isForExchange: isForExchange, // Wartość z checkboxa
            categoryId: parsedCategoryId,
            condition: condition, // Wartość z selecta stanu
            tagIds: [] // Twoje DTO ItemCreateDto nie miało pola TagIds. Jeśli chcesz obsługiwać tagi, musisz zmodyfikować DTO i API lub dodać tagi osobnym zapytaniem.
        };

        console.log("Wysyłanie danych przedmiotu:", itemData);

        try {
            // KROK 1: Wyślij dane tekstowe przedmiotu do API, aby uzyskać ID
            const responseItem = await fetch('/api/items', { // Endpoint tworzenia przedmiotu
                method: 'POST',
                headers: getAuthHeaders(true), // Wymaga tokena JWT i Content-Type: application/json
                body: JSON.stringify(itemData)
            });
            // Użyj funkcji pomocniczej do obsługi odpowiedzi (sprawdza błędy, parsuje JSON lub ID z Location)
            const createdItemResponse = await handleApiResponse(responseItem);

            // ----- POPRAWIONE SPRAWDZANIE ODPOWIEDZI Z ID -----
            let newItemId = null;
            // Sprawdź, czy odpowiedź to poprawna liczba (ID zwrócone w ciele)
            if (typeof createdItemResponse === 'number' && createdItemResponse > 0) {
                newItemId = createdItemResponse;
            }
            // Sprawdź, czy odpowiedź to obiekt z poprawnym polem 'id' (np. sparsowane z nagłówka Location)
            else if (createdItemResponse && typeof createdItemResponse === 'object' && createdItemResponse.id && typeof createdItemResponse.id === 'number' && createdItemResponse.id > 0) {
                newItemId = createdItemResponse.id;
            }

            // Jeśli nie udało się uzyskać poprawnego ID, rzuć błąd
            if (newItemId === null) {
                console.error("Odpowiedź API po utworzeniu przedmiotu (nie udało się uzyskać ID):", createdItemResponse);
                throw new Error("Nie udało się utworzyć przedmiotu lub API nie zwróciło oczekiwanego ID.");
            }
            // ----- KONIEC POPRAWIONEGO SPRAWDZANIA -----

            console.log("Przedmiot utworzony, ID:", newItemId);


            // KROK 2: Jeśli są zdjęcia, wyślij je do odpowiedniego endpointu
            if (photos.length > 0) {
                console.log(`Wysyłanie ${photos.length} zdjęć...`);
                // Używamy pętli for...of z await, aby wysyłać zdjęcia sekwencyjnie (bezpieczniejsze niż Promise.all dla uploadu)
                for (const photoFile of photos) {
                    const formData = new FormData(); // Użyj FormData do wysłania pliku
                    formData.append('photo', photoFile); // Klucz 'photo' musi pasować do parametru IFormFile w API

                    const photoUploadUrl = `/api/items/${newItemId}/photos`; // Endpoint do uploadu zdjęć dla konkretnego itemu
                    console.log(`Wysyłanie zdjęcia: ${photoFile.name} do ${photoUploadUrl}`);

                    const responsePhoto = await fetch(photoUploadUrl, {
                        method: 'POST',
                        headers: {
                            // Dla FormData NIE ustawiamy 'Content-Type', przeglądarka zrobi to sama (z odpowiednim boundary)
                            // Wymagany jest tylko nagłówek autoryzacji
                            'Authorization': getAuthHeaders(false)['Authorization'] // Pobierz tylko token
                        },
                        body: formData
                    });
                    // Sprawdź odpowiedź dla każdego zdjęcia (oczekujemy 200, 201 lub 204)
                    await handleApiResponse(responsePhoto);
                    console.log(`Zdjęcie ${photoFile.name} wysłane.`);
                }
                console.log("Wszystkie zdjęcia wysłane.");
            }

            // KROK 3 (Opcjonalnie): Dodawanie tagów (jeśli zaimplementujesz)
            // const tagNames = tags.split(',').map(tag => tag.trim()).filter(tag => tag.length > 0);
            // if (tagNames.length > 0) { /* ... wywołaj API do dodania tagów ... */ }

            // Sukces! Poinformuj użytkownika i wyczyść formularz
            setSuccess(`Przedmiot "${name}" został pomyślnie dodany! Zostaniesz przekierowany na jego stronę...`);
            setName('');
            setDescription('');
            setPrice('');
            setCategoryId('');
            setCondition('Used');
            setIsForExchange(false);
            // setTags(''); // Jeśli używasz pola tekstowego dla tagów
            setPhotos([]); // Wyczyść listę plików

            // Zresetuj wartość inputa typu file, aby można było dodać te same pliki ponownie
            const photoInput = document.getElementById('itemPhotos');
            if (photoInput) {
                photoInput.value = null;
            }

            // Opcjonalne przekierowanie po krótkim czasie, aby użytkownik zobaczył komunikat sukcesu
            setTimeout(() => {
                window.location.href = `/Items/Details/${newItemId}`; // Przekieruj na stronę szczegółów nowego przedmiotu
            }, 2000); // Poczekaj 2 sekundy

        } catch (err) {
            // Obsługa błędów z API lub z logiki JS
            console.error("Błąd podczas dodawania przedmiotu:", err);
            setError(err.message || "Wystąpił nieoczekiwany błąd podczas dodawania przedmiotu.");
            // Nie czyść formularza przy błędzie, aby użytkownik mógł poprawić dane
        } finally {
            // Zawsze wyłącz wskaźnik ładowania na końcu
            setLoading(false);
        }
    };

    // --- Renderowanie JSX Formularza ---
    // (Ta część jest taka sama jak w poprzedniej wersji - zawiera pola input, select, textarea, button itp.)
    return (
        <div className="card shadow-sm">
            <div className="card-body">
                <form onSubmit={handleSubmit}>
                    {/* Wyświetlanie błędów i sukcesu */}
                    {error && <div className="alert alert-danger" role="alert" dangerouslySetInnerHTML={{ __html: error }}></div>}
                    {success && <div className="alert alert-success" role="alert">{success}</div>}

                    {/* Pole Nazwa */}
                    <div className="mb-3">
                        <label htmlFor="itemName" className="form-label">Nazwa przedmiotu <span className="text-danger">*</span></label>
                        <input type="text" className="form-control" id="itemName" value={name} onChange={e => setName(e.target.value)} required maxLength={100}/>
                    </div>

                    {/* Pole Opis */}
                    <div className="mb-3">
                        <label htmlFor="itemDescription" className="form-label">Opis <span className="text-danger">*</span></label>
                        <textarea className="form-control" id="itemDescription" rows="4" value={description} onChange={e => setDescription(e.target.value)} required maxLength={1000}></textarea>
                    </div>

                    <div className="row g-3">
                        {/* Pole Kategoria */}
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCategory" className="form-label">Kategoria <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCategory" value={categoryId} onChange={e => setCategoryId(e.target.value)} required disabled={loadingCategories}>
                                <option value="" disabled>{loadingCategories ? "Ładowanie..." : "-- Wybierz --"}</option>
                                {/* Zakładamy, że CategoryDto ma pola 'id' i 'name' */}
                                {categories.map(cat => (
                                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                                ))}
                            </select>
                        </div>
                        {/* Pole Stan */}
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCondition" className="form-label">Stan <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCondition" value={condition} onChange={e => setCondition(e.target.value)} required>
                                {/* Upewnij się, że wartości 'value' pasują do tego, czego oczekuje backend (np. enum jako string) */}
                                <option value="New">Nowy</option>
                                <option value="LikeNew">Jak nowy</option>
                                <option value="Used">Używany</option>
                                <option value="Damaged">Uszkodzony</option>
                            </select>
                        </div>
                    </div>

                    {/* Pola Cena i Wymiana */}
                    <div className="row g-3 align-items-center">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemPrice" className="form-label">Oczekiwana wartość / Cena (PLN)</label>
                            <input
                                type="number" step="0.01" min="0" className="form-control" id="itemPrice"
                                value={price} onChange={e => setPrice(e.target.value)}
                                placeholder="np. 50.00" disabled={isForExchange} />
                        </div>
                        <div className="col-md-6 mb-3 align-self-end">
                            <div className="form-check">
                                <input className="form-check-input" type="checkbox" id="isForExchange"
                                       checked={isForExchange}
                                       onChange={e => { setIsForExchange(e.target.checked); if (e.target.checked) { setPrice(''); } }}/>
                                <label className="form-check-label" htmlFor="isForExchange">Tylko na wymianę?</label>
                            </div>
                        </div>
                    </div>
                    <small className="form-text text-muted d-block mb-3">
                        Podaj cenę sprzedaży lub zostaw puste dla wymiany/za darmo. Zaznaczenie "Tylko na wymianę" wyłączy pole ceny. Wartość oczekiwana (ExpectedValue) zostanie ustawiona na podstawie ceny lub na 0.
                    </small>

                    {/* Pole Zdjęcia */}
                    <div className="mb-3">
                        <label htmlFor="itemPhotos" className="form-label">Zdjęcia <span className="text-danger">*</span> <small className="text-muted">(min. 1, max. 5)</small></label>
                        <input className="form-control" type="file" id="itemPhotos" multiple
                               accept="image/jpeg, image/png, image/webp" // Akceptowane formaty
                               onChange={handlePhotoChange} required />
                        {/* Podgląd miniaturek */}
                        {photos.length > 0 && (
                            <div className="mt-2 d-flex flex-wrap gap-2 border p-2 rounded" style={{backgroundColor: '#f8f9fa'}}>
                                {photos.map((file, index) => (
                                    <div key={index} className="text-center">
                                        <img src={URL.createObjectURL(file)} alt={`Podgląd ${index + 1}`} style={{ height: '60px', width: 'auto', border: '1px solid #ddd', display: 'block' }} />
                                        <small className="text-muted d-block" style={{fontSize: '0.7rem', maxWidth: '80px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap'}}>{file.name}</small>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>

                    {/* Przycisk Submit */}
                    <div className="d-grid">
                        <button type="submit" className="btn btn-lg btn-success" disabled={loading}>
                            {loading ? ( <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Trwa dodawanie...</> )
                                : ( <><i className="bi bi-check-circle me-1"></i> Dodaj przedmiot </> )}
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
        // Użyj pustej tablicy jako domyślnej, jeśli categoriesData jest puste, null, lub nie jest poprawnym JSON
        initialCategories = (categoriesData && categoriesData.trim() !== '' && categoriesData.trim() !== 'null') ? JSON.parse(categoriesData) : [];
    } catch (e) {
        console.error("Błąd parsowania danych kategorii z atrybutu (używam pustej listy):", e);
        // initialCategories pozostaje []
    }
    const root = ReactDOM.createRoot(container);
    // Przekazujemy initialCategories jako prop do komponentu
    root.render(<StrictMode><ItemCreateForm initialCategories={initialCategories} /></StrictMode>);
} else {
    console.error("Nie znaleziono kontenera 'react-item-create-form-container' do renderowania formularza.");
}

