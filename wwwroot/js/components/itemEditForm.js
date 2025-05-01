// Path: wwwroot/js/components/itemEditForm.js
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js';

const ItemEditForm = ({ itemId, initialCategories = [] }) => {
    // State variables
    const [item, setItem] = useState(null);
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [condition, setCondition] = useState('');
    const [isAvailable, setIsAvailable] = useState(true);
    const [isForExchange, setIsForExchange] = useState(false);
    const [categories, setCategories] = useState(initialCategories);
    const [photos, setPhotos] = useState([]);

    // Loading, error, success states
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);
    const [loadingCategories, setLoadingCategories] = useState(initialCategories.length === 0);

    // Fetch item data when component mounts
    useEffect(() => {
        const fetchItemData = async () => {
            if (!itemId || itemId <= 0) {
                setError('Invalid item ID.');
                setLoading(false);
                return;
            }

            try {
                // Fetch item
                const response = await fetch(`/api/items/${itemId}`, {
                    headers: getAuthHeaders(false)
                });
                const data = await handleApiResponse(response);

                // Set item and form fields
                setItem(data);
                setName(data.name || '');
                setDescription(data.description || '');
                setPrice(data.expectedValue ? data.expectedValue.toString() : '');
                setCategoryId(data.categoryId ? data.categoryId.toString() : '');
                setCondition(data.condition || '');
                setIsAvailable(!!data.isAvailable);
                setIsForExchange(!!data.isForExchange);
                setPhotos(data.photos || []);

                // Fetch categories if needed
                if (initialCategories.length === 0) {
                    fetchCategories();
                }
            } catch (err) {
                console.error("Error loading item data:", err);
                setError(`Error loading item data: ${err.message}`);
            } finally {
                setLoading(false);
            }
        };

        const fetchCategories = async () => {
            setLoadingCategories(true);
            try {
                const response = await fetch('/api/categories', {
                    headers: getAuthHeaders(false)
                });
                const data = await handleApiResponse(response);
                setCategories(data || []);
            } catch (err) {
                console.error("Error loading categories:", err);
                setError((prevError) =>
                    prevError ? `${prevError}. Also, could not load categories.` : "Could not load categories."
                );
            } finally {
                setLoadingCategories(false);
            }
        };

        fetchItemData();
    }, [itemId, initialCategories]);

    // Handle form submission
    const handleSubmit = async (event) => {
        event.preventDefault();
        setLoading(true);
        setError(null);
        setSuccess(null);

        // Validation
        if (!name || !description || !categoryId || !condition) {
            setError("Name, description, category, and condition are required.");
            setLoading(false);
            return;
        }

        // Parse values
        const parsedCategoryId = parseInt(categoryId, 10);
        const parsedPrice = price ? parseFloat(price) : 0;

        // Create update data
        const itemData = {
            id: itemId, // Important for PUT request
            name,
            description,
            expectedValue: parsedPrice,
            isForExchange,
            isAvailable,
            categoryId: parsedCategoryId,
            condition
        };

        try {
            // Update item data
            const response = await fetch(`/api/items/${itemId}`, {
                method: 'PUT',
                headers: getAuthHeaders(true),
                body: JSON.stringify(itemData)
            });

            await handleApiResponse(response);

            // Display success message
            setSuccess("Item updated successfully!");

            // Optional: Redirect after short delay
            setTimeout(() => {
                window.location.href = `/Items/Details/${itemId}`;
            }, 1500);

        } catch (err) {
            console.error("Error updating item:", err);
            setError(`Error updating item: ${err.message}`);
        } finally {
            setLoading(false);
        }
    };

    // Render loading state
    if (loading) {
        return (
            <div className="d-flex justify-content-center my-4">
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return <div className="alert alert-danger">{error}</div>;
    }

    // Render form
    return (
        <div className="card shadow-sm">
            <div className="card-body">
                <form onSubmit={handleSubmit}>
                    {success && <div className="alert alert-success">{success}</div>}

                    <div className="mb-3">
                        <label htmlFor="itemName" className="form-label">Name <span className="text-danger">*</span></label>
                        <input type="text" className="form-control" id="itemName" value={name} onChange={e => setName(e.target.value)} required />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="itemDescription" className="form-label">Description <span className="text-danger">*</span></label>
                        <textarea className="form-control" id="itemDescription" rows="4" value={description} onChange={e => setDescription(e.target.value)} required></textarea>
                    </div>

                    <div className="row g-3">
                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCategory" className="form-label">Category <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCategory" value={categoryId} onChange={e => setCategoryId(e.target.value)} required disabled={loadingCategories}>
                                <option value="" disabled>{loadingCategories ? "Loading..." : "-- Select --"}</option>
                                {categories.map(cat => (
                                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                                ))}
                            </select>
                        </div>

                        <div className="col-md-6 mb-3">
                            <label htmlFor="itemCondition" className="form-label">Condition <span className="text-danger">*</span></label>
                            <select className="form-select" id="itemCondition" value={condition} onChange={e => setCondition(e.target.value)} required>
                                <option value="" disabled>-- Select --</option>
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
                            <input type="number" step="0.01" min="0" className="form-control" id="itemPrice" value={price} onChange={e => setPrice(e.target.value)} placeholder="e.g., 50.00" disabled={isForExchange} />
                        </div>

                        <div className="col-md-6 mb-3">
                            <div className="form-check mt-4">
                                <input className="form-check-input" type="checkbox" id="isForExchange" checked={isForExchange} onChange={e => {
                                    setIsForExchange(e.target.checked);
                                    if (e.target.checked) setPrice('');
                                }} />
                                <label className="form-check-label" htmlFor="isForExchange">For exchange only?</label>
                            </div>

                            <div className="form-check">
                                <input className="form-check-input" type="checkbox" id="isAvailable" checked={isAvailable} onChange={e => setIsAvailable(e.target.checked)} />
                                <label className="form-check-label" htmlFor="isAvailable">Available</label>
                            </div>
                        </div>
                    </div>

                    {/* Display existing photos */}
                    {photos.length > 0 && (
                        <div className="mb-3">
                            <label className="form-label">Current Photos</label>
                            <div className="d-flex flex-wrap gap-2">
                                {photos.map(photo => (
                                    <div key={photo.id} className="position-relative">
                                        <img src={photo.path} alt={photo.fileName} style={{ height: '80px', width: 'auto', border: '1px solid #ddd' }} />
                                    </div>
                                ))}
                            </div>
                            <small className="form-text text-muted">To manage photos, visit the item details page.</small>
                        </div>
                    )}

                    <div className="d-grid mt-4">
                        <button type="submit" className="btn btn-success" disabled={loading}>
                            {loading ? (
                                <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Updating...</>
                            ) : (
                                <><i className="bi bi-check-circle me-1"></i> Update Item</>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

// Render component
const container = document.getElementById('react-item-edit-form-container');
if (container) {
    const itemId = parseInt(container.getAttribute('data-item-id'), 10);
    const categoriesData = container.getAttribute('data-categories');
    let initialCategories = [];

    try {
        initialCategories = (categoriesData && categoriesData.trim() !== '' && categoriesData.trim() !== 'null')
            ? JSON.parse(categoriesData)
            : [];
    } catch (e) {
        console.error("Error parsing categories data:", e);
    }

    const root = ReactDOM.createRoot(container);
    root.render(
        <StrictMode>
            <ItemEditForm itemId={itemId} initialCategories={initialCategories} />
        </StrictMode>
    );
} else {
    console.error("Container 'react-item-edit-form-container' not found");
}