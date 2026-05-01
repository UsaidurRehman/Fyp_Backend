"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { CheckCircle, XCircle, Info, X } from 'lucide-react';

const Toast = () => {
    const [notifications, setNotifications] = useState([]);

    const removeNotification = useCallback((id) => {
        setNotifications((prev) => prev.filter((n) => n.id !== id));
    }, []);

    useEffect(() => {
        const handleNotification = (event) => {
            const { message, type, id = Date.now() } = event.detail;
            setNotifications((prev) => [...prev, { id, message, type }]);

            // Auto-remove after 4 seconds
            setTimeout(() => {
                removeNotification(id);
            }, 4000);
        };

        window.addEventListener('app-notification', handleNotification);
        return () => window.removeEventListener('app-notification', handleNotification);
    }, [removeNotification]);

    if (notifications.length === 0) return null;

    return (
        <div className="fixed top-5 right-5 z-[9999] flex flex-col gap-3 pointer-events-none w-full max-w-[350px]">
            {notifications.map((n) => (
                <div
                    key={n.id}
                    className={`pointer-events-auto flex items-center p-4 rounded-[16px] shadow-2xl border animate-toast-in ${
                        n.type === 'success' ? 'bg-[#E8F5E9] border-[#4CAF50] text-[#2E7D32]' :
                        n.type === 'error' ? 'bg-[#FFEBEE] border-[#EF5350] text-[#C62828]' :
                        'bg-[#E3F2FD] border-[#2196F3] text-[#1565C0]'
                    }`}
                >
                    <div className="mr-3 shrink-0">
                        {n.type === 'success' && <CheckCircle size={24} />}
                        {n.type === 'error' && <XCircle size={24} />}
                        {n.type === 'info' && <Info size={24} />}
                    </div>
                    <p className="text-[14px] font-bold flex-1 leading-tight">{n.message}</p>
                    <button 
                        onClick={() => removeNotification(n.id)}
                        className="ml-2 p-1 hover:bg-black/5 rounded-full transition-colors"
                    >
                        <X size={18} />
                    </button>
                </div>
            ))}
        </div>
    );
};

export default Toast;
