// Path: wwwroot/js/components/userProfile.js

import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Use ApiService
import { getCurrentUserId } from '../utils/auth.js'; 
import UserLocationMap from './userLocationMap.js'; 

// Component UserProfile adapted to use ApiService
const UserProfile = ({ userId }) => {
    const [user, setUser] = useState(null); // Stores user data (e.g., UserWithDetailsDto)
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isCurrentUser, setIsCurrentUser] = useState(false); // State to check if this is the logged-in user's profile

    // Use useCallback for function stability
    const fetchUserData = useCallback(async () => {
        if (!userId || userId <= 0) {
            setError("Invalid User ID.");
            setLoading(false);
            return;
        }
        console.log(`UserProfile: Starting data fetch for userId: ${userId}`);
        setLoading(true);
        setError(null);

        try {
            const userData = await ApiService.get(`/api/users/${userId}`); // Assumes endpoint /api/users/{id} returns UserWithDetailsDto
            console.log("UserProfile: Received user data from ApiService:", userData);

            if (!userData) {
                throw new Error("User data not found in API response.");
            }
            setUser(userData);
            console.log('[UserProfile] Dane użytkownika po fetchu:', userData);
            console.log('[UserProfile] Przekazywany adres do UserLocationMap:', userData?.address);
            console.log('[UserProfile] Przekazywany searchRadius do UserLocationMap:', userData?.searchRadius);


            // Check if this profile belongs to the currently authenticated user
            const currentAuthUserId = getCurrentUserId(); // Function from your auth.js
            setIsCurrentUser(currentAuthUserId === userId);

        } catch (err) {
            console.error("UserProfile: Error fetching user data:", err);
            setError(err.message || 'An error occurred while loading the profile.');
            setUser(null);
        } finally {
            setLoading(false);
        }
    }, [userId]);

    useEffect(() => {
        fetchUserData();
    }, [fetchUserData]);

    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Loading profile...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center" role="alert">
                <span><i className="bi bi-exclamation-triangle-fill me-2"></i>{error}</span>
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchUserData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Try Again
                </button>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="alert alert-warning" role="alert">
                <i className="bi bi-info-circle-fill me-2"></i>
                Could not load profile data or user does not exist.
            </div>
        );
    }

    const defaultAvatar = ApiService.getImageUrl(null); // Get default placeholder URL
    const { firstName, lastName, email, avatarPath, ecoScore, createdDate, lastActivity, address, averageRating, badges, searchRadius } = user; // Destructure searchRadius from user

    return (
        <div className="card shadow-sm mb-4"> {/* Added mb-4 for spacing if multiple cards */}
            <div className="card-body">
                <div className="row">
                    {/* Left Column */}
                    <div className="col-md-4 text-center mb-3 mb-md-0">
                        <img
                            src={ApiService.getImageUrl(avatarPath)}
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
                        {averageRating !== undefined && averageRating > 0 && (
                            <div className="mt-2">
                                 <span className="badge bg-warning text-dark fs-6">
                                     <i className="bi bi-star-fill me-1"></i>
                                     Rating: {averageRating.toFixed(1)}/5.0
                                 </span>
                            </div>
                        )}
                    </div>

                    {/* Right Column */}
                    <div className="col-md-8">
                        <h2>{firstName || 'User'} {lastName || ''}</h2>
                        {email && (
                            <p className="text-muted mb-2">
                                <i className="bi bi-envelope-fill me-2 text-success"></i>{email}
                            </p>
                        )}
                        {createdDate && (
                            <p className="card-text mb-1">
                                <small className="text-muted">
                                    <i className="bi bi-calendar-check me-2"></i>
                                    Joined: {new Date(createdDate).toLocaleDateString()} {/* Use default locale or specify */}
                                </small>
                            </p>
                        )}
                        {lastActivity && (
                            <p className="card-text mb-3">
                                <small className="text-muted">
                                    <i className="bi bi-clock-history me-2"></i>
                                    Last activity: {new Date(lastActivity).toLocaleString()} {/* Use default locale or specify */}
                                </small>
                            </p>
                        )}

                        {/* Textual Address Display */}
                        {address && (address.street || address.city || address.country) ? ( // Check if any main address field exists
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-geo-alt-fill me-2 text-success"></i>Address</h5>
                                <address className="mb-0" style={{ lineHeight: '1.4' }}>
                                    {address.street && <>{address.street}<br /></>}
                                    {address.buildingNumber && <>{address.buildingNumber}</>}
                                    {address.apartmentNumber && <>{`, apt. ${address.apartmentNumber}`}{(address.postalCode || address.city) ? <br /> : ''}</>}
                                    {(!address.buildingNumber && address.apartmentNumber && (address.postalCode || address.city)) && <br />}
                                    {address.postalCode && <>{address.postalCode}{` `}</>}
                                    {address.city && <>{address.city}{(address.province || address.country) ? <br /> : ''}</>}
                                    {address.province && <>{address.province}{(address.country && address.province !== address.country) ? <br /> : ''}</>}
                                    {address.country && address.province !== address.country && <>{address.country}</>}
                                </address>
                            </div>
                        ) : (
                            !isCurrentUser && <p className="text-muted mt-3 pt-3 border-top">No location information provided.</p>
                            // If it's the current user and no address, UserLocationMap will show "Set Location"
                        )}

                        {/* Badges */}
                        {badges && badges.length > 0 && (
                            <div className="mt-3 pt-3 border-top">
                                <h5 className="mb-2"><i className="bi bi-trophy-fill me-2 text-success"></i>Badges</h5>
                                <div className="d-flex flex-wrap gap-2">
                                    {badges.map(badge => (
                                        <span key={badge.id} className="badge text-bg-secondary p-2 d-inline-flex align-items-center" title={badge.description || badge.name}>
                                            {badge.iconPath && ApiService.getImageUrl(badge.iconPath) && (
                                                <img
                                                    src={ApiService.getImageUrl(badge.iconPath)}
                                                    alt=""
                                                    className="me-1"
                                                    style={{ height: '1.1em', width: 'auto', verticalAlign: 'middle' }}
                                                />
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

            {/* User Location Map Section - RENDER UserLocationMap HERE */}
            {/* It will only render if (address has coordinates) OR (it's the current user) */}
            <UserLocationMap
                address={address} // Pass the address object (can be null if user has no address)
                userSearchRadius={searchRadius} // Pass the user's preferred search radius
                userId={userId}    // The ID of the profile being viewed
                isCurrentUser={isCurrentUser} // Is this the logged-in user's own profile?
            />
            {/* End of User Location Map Section */}
        </div>
    );
};

// --- Component Initialization Logic (translate console logs and error messages) ---
const userProfileContainer = document.getElementById('react-user-profile');
if (userProfileContainer) {
    const userIdString = userProfileContainer.getAttribute('data-user-id');
    const userId = parseInt(userIdString, 10);

    if (!isNaN(userId) && userId > 0) {
        const root = ReactDOM.createRoot(userProfileContainer);
        root.render(
            <StrictMode>
                <UserProfile userId={userId} />
            </StrictMode>
        );
        console.log(`UserProfile component initialized for UserID: ${userId}`);
    } else {
        console.error(`UserProfile: Invalid or missing user ID in data-user-id attribute. Value found: "${userIdString}". Parsed as: ${userId}`);
        if (userProfileContainer) { // Check again as it might be null
            userProfileContainer.innerHTML = '<div class="alert alert-danger">Critical Error: Cannot load profile. Invalid User ID specified in HTML attribute.</div>';
        }
    }
} else {
}