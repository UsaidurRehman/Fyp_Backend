"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, MapPin, Phone, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD } from '../../config';

export default function AcceptedRequestScreen() {
    const router = useRouter();
    const [acceptedRequests, setAcceptedRequests] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => { fetchAcceptedRequests(); }, []);

    const fetchAcceptedRequests = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetAcceptedWorkerRequests`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setAcceptedRequests(await res.json());
            else NotificationHelper.showError("Failed to load accepted requests.");
        } catch { NotificationHelper.showError("Could not connect to server."); }
        finally { setIsLoading(false); }
    };

    const handleReject = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/UpdateWorkerDecision/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ decision: 'Rejected' })
            });
            if (res.ok) { NotificationHelper.showSuccess("Request Rejected successfully."); setAcceptedRequests(p => p.filter(r => r.id !== id)); }
            else NotificationHelper.showError("Failed to reject request.");
        } catch { NotificationHelper.showError("Could not connect to server."); }
    };

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans relative overflow-hidden">
            <div className="absolute top-[-50px] left-[-50px] w-[200px] h-[200px] rounded-full bg-[#E3F2FD] -z-10" />
            <div className="p-5">
                <button onClick={() => router.back()} className="w-[40px] h-[40px] rounded-full bg-[#F5F5F5] flex items-center justify-center mb-5 shadow-md">
                    <ArrowLeft size={24} color="#666" />
                </button>
                <div className="mb-[30px]">
                    <h1 className="text-[28px] font-bold text-[#000]">Accepted Request</h1>
                    <p className="text-[16px] text-[#BDBDBD] mt-[5px]">Accepted Offers</p>
                </div>
                <p className="text-[20px] font-bold text-[#000] mb-5">New Accepted Requests ({acceptedRequests.length})</p>

                {isLoading ? (
                    <div className="flex justify-center mt-5"><Loader2 className="animate-spin text-[#4CAF50]" size={40} /></div>
                ) : acceptedRequests.length === 0 ? (
                    <p className="text-center mt-5 italic text-[#999]">No accepted requests found.</p>
                ) : acceptedRequests.map(item => (
                    <div key={item.id} className="bg-white rounded-[20px] p-[15px] mb-5 border border-[#F0F0F0] shadow-md">
                        <div className="flex justify-between items-center mb-[10px]">
                            <span className="border border-[#1E64D3] rounded-[10px] px-3 py-1 text-[12px] text-[#1E64D3] font-semibold">{item.service}</span>
                            <div className="flex items-center">
                                <div className="w-2 h-2 rounded-full bg-[#4CAF50] mr-[6px]" />
                                <span className="text-[14px] text-[#4CAF50] font-medium">Accepted</span>
                            </div>
                        </div>
                        <p className="text-[18px] font-bold text-[#000] mb-[10px]">Client: {item.client}</p>
                        <div className="flex items-center mb-[15px]">
                            <MapPin size={18} color="#E91E63" className="mr-2" />
                            <span className="text-[14px] text-[#666]">{item.location}</span>
                        </div>
                        <div className="flex items-center mb-[15px]">
                            <Phone size={18} color="#4CAF50" className="mr-2" />
                            <span className="text-[14px] text-[#666]">{item.clientPhone}</span>
                        </div>
                        <div className="flex justify-end">
                            <button onClick={() => handleReject(item.id)} className="bg-[#F5F5F5] px-[25px] py-2 rounded-[20px] border border-[#E0E0E0]">
                                <span className="text-[#9E9E9E] font-bold text-[16px]">Reject</span>
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
