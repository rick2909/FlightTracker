/**
 * Form helper utilities for FlightTracker
 * Handles form submissions with anti-forgery tokens
 */

window.FlightTracker = window.FlightTracker || {};

/**
 * Submit a form with anti-forgery token protection
 * @param {string} url - The endpoint URL
 * @param {string} token - The anti-forgery token
 * @param {Object} data - Form data as key-value pairs
 * @returns {Promise<{success: boolean, redirectUrl?: string}>}
 */
window.FlightTracker.submitForm = async function(url, token, data) {
    try {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);
        
        for (const [key, value] of Object.entries(data)) {
            if (value !== null && value !== undefined) {
                formData.append(key, value);
            }
        }
        
        const response = await fetch(url, {
            method: 'POST',
            body: formData,
            credentials: 'same-origin',
            redirect: 'manual'
        });
        
        // Treat redirects (RedirectToAction) as success
        if (response.type === 'opaqueredirect' || (response.status >= 300 && response.status < 400) || response.redirected) {
            return { success: true, redirectUrl: response.headers.get('Location') };
        }

        // If JSON, allow server to signal success/failure
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            const payload = await response.json();
            if (typeof payload?.success === 'boolean') {
                return { success: payload.success, errors: payload.errors };
            }
            return { success: response.ok };
        }

        // Non-redirect HTML (e.g., validation errors) should be treated as failure
        return { success: false };
    } catch (error) {
        console.error('Form submission failed:', error);
        return { success: false };
    }
};
