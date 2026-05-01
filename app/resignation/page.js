"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Star, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD, SERVER_BASE } from '../../config';

export default function ResignationScreen() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const resignationId = searchParams.get('resignationId');

    const [isLoading, setIsLoading] = useState(true);
    const [data, setData] = useState(null);
    const [rating, setRating] = useState(3);
    const [remarks, setRemarks] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => { fetchDetail(); }, []);

    const fetchDetail = async () => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetResignationDetail/${resignationId}`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setData(await res.json());
            else { NotificationHelper.showError("Failed to load details."); router.back(); }
        } catch { NotificationHelper.showError("Network error."); }
        finally { setIsLoading(false); }
    };

    const handleConfirm = async () => {
        if (!remarks.trim()) { NotificationHelper.showError("Please enter some remarks before confirming."); return; }
        setIsSubmitting(true);
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/ConfirmResignation`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ InterviewId: data.interviewId, Rating: rating, Comment: remarks })
            });
            if (res.ok) { NotificationHelper.showSuccess("Resignation successfully confirmed."); router.push('/user-dashboard'); }
            else { const err = await res.json(); NotificationHelper.showError(err.message || "Failed to confirm."); }
        } catch { NotificationHelper.showError("Server error."); }
        finally { setIsSubmitting(false); }
    };

    if (isLoading) return <div className="flex justify-center items-center h-screen"><Loader2 className="animate-spin text-[#1E64D3]" size={48} /></div>;
    if (!data) return null;

    const avatarUrl = data.workerAvatar?.startsWith('/') ? `${SERVER_BASE}${data.workerAvatar}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';
    const progress = Math.min(Math.max(data.progress || 0, 0), 1);

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans">
            <div className="flex items-center justify-between px-5 py-[15px]">
                <button onClick={() => router.back()} className="w-[35px] h-[35px] rounded-full bg-[#F0F0F0] flex items-center justify-center">
                    <ArrowLeft size={20} color="#666" />
                </button>
                <h1 className="text-[28px] font-bold text-[#000]">Resignation</h1>
                <img src="/images/logo.png" className="w-[40px] h-[40px]" alt="Logo" />
            </div>

            <div className="px-5 pb-10">
                <div className="flex items-center mb-[25px]">
                    <img src={avatarUrl} className="w-[80px] h-[80px] rounded-full bg-[#EEE] object-cover" alt={data.workerName} />
                    <div className="ml-[15px]">
                        <p className="text-[24px] font-bold text-[#000]">{data.workerName}</p>
                        <p className="text-[18px] font-bold text-[#5B4CF2]">{data.workerRole}</p>
                    </div>
                </div>

                <div className="bg-white rounded-[15px] border border-[#DDD] overflow-hidden mb-5 shadow-sm">
                    <div className="bg-[#6289F4] p-[15px]">
                        <p className="text-white text-[18px] font-medium">Official Notice Period</p>
                        <p className="text-white text-[20px] font-bold">{data.totalNoticeDays} Days Total</p>
                    </div>
                    <div className="p-[15px]">
                        <p className="text-[18px] font-bold text-[#000]">Notice Period Status</p>
                        <p className="text-[16px] text-[#666] mt-[5px]">Remaining Days: <span className="text-[#5B4CF2] font-bold">{data.remainingDays}</span></p>
                        <div className="h-[6px] bg-[#EEE] rounded-full mt-[15px] overflow-hidden">
                            <div className="h-full bg-[#6289F4] rounded-full" style={{ width: `${progress * 100}%` }} />
                        </div>
                    </div>
                </div>

                <div className="mb-5">
                    <p className="text-[22px] font-bold mb-[10px]">Last Working Day</p>
                    <div className="border border-[#CCC] rounded-[10px] p-3">
                        <p className="text-[16px] text-[#666]">{data.lastWorkingDate}</p>
                    </div>
                </div>

                <div className="mb-5">
                    <p className="text-[22px] font-bold mb-[10px]">Reason for Leaving</p>
                    <div className="border border-[#CCC] rounded-[10px] p-3 min-h-[100px]">
                        <p className="text-[16px] text-[#666]">{data.reason}</p>
                    </div>
                </div>

                <div className="border border-[#CCC] rounded-[15px] p-[10px] mb-[30px]">
                    <div className="flex justify-end mb-[5px]">
                        {[1,2,3,4,5].map(star => (
                            <button key={star} onClick={() => setRating(star)}>
                                <Star size={20} color={star <= rating ? "#FFD700" : "#666"} fill={star <= rating ? "#FFD700" : "none"} />
                            </button>
                        ))}
                    </div>
                    <div className="flex items-center border border-[#CCC] rounded-[20px] px-[15px] h-[45px]">
                        <input className="flex-1 text-[14px] outline-none" placeholder="Enter your remarks here" value={remarks} onChange={e => setRemarks(e.target.value)} />
                        <button onClick={handleConfirm} className="bg-[#1E64D3] px-5 h-[30px] rounded-[15px] shadow-md">
                            <span className="text-white text-[13px] font-bold">Submit</span>
                        </button>
                    </div>
                </div>

                <button onClick={handleConfirm} disabled={isSubmitting}
                    className="w-full h-[55px] rounded-[10px] bg-[#008000] flex items-center justify-center shadow-md disabled:opacity-70">
                    {isSubmitting ? <Loader2 className="animate-spin text-white" size={24} /> :
                        <span className="text-white text-[20px] font-bold">Confirm Resignation</span>}
                </button>
            </div>
        </main>
    );
}
