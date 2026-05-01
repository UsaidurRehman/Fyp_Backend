"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, CalendarClock, ChevronRight, FileText, Loader2 } from 'lucide-react';
import { API_DASHBOARD } from '../../config';

export default function ResignationsScreen() {
    const router = useRouter();
    const [resignations, setResignations] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isRefreshing, setIsRefreshing] = useState(false);

    useEffect(() => { fetchResignations(); }, []);

    const fetchResignations = async () => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${API_DASHBOARD}/GetClientResignations`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) setResignations(await res.json());
        } catch (e) { console.error(e); }
        finally { setIsLoading(false); setIsRefreshing(false); }
    };

    return (
        <main className="max-w-md mx-auto bg-[#F8F9FA] min-h-screen font-sans">
            <div className="flex items-center justify-between px-5 py-[15px] bg-white shadow-sm">
                <button onClick={() => router.back()} className="p-[5px]"><ArrowLeft size={24} color="#333" /></button>
                <h1 className="text-[20px] font-bold text-[#333]">Worker Resignations</h1>
                <div className="w-[40px]" />
            </div>

            {isLoading ? (
                <div className="flex justify-center items-center mt-20"><Loader2 className="animate-spin text-[#1E64D3]" size={48} /></div>
            ) : resignations.length === 0 ? (
                <div className="flex flex-col items-center mt-[100px] px-5">
                    <FileText size={60} color="#DDD" />
                    <p className="text-[#999] text-[16px] mt-[15px] italic">No resignation notices received yet.</p>
                </div>
            ) : (
                <div className="p-5">
                    {resignations.map(item => (
                        <div key={item.resignationId} className="bg-white rounded-[15px] p-4 mb-5 border border-[#EEE] shadow-md">
                            <div className="flex justify-between items-start border-b border-[#F0F0F0] pb-[10px] mb-3">
                                <div>
                                    <p className="text-[18px] font-bold text-[#1E64D3]">{item.workerName}</p>
                                    <p className="text-[13px] text-[#666] mt-[2px]">{item.workerRole}</p>
                                </div>
                                <div className="bg-[#F0F4FF] px-[10px] py-1 rounded-[10px]">
                                    <span className="text-[11px] text-[#1E64D3] font-semibold">{item.submittedDate}</span>
                                </div>
                            </div>
                            <div className="mb-[15px]">
                                <p className="text-[12px] text-[#888] uppercase tracking-[0.5px] mb-1">Reason for Leaving:</p>
                                <p className="text-[14px] text-[#444] italic leading-5 line-clamp-2">{item.reason}</p>
                            </div>
                            <div className="flex justify-between items-center pt-3 border-t border-[#F0F0F0]">
                                <div className="flex items-center">
                                    <CalendarClock size={16} color="#E91E63" />
                                    <span className="text-[13px] text-[#333] font-semibold ml-[6px]">Last Day: {item.lastWorkingDate}</span>
                                </div>
                                <button onClick={() => router.push(`/resignation?resignationId=${item.resignationId}`)}
                                    className="flex items-center bg-[#1E64D3] px-[15px] py-2 rounded-[20px] shadow-sm">
                                    <span className="text-white font-bold text-[13px] mr-1">View Detail</span>
                                    <ChevronRight size={18} color="#FFF" />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </main>
    );
}
