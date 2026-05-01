"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Star, CheckSquare, Square, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { SERVER_BASE } from '../../config';

export default function TerminateContractScreen() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const workerId = searchParams.get('workerId');
    const interviewId = searchParams.get('interviewId');

    const [reason, setReason] = useState('');
    const [remarks, setRemarks] = useState('');
    const [rating, setRating] = useState(0);
    const [isConfirmed, setIsConfirmed] = useState(false);
    const [worker, setWorker] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => { fetchWorkerDetails(); }, []);

    const fetchWorkerDetails = async () => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${SERVER_BASE}/api/Dashboard/GetWorkerDetail/${workerId}`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setWorker(await res.json());
            else NotificationHelper.showError("Failed to load worker details");
        } catch { NotificationHelper.showError("Network error loading worker"); }
        finally { setIsLoading(false); }
    };

    const handleTerminate = async () => {
        if (!reason.trim()) { NotificationHelper.showError("Please enter a termination reason"); return; }
        if (rating === 0) { NotificationHelper.showError("Please provide a rating for the worker"); return; }
        if (!isConfirmed) { NotificationHelper.showError("Please confirm termination by checking the box"); return; }
        setIsSubmitting(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${SERVER_BASE}/api/Dashboard/TerminateContract`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ InterviewId: interviewId, Reason: reason, Remarks: remarks, Rating: rating })
            });
            if (res.ok) { NotificationHelper.showSuccess("Contract terminated successfully"); router.push('/user-dashboard'); }
            else { const e = await res.json(); NotificationHelper.showError(e.message || "Termination failed"); }
        } catch { NotificationHelper.showError("Network error occurred"); }
        finally { setIsSubmitting(false); }
    };

    if (isLoading) return <div className="flex justify-center items-center h-screen"><Loader2 className="animate-spin text-[#1E64D3]" size={48} /></div>;
    if (!worker) return null;

    const avatarUrl = worker.picture ? `${SERVER_BASE}${worker.picture}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';
    const canSubmit = isConfirmed && !isSubmitting;

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans relative overflow-hidden">
            <div className="absolute top-[-50px] left-[-50px] w-[200px] h-[200px] rounded-full bg-[#E3F2FD] -z-10" />
            <div className="overflow-y-auto p-5">
                <div className="flex items-center justify-center mb-[25px] relative">
                    <button onClick={() => router.back()} className="absolute left-0 w-[40px] h-[40px] rounded-full bg-[#E3F2FD] flex items-center justify-center">
                        <ArrowLeft size={24} color="#666" />
                    </button>
                    <h1 className="text-[24px] font-bold text-[#000]">Terminate Contract</h1>
                    <img src="/images/logo.png" className="absolute right-0 w-[40px] h-[40px]" alt="Logo" />
                </div>

                <div className="flex items-center bg-white p-[15px] rounded-[20px] border border-[#EEE] mb-[25px] shadow-md">
                    <img src={avatarUrl} className="w-[70px] h-[70px] rounded-full border-2 border-[#1E64D3] object-cover" alt={worker.name} />
                    <div className="ml-[15px]">
                        <p className="text-[22px] font-bold text-[#000]">{worker.name}</p>
                        <p className="text-[16px] text-[#666] mt-[2px]">{worker.categoryName || 'Hired Worker'}</p>
                    </div>
                </div>

                <div className="mb-[25px]">
                    <p className="text-[16px] font-bold text-[#333]">Termination Reason</p>
                    <textarea value={reason} onChange={e => setReason(e.target.value)}
                        className="w-full border border-[#DDD] rounded-[15px] p-[15px] text-[16px] bg-[#F9F9F9] min-h-[100px] resize-none outline-none mt-[10px]"
                        placeholder="Why are you ending this contract?" />
                </div>

                <div className="bg-white rounded-[20px] p-5 border border-[#EEE] mb-[30px] shadow-sm">
                    <p className="text-[16px] font-bold text-[#333] mb-[15px]">Rate their service</p>
                    <div className="flex justify-center gap-2 my-[10px]">
                        {[1,2,3,4,5].map(star => (
                            <button key={star} onClick={() => setRating(star)}>
                                <Star size={36} color={star <= rating ? "#FFD700" : "#CCC"} fill={star <= rating ? "#FFD700" : "none"} />
                            </button>
                        ))}
                    </div>
                    <textarea value={remarks} onChange={e => setRemarks(e.target.value)}
                        className="w-full border border-[#DDD] rounded-[15px] p-[15px] text-[16px] bg-[#F9F9F9] h-[80px] resize-none outline-none mt-[15px]"
                        placeholder="Add additional remarks/feedback" />
                </div>

                <button onClick={() => setIsConfirmed(!isConfirmed)} className="flex items-center mb-[35px] px-[5px] w-full">
                    {isConfirmed ? <CheckSquare size={26} color="#1E64D3" /> : <Square size={26} color="#666" />}
                    <p className="text-[13px] text-[#555] ml-3 flex-1 leading-[18px]">
                        I confirm that I want to terminate this contract and have cleared all dues as per the agreement.
                    </p>
                </button>

                <button onClick={handleTerminate} disabled={!canSubmit}
                    className={`w-full h-[60px] rounded-[15px] flex items-center justify-center shadow-md transition-all ${canSubmit ? 'bg-[#E63917]' : 'bg-[#FFA4A4]'}`}>
                    {isSubmitting ? <Loader2 className="animate-spin text-white" size={24} /> :
                        <span className="text-white text-[18px] font-bold">Confirm Termination</span>}
                </button>

                <button onClick={() => router.back()} className="mt-5 w-full py-[10px] flex justify-center">
                    <span className="text-[16px] font-semibold text-[#666]">Cancel</span>
                </button>
            </div>
        </main>
    );
}
