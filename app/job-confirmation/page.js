"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Search, UserCheck, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD, SERVER_BASE } from '../../config';

export default function JobConfirmationScreen() {
    const router = useRouter();
    const [jobs, setJobs] = useState([]);
    const [searchQuery, setSearchQuery] = useState('');
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => { fetchJobConfirmations(); }, []);

    const fetchJobConfirmations = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetWorkerJobConfirmations`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setJobs(await res.json());
            else NotificationHelper.showError("Failed to fetch jobs.");
        } catch { NotificationHelper.showError("Server error."); }
        finally { setIsLoading(false); }
    };

    const handleAcceptJob = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/WorkerAcceptJobOffer/${id}`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { NotificationHelper.showSuccess("Job Accepted! You are now hired."); fetchJobConfirmations(); }
            else NotificationHelper.showError("Error accepting job.");
        } catch { NotificationHelper.showError("Network Error"); }
    };

    const handleRejectJob = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/WorkerRejectJobOffer/${id}`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { NotificationHelper.showSuccess("Job offer rejected."); fetchJobConfirmations(); }
            else NotificationHelper.showError("Error rejecting job.");
        } catch { NotificationHelper.showError("Network Error"); }
    };

    const handleDeleteJob = async (id) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/DeleteInterviewRequest/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { NotificationHelper.showSuccess("Job request removed."); setJobs(p => p.filter(j => j.id !== id)); }
            else NotificationHelper.showError("Failed to remove request.");
        } catch { NotificationHelper.showError("Network Error"); }
    };

    const filtered = jobs.filter(j => j.clientName?.toLowerCase().includes(searchQuery.toLowerCase()));

    const renderCard = (item) => {
        const isNeg = item.type === 'rejected' || item.type === 'terminated';
        const borderColor = isNeg ? '#FF5252' : '#4CAF50';
        const statusBg = isNeg ? '#FFCDD2' : '#C8E6C9';
        const statusTextColor = isNeg ? '#D32F2F' : '#388E3C';
        const imgUrl = item.clientImage?.startsWith('/') ? `${SERVER_BASE}${item.clientImage}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';
        const headerText = item.type === 'terminated' ? 'Contract Terminated!' : item.type === 'rejected' ? 'Job Rejected!' : 'Job Offered!';

        return (
            <div key={item.id} className="bg-white rounded-[15px] p-[15px] mb-5 border shadow-md" style={{ borderColor }}>
                <p className="text-[20px] font-bold mb-[10px]" style={{ color: borderColor }}>{headerText}</p>
                <div className="flex items-center mb-[15px]">
                    <div className="relative">
                        <img src={imgUrl} className="w-[70px] h-[70px] rounded-full bg-[#EEE] object-cover" alt={item.clientName} />
                        <div className="absolute bottom-0 right-0 bg-white rounded-full p-[2px] border border-[#CCC]"><UserCheck size={12} /></div>
                    </div>
                    <div className="ml-[15px]">
                        <p className="text-[22px] font-bold text-[#000]">{item.clientName}</p>
                        <span className="inline-block px-[15px] py-[3px] rounded-[15px] mt-[5px] text-[13px] font-bold" style={{ backgroundColor: statusBg, color: statusTextColor }}>{item.status}</span>
                    </div>
                </div>
                <div className="mb-[15px]">
                    <p className="text-[15px] text-[#333] mb-1"><span className="font-bold">Interview Date:</span> {item.date}</p>
                    <p className="text-[15px] text-[#333] mb-1"><span className="font-bold">Job Role:</span> {item.role}</p>
                    <p className="text-[15px] text-[#333] mb-1"><span className="font-bold">Address:</span> {item.address}</p>
                    <p className="text-[14px] text-[#444] leading-5 mt-[5px]">{item.message}</p>
                </div>
                <div className="flex justify-end gap-[15px] mt-[10px]">
                    {item.type === 'offered' && (<>
                        <button onClick={() => handleRejectJob(item.id)} className="bg-[#CFD8DC] px-[30px] py-[10px] rounded-[20px]"><span className="text-[#607D8B] font-bold text-[16px]">Reject</span></button>
                        <button onClick={() => handleAcceptJob(item.id)} className="bg-[#1E64D3] px-[30px] py-[10px] rounded-[20px]"><span className="text-white font-bold text-[16px]">Accept</span></button>
                    </>)}
                    {(item.type === 'rejected' || item.type === 'terminated') && (
                        <button onClick={() => handleDeleteJob(item.id)} className={`px-[30px] py-[10px] rounded-[20px] ${item.type === 'terminated' ? 'bg-[#FF5252]' : 'bg-[#CFD8DC]'}`}>
                            <span className={`font-bold text-[16px] ${item.type === 'terminated' ? 'text-white' : 'text-[#607D8B]'}`}>Delete</span>
                        </button>
                    )}
                    {item.type === 'final' && (
                        <button disabled className="bg-[#008000] px-[40px] py-[10px] rounded-[20px] w-full flex justify-center"><span className="text-white font-bold text-[16px]">Accepted</span></button>
                    )}
                </div>
            </div>
        );
    };

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans relative overflow-hidden">
            <div className="absolute top-[-50px] left-[-50px] w-[200px] h-[200px] rounded-full bg-[#E3F2FD] -z-10" />
            <div className="p-5">
                <div className="flex justify-between items-center mb-[15px]">
                    <button onClick={() => router.back()} className="p-[5px]"><ArrowLeft size={24} color="#555" /></button>
                    <h1 className="text-[24px] font-bold text-[#000]">Job Confirmation</h1>
                    <img src="/images/logo.png" className="w-[40px] h-[40px]" alt="Logo" />
                </div>
                <div className="flex items-center bg-white rounded-[25px] border border-[#CCC] px-[15px] h-[45px] shadow-sm mb-4">
                    <Search size={24} color="#666" className="mr-[10px]" />
                    <input className="flex-1 text-[14px] outline-none" placeholder="Search by client name" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
                </div>
            </div>
            <div className="px-[15px] pb-5">
                {isLoading ? <div className="flex justify-center mt-5"><Loader2 className="animate-spin text-[#1E64D3]" size={40} /></div> :
                    filtered.length === 0 ? <p className="text-center italic text-[#999] mt-5">No job confirmations available.</p> :
                        filtered.map(renderCard)}
            </div>
        </main>
    );
}
