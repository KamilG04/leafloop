// Simple test to verify token storage and retrieval
document.addEventListener('DOMContentLoaded', function() {
    // Try to get token from various sources
    const localStorageToken = localStorage.getItem('jwt_token');
    console.log("localStorage token:", localStorageToken ? "Present" : "Missing");

    // Check cookies
    console.log("document.cookie:", document.cookie);
    const jwtCookie = document.cookie.split('; ')
        .find(row => row.startsWith('jwt_token='));
    console.log("JWT cookie:", jwtCookie ? "Present" : "Missing");

    // Try a fetch with auth headers (to console)
    const headers = {};
    const token = localStorageToken || (jwtCookie ? jwtCookie.split('=')[1] : null);

    if (token) {
        headers.Authorization = `Bearer ${token}`;
        console.log("Auth header:", headers.Authorization);
    } else {
        console.log("No token available for Authorization header");
    }
});