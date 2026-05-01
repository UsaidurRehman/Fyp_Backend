"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, CheckCircle2, AlertOctagon, Calendar, Quote, Info, Loader2 } from 'lucide-react';
import { SERVER_BASE } from '../../config';

export default function WorkerTerminationStatusScreen() {
    const router = useRouter();
    const [termination, setTermination] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchTerminationStatus();
    }, []);

    const fetchTerminationStatus = async () => {
        try {
            const workerId = localStorage.getItem('workerId');
            const token = localStorage.getItem('userToken');
            
            const response = await fetch(`${SERVER_BASE}/api/Dashboard/GetLatestTermination/${workerId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const data = await response.json();
                setTermination(data);
            }
        } catch (error) {
            console.error("Error fetching termination status:", error);
        } finally {
            setIsLoading(false);
        }
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center h-screen bg-[#F8F9FB]">
                <Loader2 className="animate-spin text-[#1E64D3]" size={48} />
            </div>
        );
    }

    return (
        <main className="max-w-md mx-auto bg-[#F8F9FB] min-h-screen font-sans">
            {/* Header */}
            <div className="flex items-center justify-center p-5 bg-white shadow-sm sticky top-0 z-10">
                <button onClick={() => router.back()} className="absolute left-5 p-1">
                    <ArrowLeft size={24} className="text-[#333]" />
                </button>
                <h1 className="text-[20px] font-bold text-[#333]">Termination Status</h1>
                <img src="/images/logo.png" className="absolute right-5 w-[35px] h-[35px]" alt="Logo" />
            </div>

            <div className="p-5">
                {!termination ? (
                    <div className="flex flex-col items-center mt-20 px-5 text-center">
                        <div className="w-[140px] h-[140px] rounded-full bg-[#E8F5E9] flex items-center justify-center mb-8">
                            <CheckCircle2 size={80} className="text-[#4CAF50]" />
                        </div>
                        <h2 className="text-[28px] font-bold text-[#2E7D32] mb-3">Good News!</h2>
                        <p className="text-[16px] text-[#666] leading-relaxed mb-10">
                            You currently have no recorded terminations. Your professional record is clean.
                        </p>
                        <button 
                            onClick={() => router.push('/worker-dashboard')}
                            className="bg-[#1E64D3] text-white font-bold text-[16px] px-10 py-4 rounded-full shadow-lg active:scale-95 transition-transform"
                        >
                            Return to Dashboard
                        </button>
                    </div>
                ) : (
                    <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                        {/* Status Card */}
                        <div className="bg-white rounded-[20px] p-5 shadow-md mb-6 border-l-[8px] border-[#E63917]">
                            <div className="flex items-center">
                                <div className="w-[50px] h-[50px] rounded-full bg-[#FFEBEE] flex items-center justify-center mr-4">
                                    <AlertOctagon size={30} className="text-[#E63917]" />
                                </div>
                                <div>
                                    <p className="text-[14px] text-[#666] font-semibold">Contract Status</p>
                                    <p className="text-[24px] font-bold text-[#E63917]">Terminated</p>
                                </div>
                            </div>
                            <div className="h-[1px] bg-[#EEE] my-4" />
                            <div className="flex items-center text-[#555] font-medium">
                                <Calendar size={20} className="mr-3 text-[#666]" />
                                <span>
                                    Terminated on: {new Date(termination.terminatedDate).toLocaleDateString('en-US', {
                                        month: 'long', day: 'numeric', year: 'numeric'
                                    })}
                                </span>
                            </div>
                        </div>

                        {/* Reason Section */}
                        <h3 className="text-[18px] font-bold text-[#333] mb-4 ml-1">Reason for Termination</h3>
                        <div className="bg-[#E3F2FD] rounded-[20px] p-6 mb-6 border border-[#BBDEFB] relative">
                            <Quote size={24} className="text-[#1E64D3] mb-2 opacity-50" />
                            <p className="text-[16px] text-[#1E64D3] leading-relaxed italic text-center font-medium">
                                {termination.terminatedReason || "No specific reason provided by the client."}
                            </p>
                            <Quote size={24} className="text-[#1E64D3] mt-2 opacity-50 rotate-180 ml-auto" />
                        </div>

                        {/* Employer Info */}
                        <h3 className="text-[18px] font-bold text-[#333] mb-4 ml-1">Employer Details</h3>
                        <div className="flex items-center bg-white p-4 rounded-[20px] shadow-sm mb-8">
                            <img 
                                src={termination.clientPicture 
                                    ? `${SERVER_BASE}${termination.clientPicture}` 
                                    : 'https://cdn-icons-png.flaticon.com/512/3135/3135768.png'} 
                                className="w-15 h-15 rounded-full border-2 border-[#EEE] object-cover"
                                alt={termination.clientName}
                            />
                            <div className="ml-4">
                                <p className="text-[18px] font-bold text-black">{termination.clientName}</p>
                                <p className="text-[14px] text-[#999]">Client / Employer</p>
                            </div>
                        </div>

                        {/* Feedback Box */}
                        <div className="flex bg-white rounded-[15px] p-4 border border-[#EEE] mb-8 items-start">
                            <Info size={22} className="text-[#1E64D3] mt-1 shrink-0" />
                            <p className="ml-3 text-[13px] text-[#666] leading-relaxed">
                                Termination is a part of professional life. Don't be discouraged! 
                                Your profile is now visible for other potential employers.
                            </p>
                        </div>

                        <button 
                            onClick={() => router.push('/worker-dashboard')} // Or relevant earnings screen
                            className="w-full bg-[#1E64D3] text-white font-bold text-[16px] h-14 rounded-[15px] shadow-lg active:scale-[0.98] transition-transform mb-5"
                        >
                            View My Dashboard
                        </button>
                    </div>
                )}
            </div>
        </main>
    );
}
