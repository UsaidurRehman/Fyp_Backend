// app/Notification/NotificationHelper.js

const NotificationHelper = {
  showSuccess: (message) => {
    if (typeof window !== 'undefined') {
      const event = new CustomEvent('app-notification', {
        detail: { message, type: 'success' }
      });
      window.dispatchEvent(event);
    }
  },
  showError: (message) => {
    if (typeof window !== 'undefined') {
      const event = new CustomEvent('app-notification', {
        detail: { message, type: 'error' }
      });
      window.dispatchEvent(event);
    }
  },
  showInfo: (message) => {
    if (typeof window !== 'undefined') {
      const event = new CustomEvent('app-notification', {
        detail: { message, type: 'info' }
      });
      window.dispatchEvent(event);
    }
  },
};

export default NotificationHelper;