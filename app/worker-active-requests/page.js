"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, MapPin, Phone, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD } from '../../config';

export default function WorkerActiveRequestsScreen() {
    const router = useRouter();
    const [requests, setRequests] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => { fetchRequests(); }, []);

    const fetchRequests = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetWorkerRequests`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setRequests(await res.json());
            else NotificationHelper.showError("Failed to load requests.");
        } catch { NotificationHelper.showError("Could not connect to server."); }
        finally { setIsLoading(false); }
    };

    const handleStatusUpdate = async (id, decision) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/UpdateWorkerDecision/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ workerDecision: decision })
            });
            if (res.ok) { NotificationHelper.showSuccess(`Request ${decision}!`); setRequests(p => p.filter(r => r.id !== id)); }
            else NotificationHelper.showError("Failed to update status.");
        } catch { NotificationHelper.showError("Could not connect to server."); }
    };

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans">
            <div className="flex items-start p-5">
                <button onClick={() => router.back()} className="w-[35px] h-[35px] rounded-full bg-[#F0F0F0] flex items-center justify-center mt-[5px] shadow">
                    <ArrowLeft size={20} color="#666" />
                </button>
                <div className="ml-[15px] flex-1">
                    <h1 className="text-[26px] font-bold text-[#000]">Active Requests</h1>
                    <div className="flex justify-between mt-[5px]">
                        <span className="text-[14px] text-[#888]">Request pending</span>
                        <button onClick={() => router.push('/accepted-request')} className="text-[14px] text-[#1E64D3] font-bold">
                            Goto Accepted
                        </button>
                    </div>
                </div>
            </div>

            <div className="px-5 pb-[30px]">
                <p className="text-[20px] font-bold text-[#000] mb-5">New Booking Requests ({requests.length})</p>

                {isLoading ? (
                    <div className="flex justify-center"><Loader2 className="animate-spin text-[#1E64D3]" size={40} /></div>
                ) : requests.length === 0 ? (
                    <p className="text-center italic text-[#999] mt-5">No pending requests.</p>
                ) : requests.map(item => (
                    <div key={item.id} className="bg-white rounded-[20px] p-[15px] mb-[15px] border border-[#EEE] shadow-md">
                        <div className="flex justify-between items-center mb-3">
                            <span className="border border-[#1E64D3] px-[15px] py-1 rounded-[10px] text-[12px] text-[#1E64D3] font-bold">{item.service}</span>
                            <div className="flex items-center">
                                <div className="w-2 h-2 rounded-full bg-[#4CAF50] mr-[6px]" />
                                <span className="text-[12px] text-[#888]">{item.time}</span>
                            </div>
                        </div>
                        <p className="text-[18px] font-bold text-[#000] mb-[5px]">Client: {item.client}</p>
                        <div className="flex items-center mb-[5px]">
                            <MapPin size={18} color="#E91E63" />
                            <span className="text-[14px] text-[#666] ml-[5px]">{item.location}</span>
                        </div>
                        <div className="flex items-center mb-[15px]">
                            <Phone size={18} color="#4CAF50" />
                            <span className="text-[14px] text-[#666] ml-[5px]">{item.clientPhone}</span>
                        </div>
                        <div className="flex justify-between gap-3">
                            <button onClick={() => handleStatusUpdate(item.id, 'Rejected')}
                                className="flex-1 h-[45px] rounded-[22px] bg-[#F5F5F5] border border-[#DDD] flex items-center justify-center">
                                <span className="text-[#666] font-bold">Reject</span>
                            </button>
                            <button onClick={() => handleStatusUpdate(item.id, 'Accepted')}
                                className="flex-1 h-[45px] rounded-[22px] bg-[#4CAF50] flex items-center justify-center">
                                <span className="text-white font-bold">Accept Booking</span>
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
