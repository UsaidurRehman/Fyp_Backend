// =============================================================
//  CENTRAL API CONFIGURATION
// =============================================================

// For Next.js (Web) + Backend running on same PC:
const IP_ADDRESS = 'localhost';
const PORT = '5150';
export const SERVER_BASE = `http://${IP_ADDRESS}:${PORT}`;

export const API_DASHBOARD = `${SERVER_BASE}/api/Dashboard`;
export const API_AUTH = `${SERVER_BASE}/api/Auth`;
export const API_ACCOUNT = `${SERVER_BASE}/api/AccountCreation`;