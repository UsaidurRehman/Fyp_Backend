"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Calendar, Clock, MapPin, ChevronRight, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { SERVER_BASE } from '../../config';

export default function InterviewSelectionScreen() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const workerId = searchParams.get('workerId');
    const workerName = searchParams.get('workerName');

    const [date, setDate] = useState(() => {
        const d = new Date();
        return d.toISOString().slice(0,10);
    });
    const [time, setTime] = useState('10:00');
    const [isLoading, setIsLoading] = useState(false);
    const [clientAddress, setClientAddress] = useState('');

    useEffect(() => {
        const addr = localStorage.getItem('userAddress');
        if (addr) setClientAddress(addr);
    }, []);

    const handleConfirmInterview = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const combined = new Date(`${date}T${time}:00`);
            const offset = combined.getTimezoneOffset() * 60000;
            const localISO = new Date(combined - offset).toISOString().slice(0,19);
            const response = await fetch(`${SERVER_BASE}/api/Dashboard/BookInterview`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ WorkerId: workerId, InterviewDate: localISO, Address: clientAddress, Status: 'Pending' })
            });
            if (response.ok) {
                NotificationHelper.showSuccess(`Interview request sent to ${workerName}!`);
                router.push('/user-dashboard');
            } else {
                const err = await response.json();
                NotificationHelper.showError(err.message || "Failed to book interview.");
            }
        } catch { NotificationHelper.showError("Could not connect to server."); }
        finally { setIsLoading(false); }
    };

    const displayDate = date ? new Date(date + 'T00:00:00').toDateString() : '';
    const displayTime = time ? (() => {
        const [h, m] = time.split(':').map(Number);
        const ampm = h >= 12 ? 'PM' : 'AM';
        return `${h % 12 || 12}:${m.toString().padStart(2,'0')} ${ampm}`;
    })() : '';

    return (
        <main className="max-w-md mx-auto bg-[#F8FBFF] min-h-screen font-sans relative pb-[100px]">
            <div className="flex items-center p-5 pt-[10px]">
                <button onClick={() => router.back()} className="p-2 bg-white rounded-full shadow-md">
                    <ArrowLeft size={24} color="#555" />
                </button>
                <h1 className="text-[22px] font-bold ml-[15px] text-[#000]">Select Date & Time</h1>
            </div>

            <div className="px-5">
                <p className="text-[16px] text-[#555] mb-5">When do you need to interview {workerName}?</p>

                <div className="mb-[25px]">
                    <p className="text-[16px] font-bold text-[#000] mb-[10px] ml-[5px]">Interview Date</p>
                    <div className="flex items-center bg-white p-[15px] rounded-[15px] shadow-sm border border-[#EEE]">
                        <div className="w-[45px] h-[45px] rounded-full bg-[#E3F2FD] flex items-center justify-center mr-[15px]">
                            <Calendar size={24} color="#1E64D3" />
                        </div>
                        <div className="flex-1">
                            <p className="text-[12px] text-[#888] mb-[2px]">Tap to choose date</p>
                            <input type="date" value={date} onChange={e => setDate(e.target.value)}
                                className="text-[16px] font-bold text-[#333] bg-transparent outline-none w-full" />
                        </div>
                        <ChevronRight size={24} color="#888" />
                    </div>
                </div>

                <div className="mb-[25px]">
                    <p className="text-[16px] font-bold text-[#000] mb-[10px] ml-[5px]">Interview Time</p>
                    <div className="flex items-center bg-white p-[15px] rounded-[15px] shadow-sm border border-[#EEE]">
                        <div className="w-[45px] h-[45px] rounded-full bg-[#F3EDF7] flex items-center justify-center mr-[15px]">
                            <Clock size={24} color="#6750A4" />
                        </div>
                        <div className="flex-1">
                            <p className="text-[12px] text-[#888] mb-[2px]">Tap to choose time</p>
                            <input type="time" value={time} onChange={e => setTime(e.target.value)}
                                className="text-[16px] font-bold text-[#333] bg-transparent outline-none w-full" />
                        </div>
                        <ChevronRight size={24} color="#888" />
                    </div>
                </div>

                <div className="mb-[25px]">
                    <p className="text-[16px] font-bold text-[#000] mb-[10px] ml-[5px]">Interview Location</p>
                    <div className="flex items-center bg-white p-[15px] rounded-[15px] shadow-sm border border-[#EEE]">
                        <div className="w-[45px] h-[45px] rounded-full bg-[#FFF3E0] flex items-center justify-center mr-[15px]">
                            <MapPin size={24} color="#E65100" />
                        </div>
                        <div className="flex-1">
                            <p className="text-[12px] text-[#888] mb-[2px]">Your Address</p>
                            <p className="text-[16px] font-bold text-[#333]">{clientAddress || 'Loading address...'}</p>
                        </div>
                    </div>
                </div>
            </div>

            <div className="fixed bottom-0 left-1/2 -translate-x-1/2 w-full max-w-md p-5 bg-[#F8FBFF]">
                <button onClick={handleConfirmInterview} disabled={isLoading}
                    className="w-full h-[55px] rounded-[28px] bg-[#1E64D3] flex items-center justify-center shadow-md disabled:opacity-60">
                    {isLoading ? <Loader2 className="animate-spin text-white" size={24} /> :
                        <span className="text-white text-[18px] font-bold">Confirm Booking</span>}
                </button>
            </div>
        </main>
    );
}
