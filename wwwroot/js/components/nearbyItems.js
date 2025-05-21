
import React, { useState, useEffect, useCallback } from 'react';
import ApiService from '../services/api.js'; 
import LocationPicker from './locationPicker.js'; 

/**
 * Component for displaying items near a selected location.
 * @param {Object} props
 * @param {boolean} props.showLocationPicker Whether to show the location picker.
 * @param {Object} props.initialLocation Optional initial location { latitude, longitude, searchRadius, locationName }.
 * @param {number} props.userId Optional user ID (primarily for LocationPicker if it needs to save user-specific preferences, though not used directly by NearbyItems for fetching).
 */
const NearbyItems = ({ showLocationPicker = true, initialLocation = null, userId = null }) => {
    const [location, setLocation] = useState(initialLocation || {
        latitude: null, // Will be set by LocationPicker or user input
        longitude: null,
        searchRadius: 10, // Default search radius
        locationName: ''
    });
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [totalCount, setTotalCount] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedCategory, setSelectedCategory] = useState(null); // Store category ID
    const [categories, setCategories] = useState([]);
    const [hasLocationBeenSet, setHasLocationBeenSet] = useState(!!(initialLocation && initialLocation.latitude && initialLocation.longitude));

    // Fetch categories for the filter dropdown
    useEffect(() => {
        const fetchCategories = async () => {
            try {
                const categoriesData = await ApiService.get('/api/categories'); // Assumes this endpoint exists and returns Array<CategoryDto>
                setCategories(categoriesData || []);
            } catch (err) {
                console.error('Error fetching categories:', err);
                // Not setting a global error as this might not be critical for core functionality
            }
        };
        fetchCategories();
    }, []);

    // Callback for when location is changed by LocationPicker
    const handleLocationChange = useCallback((newLocation) => {
        setLocation(newLocation);
        setHasLocationBeenSet(!!(newLocation && newLocation.latitude && newLocation.longitude));
        setPage(1); // Reset to first page when location changes
    }, []);

    // Fetch nearby items based on current location, filters, and pagination
    const fetchNearbyItems = useCallback(async () => {
        if (!hasLocationBeenSet || typeof location.latitude !== 'number' || typeof location.longitude !== 'number') {
            setItems([]); // Clear items if no valid location
            setTotalPages(0);
            setTotalCount(0);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const params = new URLSearchParams({
                lat: location.latitude,
                lon: location.longitude,
                radius: location.searchRadius,
                page: page,
                pageSize: 12 // Example page size
            });

            if (searchTerm) {
                params.append('searchTerm', searchTerm);
            }
            if (selectedCategory) { // Check if selectedCategory is not null/0
                params.append('categoryId', selectedCategory);
            }

            // API call to the endpoint we defined in NearbyItemsController
            const result = await ApiService.get(`/api/nearbyitems?${params.toString()}`);

            // Assuming API returns PagedResult<ItemSummaryDto>
            // The controller maps this to ApiResponse<PagedResult<ItemSummaryDto>>
            // ApiService.get should ideally return the 'data' part of ApiResponse, which is PagedResult
            setItems(result.items || []);
            setTotalPages(result.totalPages || 0);
            setTotalCount(result.totalCount || 0);

        } catch (err) {
            console.error('Error fetching nearby items:', err);
            setError(err.message || 'An error occurred while searching for nearby items.');
            setItems([]); // Clear items on error
            setTotalPages(0);
            setTotalCount(0);
        } finally {
            setLoading(false);
        }
    }, [location, page, searchTerm, selectedCategory, hasLocationBeenSet]);

    // Effect to fetch items when dependencies change
    useEffect(() => {
        fetchNearbyItems();
    }, [fetchNearbyItems]); // fetchNearbyItems is memoized by useCallback

    // Handler for submitting the search form
    const handleSearchFormSubmit = (e) => {
        e.preventDefault();
        setPage(1); // Reset to first page on new search
        // fetchNearbyItems will be triggered by useEffect due to searchTerm/selectedCategory change (if they are deps of fetchNearbyItems)
        // Explicit call might be needed if those states aren't direct deps, but current setup should work.
    };

    // Handler for changing page
    const handlePageChange = (newPage) => {
        if (newPage >= 1 && newPage <= totalPages) {
            window.scrollTo({ top: 0, behavior: 'smooth' });
            setPage(newPage);
        }
    };

    // Handler for changing category
    const handleCategoryChange = (e) => {
        const categoryId = e.target.value ? parseInt(e.target.value, 10) : null;
        setSelectedCategory(categoryId);
        setPage(1); // Reset to first page on category change
    };

    const renderSearchPanel = () => (
        <div className="mb-4">
            <div className="card shadow-sm">
                <div className="card-body">
                    <h5 className="card-title">
                        <i className="bi bi-search me-2 text-success"></i>
                        Search Items
                    </h5>
                    <form onSubmit={handleSearchFormSubmit}>
                        <div className="row g-3">
                            <div className="col-md-6">
                                <div className="input-group">
                                    <input
                                        type="text"
                                        className="form-control"
                                        placeholder="What are you looking for?"
                                        value={searchTerm}
                                        onChange={(e) => setSearchTerm(e.target.value)}
                                    />
                                    <button className="btn btn-success" type="submit">
                                        <i className="bi bi-search"></i>
                                    </button>
                                </div>
                            </div>
                            <div className="col-md-6">
                                <select
                                    className="form-select"
                                    value={selectedCategory || ''}
                                    onChange={handleCategoryChange}
                                >
                                    <option value="">All Categories</option>
                                    {categories.map(category => (
                                        <option key={category.id} value={category.id}>
                                            {category.name}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );

    const renderLocationPickerSection = () => {
        if (!showLocationPicker) return null;
        return (
            <div className="mb-4">
                {/* LocationPicker already has a card structure, so no extra card needed here unless desired */}
                <LocationPicker
                    initialLocation={initialLocation} // Pass initial location if available
                    onLocationChange={handleLocationChange}
                    userId={userId} // Pass userId if location picker needs to save preferences for this user
                    readOnly={false} // Allow user to pick location for search
                />
            </div>
        );
    };

    const renderItemsList = () => {
        if (!hasLocationBeenSet && showLocationPicker) {
            return (
                <div className="alert alert-info">
                    <i className="bi bi-info-circle-fill me-2"></i>
                    Please select a location on the map to see nearby items.
                </div>
            );
        }
        if (loading && items.length === 0) { // Show main loader only if no items are currently displayed
            return (
                <div className="d-flex justify-content-center py-5">
                    <div className="spinner-border text-success" role="status" style={{width: "3rem", height: "3rem"}}>
                        <span className="visually-hidden">Loading items...</span>
                    </div>
                </div>
            );
        }
        if (error) {
            return (
                <div className="alert alert-danger">
                    <i className="bi bi-exclamation-triangle-fill me-2"></i>
                    {error}
                    <button className="btn btn-outline-danger btn-sm ms-3" onClick={fetchNearbyItems}>
                        <i className="bi bi-arrow-clockwise me-1"></i> Try Again
                    </button>
                </div>
            );
        }
        if (!loading && items.length === 0 && hasLocationBeenSet) {
            return (
                <div className="alert alert-warning">
                    <i className="bi bi-search me-2"></i>
                    No items found in the area
                    {searchTerm ? ` matching your search "${searchTerm}"` : ''}.
                    {selectedCategory && categories.find(c => c.id === selectedCategory) ? ` in category "${categories.find(c => c.id === selectedCategory).name}"` : ''}
                    {location.searchRadius < 50 && ' Try increasing the search radius.'}
                </div>
            );
        }

        return (
            <>
                {totalCount > 0 && (
                    <div className="mb-3 text-muted">
                        <i className="bi bi-geo-alt me-1"></i>
                        Found <strong>{totalCount}</strong> items within
                        <strong> {location.searchRadius} km</strong>
                        {location.locationName ? ` of ${location.locationName}` : ' of selected point'}
                        {searchTerm ? `, for query "${searchTerm}"` : ''}
                        {selectedCategory && categories.find(c => c.id === selectedCategory) ? ` in category "${categories.find(c => c.id === selectedCategory).name}"` : ''}.
                    </div>
                )}

                <div className="row row-cols-1 row-cols-sm-2 row-cols-lg-3 row-cols-xl-4 g-4 mb-4">
                    {items.map(item => ( // Assuming item is ItemSummaryDto
                        <div key={item.id} className="col">
                            <div className="card h-100 shadow-sm item-card-hover">
                                <div className="position-relative">
                                    <img
                                        src={item.mainPhotoPath ? ApiService.getImageUrl(item.mainPhotoPath) : ApiService.getImageUrl('/img/placeholder-item.png')}
                                        className="card-img-top"
                                        alt={item.name} // Use item.name from ItemSummaryDto
                                        style={{ height: '200px', objectFit: 'cover' }}
                                        onError={(e) => { e.target.src = ApiService.getImageUrl('/img/placeholder-item.png');}}
                                    />
                                    {item.categoryName && ( // Use item.categoryName from ItemSummaryDto
                                        <span className="badge bg-success-subtle text-success-emphasis position-absolute top-0 end-0 m-2">{item.categoryName}</span>
                                    )}
                                    <span className={`badge position-absolute bottom-0 end-0 m-2 ${item.isForExchange ? 'bg-primary-subtle text-primary-emphasis' : 'bg-info-subtle text-info-emphasis'}`}>
                                        {item.isForExchange ? 'For Exchange' : 'For Giving Away'}
                                    </span>
                                    <h3>{item.name}</h3>
                                    <p>{item.description}</p>
                                    <p>Condition: {item.condition}</p>
                                    <p>User: {item.userName}</p>
                                    <p>Location: {item.city}</p>
                                    <a href={`/Items/Details/${item.id}`} class="btn btn-primary">View Details</a>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>

                {totalPages > 1 && (
                    <nav aria-label="Page navigation">
                        <ul className="pagination justify-content-center">
                            <li className={`page-item ${page <= 1 ? 'disabled' : ''}`}>
                                <button className="page-link" onClick={() => handlePageChange(page - 1)} disabled={page <= 1}>
                                    <i className="bi bi-chevron-left"></i>
                                </button>
                            </li>
                            {/* Simplified pagination display logic */}
                            {[...Array(totalPages).keys()].map(num => {
                                const pageNum = num + 1;
                                // Add logic here to show only a few page numbers if totalPages is large
                                // For now, showing all for simplicity if totalPages <= 5, else a more complex logic
                                if (totalPages > 7 && Math.abs(page - pageNum) > 2 && pageNum !== 1 && pageNum !== totalPages && !(pageNum === 2 && page <=4) && !(pageNum === totalPages -1 && page >= totalPages -3 )) {
                                    if ( (pageNum === 2 && Math.abs(page-pageNum) > 2) || (pageNum === totalPages - 1 && Math.abs(page-pageNum)>2) ) {
                                        return <li key={`ellipsis-${pageNum}`} className="page-item disabled"><span className="page-link">...</span></li>;
                                    }
                                    return null;
                                }
                                return (
                                    <li key={pageNum} className={`page-item ${page === pageNum ? 'active' : ''}`}>
                                        <button className="page-link" onClick={() => handlePageChange(pageNum)}>{pageNum}</button>
                                    </li>
                                );
                            })}
                            <li className={`page-item ${page >= totalPages ? 'disabled' : ''}`}>
                                <button className="page-link" onClick={() => handlePageChange(page + 1)} disabled={page >= totalPages}>
                                    <i className="bi bi-chevron-right"></i>
                                </button>
                            </li>
                        </ul>
                    </nav>
                )}
            </>
        );
    };

    return (
        <div className="nearby-items-page container mt-4">
            {renderSearchPanel()}
            {renderLocationPickerSection()}
            <div className={(loading && items.length > 0) ? 'opacity-75 position-relative' : 'position-relative'}>
                {renderItemsList()}
                {/* Overlay loader when items are already present and new ones are loading */}
                {loading && items.length > 0 && (
                    <div className="position-absolute top-50 start-50 translate-middle" style={{zIndex: 10}}>
                        <div className="spinner-border text-success" role="status" style={{width: "3rem", height: "3rem"}}>
                            <span className="visually-hidden">Refreshing items...</span>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default NearbyItems;