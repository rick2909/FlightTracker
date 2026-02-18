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
        
        // Check if response is a redirect (controller returns RedirectToAction)
        if (response.type === 'opaqueredirect' || (response.status >= 300 && response.status < 400)) {
            return { success: true, redirectUrl: response.headers.get('Location') };
        }
        
        return { success: response.ok };
    } catch (error) {
        console.error('Form submission failed:', error);
        return { success: false };
    }
};
