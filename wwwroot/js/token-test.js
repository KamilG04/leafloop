// Path: wwwroot/js/token-test.js
document.addEventListener('DOMContentLoaded', function() {
    const testDiv = document.createElement('div');
    testDiv.style.position = 'fixed';
    testDiv.style.bottom = '10px';
    testDiv.style.right = '10px';
    testDiv.style.padding = '10px';
    testDiv.style.background = '#f8f9fa';
    testDiv.style.border = '1px solid #ddd';
    testDiv.style.borderRadius = '4px';
    testDiv.style.zIndex = '9999';
    testDiv.style.fontSize = '12px';
    testDiv.style.maxWidth = '300px';

    // Check localStorage
    let localToken = null;
    try {
        localToken = localStorage.getItem('jwt_token');
    } catch (e) {
        console.error('localStorage error:', e);
    }

    // Check cookies
    const cookies = document.cookie.split(';').reduce((acc, cookie) => {
        const [name, value] = cookie.trim().split('=');
        acc[name] = value;
        return acc;
    }, {});

    // Create report
    testDiv.innerHTML = `
        <h4>Auth Token Diagnostics</h4>
        <p><strong>localStorage token:</strong> ${localToken ? '✅ Present' : '❌ Missing'}</p>
        <p><strong>Cookie token:</strong> ${cookies['jwt_token'] ? '✅ Present' : '❌ Missing'}</p>
        <p><strong>All cookies:</strong> ${Object.keys(cookies).join(', ') || 'None found'}</p>
        <button id="close-token-test" style="position:absolute;top:5px;right:5px;border:none;background:none;cursor:pointer;">×</button>
    `;

    document.body.appendChild(testDiv);

    document.getElementById('close-token-test').addEventListener('click', () => {
        testDiv.remove();
    });
});