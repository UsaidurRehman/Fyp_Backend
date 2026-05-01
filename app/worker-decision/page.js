"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Search, UserCheck, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD, SERVER_BASE } from '../../config';

export default function WorkerDecisionScreen() {
    const router = useRouter();
    const [decisions, setDecisions] = useState([]);
    const [searchQuery, setSearchQuery] = useState('');
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => { fetchDecisions(); }, []);

    const fetchDecisions = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetClientWorkerDecisions`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setDecisions(await res.json());
            else NotificationHelper.showError("Failed to fetch decisions.");
        } catch { NotificationHelper.showError("Server error."); }
        finally { setIsLoading(false); }
    };

    const handleConfirm = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/ClientConfirmWorkerAcceptance/${id}`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { NotificationHelper.showSuccess("Worker acceptance successfully confirmed!"); fetchDecisions(); }
            else NotificationHelper.showError("Failed to confirm acceptance.");
        } catch { NotificationHelper.showError("Network Error"); }
    };

    const handleDismiss = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/ClientDismissWorkerRejection/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { setDecisions(prev => prev.filter(d => d.id !== id)); router.push('/find-service'); }
            else NotificationHelper.showError("Failed to dismiss.");
        } catch { NotificationHelper.showError("Network Error"); }
    };

    const filtered = decisions.filter(d => d.workerName?.toLowerCase().includes(searchQuery.toLowerCase()));

    const renderCard = (item) => {
        const isRejected = item.type === 'rejected';
        const borderColor = isRejected ? '#FF5252' : '#4CAF50';
        const statusBg = isRejected ? '#FFCDD2' : '#C8E6C9';
        const statusTextColor = isRejected ? '#D32F2F' : '#388E3C';
        const imgUrl = item.workerImage?.startsWith('/') ? `${SERVER_BASE}${item.workerImage}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';

        return (
            <div key={item.id} className="bg-white rounded-[12px] p-[15px] mb-5 border shadow-md" style={{ borderColor }}>
                <p className="text-[22px] font-bold mb-3" style={{ color: borderColor }}>
                    {isRejected ? 'Worker Rejected!' : 'Worker Accepted!'}
                </p>
                <div className="flex items-center mb-[15px]">
                    <div className="relative">
                        <img src={imgUrl} className="w-[75px] h-[75px] rounded-full bg-[#F0F0F0] object-cover" alt={item.workerName} />
                        <div className="absolute bottom-[2px] right-[2px] bg-white rounded-full p-[2px] border border-[#CCC]">
                            <UserCheck size={12} color="#000" />
                        </div>
                    </div>
                    <div className="ml-[15px]">
                        <p className="text-[24px] font-bold text-[#000]">{item.workerName}</p>
                        <span className="inline-block px-3 py-[3px] rounded-[15px] mt-[6px] text-[13px] font-bold" style={{ backgroundColor: statusBg, color: statusTextColor }}>{item.status}</span>
                    </div>
                </div>
                <div className="mb-5">
                    <p className="text-[15px] text-[#333] mb-[5px]"><span className="font-bold">Decision Date:</span> {item.date}</p>
                    <p className="text-[15px] text-[#333] mb-[5px]"><span className="font-bold">Job Role:</span> {item.role}</p>
                    <p className="text-[15px] text-[#333] mb-[5px]"><span className="font-bold">Address:</span> {item.address}</p>
                    <p className="text-[14px] text-[#444] mt-2 leading-5">{item.message}</p>
                </div>
                <div className="flex justify-center">
                    {item.type === 'accepted' && (
                        <button onClick={() => handleConfirm(item.id)} className="bg-[#1E64D3] px-[50px] py-[10px] rounded-[20px] shadow-md">
                            <span className="text-white font-bold text-[16px]">Confirm</span>
                        </button>
                    )}
                    {item.type === 'rejected' && (
                        <button onClick={() => handleDismiss(item.id)} className="bg-[#B0BEC5] px-[30px] py-[10px] rounded-[20px]">
                            <span className="text-white font-bold text-[16px]">View Other Worker</span>
                        </button>
                    )}
                    {item.type !== 'accepted' && item.type !== 'rejected' && (
                        <div className="bg-[#4CAF50] opacity-80 px-[50px] py-[10px] rounded-[20px]">
                            <span className="text-white font-bold text-[16px]">Hired</span>
                        </div>
                    )}
                </div>
            </div>
        );
    };

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans relative overflow-hidden">
            <div className="absolute top-[-40px] left-[-40px] w-[180px] h-[180px] rounded-full bg-[#E3F2FD] -z-10" />
            <div className="p-5">
                <div className="flex items-center justify-center mb-[15px] relative">
                    <button onClick={() => router.back()} className="absolute left-0 w-[36px] h-[36px] rounded-full bg-[#F5F5F5] flex items-center justify-center">
                        <ArrowLeft size={20} color="#666" />
                    </button>
                    <h1 className="text-[24px] font-bold text-[#000]">Worker Decision</h1>
                    <img src="/images/logo.png" className="absolute right-0 w-[35px] h-[35px]" alt="Logo" />
                </div>
                <div className="flex items-center bg-white rounded-[25px] border border-[#E0E0E0] px-[15px] h-[48px] shadow-sm mb-4">
                    <Search size={24} color="#666" className="mr-2" />
                    <input className="flex-1 text-[15px] outline-none" placeholder="Search by Worker name" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
                </div>
            </div>
            <div className="px-[15px] pb-5">
                {isLoading ? <div className="flex justify-center mt-5"><Loader2 className="animate-spin text-[#1E64D3]" size={40} /></div> :
                    filtered.length === 0 ? <p className="text-center mt-5 italic text-[#999]">No worker decisions available.</p> :
                        filtered.map(renderCard)
                }
            </div>
        </main>
    );
}
