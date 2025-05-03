import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Import ApiService

const ItemEditForm = ({ itemId, initialCategories = [] }) => {
    // State variables (keep most)
    // Removed 'item' state as we mainly need individual fields after load
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState(''); // Keep as string for input control
    const [categoryId, setCategoryId] = useState(''); // Keep as string for select control
    const [condition, setCondition] = useState('');
    const [isAvailable, setIsAvailable] = useState(true);
    const [isForExchange, setIsForExchange] = useState(false);
    const [categories, setCategories] = useState(initialCategories);
    const [photos, setPhotos] = useState([]); // Keep for display

    // UI States
    const [loading, setLoading] = useState(true); // Loading initial item data
    const [submitting, setSubmitting] = useState(false); // For submit button state
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);
    const [loadingCategories, setLoadingCategories] = useState(initialCategories.length === 0);

    // Fetch categories function (using ApiService)
    const fetchCategories = useCallback(async () => {
        // No need to setLoadingCategories(true) if already loading initial data
        setError(null); // Clear potential previous errors
        try {
            console.log("Fetching categories via API...");
            const data = await ApiService.get('/api/categories');
            setCategories(data || []);
        } catch (err) {
            console.error("Error loading categories:", err);
            // Set error, possibly append to existing item load error
            setError(prev => prev ? `${prev} | Categories Error: ${err.message}` : `Categories Error: ${err.message}`);
            setCategories([]); // Set empty array on error
        } finally {
            setLoadingCategories(false);
        }
    }, []); // No dependencies needed

    // Fetch initial item data (using ApiService)
    const fetchItemData = useCallback(async () => {
        if (!itemId || itemId <= 0) {
            setError('Invalid item ID.');
            setLoading(false);
            setLoadingCategories(false); // Ensure category loading stops
            return;
        }
        setLoading(true);
        setLoadingCategories(initialCategories.length === 0); // Set category loading state
        setError(null);
        setSuccess(null);

        try {
            console.log(`Workspaceing item data for ID: ${itemId}`);
            const data = await ApiService.get(`/api/items/${itemId}`);

            if (!data) {
                throw new Error("Item not found or empty response.");
            }

            // Set form fields from fetched data
            setName(data.name || '');
            setDescription(data.description || '');
            // Ensure price is handled correctly (null/0 becomes empty string)
            setPrice(data.expectedValue != null && data.expectedValue > 0 ? data.expectedValue.toString() : '');
            // Ensure categoryId is string for select control
            setCategoryId(data.categoryId != null ? data.categoryId.toString() : '');
            setCondition(data.condition || '');
            // Use explicit boolean conversion
            setIsAvailable(data.isAvailable === true);
            setIsForExchange(data.isForExchange === true);
            setPhotos(data.photos || []);

            // Fetch categories only if not provided initially
            if (initialCategories.length === 0) {
                await fetchCategories(); // Await category fetch completion
            }

        } catch (err) {
            console.error("Error loading item data:", err);
            setError(`Error loading item data: ${err.message}`);
        } finally {
            setLoading(false);
            // Ensure category loading is false even if categories weren't fetched
            if (initialCategories.length > 0) {
                setLoadingCategories(false);
            }
        }
    }, [itemId, initialCategories, fetchCategories]); // Add fetchCategories dependency

    useEffect(() => {
        fetchItemData();
    }, [fetchItemData]); // Run fetchItemData on mount/itemId change

    // Handle form submission (using ApiService)
    const handleSubmit = useCallback(async (event) => {
        event.preventDefault();
        // Basic client-side validation
        if (!name.trim() || !description.trim() || !categoryId || !condition) {
            setError("Name, description, category, and condition are required.");
            // Scroll to top to show error? Maybe not needed if error div is visible
            window.scrollTo(0, 0);
            return;
        }

        setSubmitting(true); // Use separate state for submission
        setError(null);
        setSuccess(null);

        // Prepare data for API
        const parsedCategoryId = parseInt(categoryId, 10);
        // Price: 0 if empty or if 'isForExchange' is true
        const parsedPrice = isForExchange || !price.trim() ? 0 : parseFloat(price);

        if (isNaN(parsedCategoryId) || parsedCategoryId <= 0) {
            setError("Invalid Category selected.");
            setSubmitting(false);
            return;
        }
        if (isNaN(parsedPrice) || parsedPrice < 0) {
            setError("Invalid Price/Value entered.");
            setSubmitting(false);
            return;
        }

        const itemData = {
            id: itemId, // Crucial for PUT
            name: name.trim(),
            description: description.trim(),
            expectedValue: parsedPrice,
            isForExchange,
            isAvailable,
            categoryId: parsedCategoryId,
            condition
        };

        console.log("Submitting item update:", itemData);

        try {
            // Use ApiService.put
            // PUT usually returns 204 No Content on success, ApiService handles this returning null
            await ApiService.put(`/api/items/${itemId}`, itemData);

            setSuccess("Item updated successfully! Redirecting...");

            // Redirect after delay
            setTimeout(() => {
                window.location.href = `/Items/Details/${itemId}`;
            }, 1500);

        } catch (err) {
            console.error("Error updating item:", err);
            setError(`Update Error: ${err.message}`);
            setSubmitting(false); // Stop submitting state on error
        }
        // No finally here, keep submitting true until redirect or error clear
    }, [itemId, name, description, price, categoryId, condition, isAvailable, isForExchange]); // All form field dependencies

    // --- RENDER ---

    // Initial loading state
    if (loading) {
        return (
            <div className="d-flex justify-content-center my-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie danych formularza...</span>
                </div>
            </div>
        );
    }

    // Error state during initial load
    if (error && !submitting && !success) { // Show load error only if not currently submitting/successful
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center">
                <span>{error}</span>
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchItemData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie załadować
                </button>
            </div>
        );
    }


    // Form rendering
    return (
        <div className="card shadow-sm">
            <div className="card-header">
                <h4>Edit Item: {name || `(ID: ${itemId})`}</h4>
            </div>
            <div className="card-body">
                <form onSubmit={handleSubmit} noValidate>
                    {/* Display Success/Error Messages */}
                    {success && <div className="alert alert-success">{success}</div>}
                    {error && !success && <div className="alert alert-danger">{error}</div>} {/* Show submit error if no success message */}


                    {/* Form Fields (keep structure, ensure state bindings are correct) */}
                    <div className="mb-3">
                        <label htmlFor="itemName" className="form-label">Name <span className="text-danger">*</span></label>
                        <input type="text" className="form-control" id="itemName" value={name} onChange={e => setName(e.target.value)} required disabled={submitting}/>
                    </div>

                    <div className="mb-3">
                        <label htmlFor="itemDescription" className="form-label">Description <span className="text-danger">*</span></label>
                        <textarea className="form-control" id="itemDescription" rows="4" value={description} onChange={e => setDescription(e.target.value)} required disabled={submitting}></textarea>
                    </div>

                    <div className="row g-3">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCategory" className="form-label">Category <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCategory" value={categoryId} onChange={e => setCategoryId(e.target.value)} required disabled={loadingCategories || submitting}>
                                <option value="" disabled>{loadingCategories ? "Ładowanie..." : "-- Wybierz --"}</option>
                                {categories.map(cat => (
                                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                                ))}
                            </select>
                            {loadingCategories && <div className="spinner-border spinner-border-sm text-secondary ms-2" role="status"><span className="visually-hidden">Ładowanie kategorii...</span></div>}
                        </div>

                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCondition" className="form-label">Condition <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCondition" value={condition} onChange={e => setCondition(e.target.value)} required disabled={submitting}>
                                <option value="" disabled>-- Wybierz --</option>
                                <option value="New">New</option>
                                <option value="LikeNew">Like New</option>
                                <option value="Used">Used</option>
                                <option value="Damaged">Damaged</option>
                            </select>
                        </div>
                    </div>

                    <div className="row g-3 align-items-center">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemPrice" className="form-label">Expected Value / Price (PLN)</label>
                            {/* Disable price if 'For Exchange' is checked */}
                            <input type="number" step="0.01" min="0" className="form-control" id="itemPrice" value={price} onChange={e => setPrice(e.target.value)} placeholder="0.00 lub zostaw puste dla 'Za darmo'" disabled={isForExchange || submitting} />
                        </div>

                        <div className="col-md-6 mb-3">
                            {/* Disable checkboxes during submission */}
                            <div className="form-check mt-md-4"> {/* Adjust margin for alignment */}
                                <input className="form-check-input" type="checkbox" id="isForExchange" checked={isForExchange} onChange={e => {
                                    const isChecked = e.target.checked;
                                    setIsForExchange(isChecked);
                                    // Clear price when checking 'For Exchange'
                                    if (isChecked) setPrice('');
                                }} disabled={submitting}/>
                                <label className="form-check-label" htmlFor="isForExchange">Tylko na wymianę?</label>
                            </div>

                            <div className="form-check">
                                <input className="form-check-input" type="checkbox" id="isAvailable" checked={isAvailable} onChange={e => setIsAvailable(e.target.checked)} disabled={submitting}/>
                                <label className="form-check-label" htmlFor="isAvailable">Dostępny (widoczny w wyszukiwarce)</label>
                            </div>
                        </div>
                    </div>

                    {/* Display existing photos (keep as is) */}
                    {photos.length > 0 && (
                        <div className="mb-3">
                            <label className="form-label d-block">Aktualne zdjęcia</label>
                            <div className="d-flex flex-wrap gap-2 p-2 bg-light rounded border">
                                {photos.map(photo => (
                                    <div key={photo.id} className="position-relative text-center">
                                        <img
                                            src={ApiService.getImageUrl(photo.path)} // Use helper
                                            alt={photo.fileName || `Zdjęcie ${photo.id}`}
                                            style={{ height: '80px', width: '80px', objectFit: 'cover', borderRadius: '4px' }}
                                            onError={(e) => { e.target.src = ApiService.getImageUrl(null); }} // Fallback
                                        />
                                        {/* <small className="d-block text-muted text-truncate" style={{maxWidth: '80px'}}>{photo.fileName}</small> */}
                                    </div>
                                ))}
                            </div>
                            <small className="form-text text-muted mt-1 d-block">Zarządzanie zdjęciami (dodawanie/usuwanie) jest dostępne na stronie szczegółów przedmiotu.</small>
                        </div>
                    )}

                    <div className="d-flex justify-content-end mt-4 gap-2">
                        {/* Cancel button */}
                        <a href={`/Items/Details/${itemId}`} className="btn btn-secondary" disabled={submitting}>
                            <i className="bi bi-x-circle me-1"></i> Anuluj
                        </a>
                        {/* Submit button */}
                        <button type="submit" className="btn btn-success" disabled={submitting || loadingCategories}>
                            {submitting ? (
                                <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Zapisywanie...</>
                            ) : (
                                <><i className="bi bi-check-circle me-1"></i> Zapisz zmiany</>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};


// Initialize component (keep similar logic)
const container = document.getElementById('react-item-edit-form-container');
if (container) {
    const itemIdString = container.getAttribute('data-item-id');
    const itemId = parseInt(itemIdString, 10);
    const categoriesData = container.getAttribute('data-categories'); // Get preloaded categories if available
    let initialCategories = [];

    // Validate itemId
    if (isNaN(itemId) || itemId <= 0) {
        console.error("ItemEditForm: Invalid or missing item ID in data-item-id attribute:", itemIdString);
        container.innerHTML = '<div class="alert alert-danger">Błąd krytyczny: Nie można załadować formularza. Brak poprawnego ID przedmiotu.</div>';
    } else {
        // Try parsing categories only if itemId is valid
        try {
            // Check if categoriesData is a non-empty string before parsing
            if (categoriesData && typeof categoriesData === 'string' && categoriesData.trim() && categoriesData.trim().toLowerCase() !== 'null') {
                initialCategories = JSON.parse(categoriesData);
                if (!Array.isArray(initialCategories)) {
                    console.warn("ItemEditForm: Parsed categories data is not an array, defaulting to empty.", initialCategories);
                    initialCategories = [];
                }
            }
        } catch (e) {
            console.error("ItemEditForm: Error parsing categories data attribute:", e, "Data received:", categoriesData);
            // Don't block rendering, component will try to fetch categories via API
            initialCategories = [];
        }

        // Render the component
        const root = ReactDOM.createRoot(container);
        root.render(
            <StrictMode>
                <ItemEditForm itemId={itemId} initialCategories={initialCategories} />
            </StrictMode>
        );
        console.log(`ItemEditForm initialized for ItemID: ${itemId}. Initial categories count: ${initialCategories.length}`);
    }
} else {
    console.warn("Container element '#react-item-edit-form-container' not found on this page.");
}