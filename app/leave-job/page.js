"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, CalendarClock, Loader2 } from 'lucide-react'; 
import { API_DASHBOARD } from '../../config';
import NotificationHelper from '../Notification/NotificationHelper';

const LeaveJobScreen = () => {
    const router = useRouter();
    const [reason, setReason] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [jobData, setJobData] = useState(null);
    const [lastWorkingDay, setLastWorkingDay] = useState(
        new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
    );

    useEffect(() => {
        fetchActiveJob();
    }, []);

    const fetchActiveJob = async () => {
        try {
            const workerId = localStorage.getItem('workerId');
            const token = localStorage.getItem('userToken');
            
            if (!workerId) {
                NotificationHelper.showError("Worker identity not found. Please re-login.");
                router.back();
                return;
            }

            const response = await fetch(`${API_DASHBOARD}/GetActiveJob/${workerId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const data = await response.json();
                setJobData(data);
            } else {
                setJobData(null);
            }
        } catch (error) {
            console.error("Connection error:", error);
            NotificationHelper.showError("Connection error. Check API configuration.");
        } finally {
            setIsLoading(false);
        }
    };

    const handleResign = async () => {
        if (!reason.trim()) {
            NotificationHelper.showError("Please provide a reason for resignation.");
            return;
        }

        if (!jobData || !jobData.interviewId) {
            NotificationHelper.showError("Process error: Job data missing.");
            return;
        }

        setIsSubmitting(true);
        try {
            const token = localStorage.getItem('userToken');
            
            const payload = {
                InterviewId: jobData.interviewId,
                ResignationReason: reason,
                LastWorkingDate: lastWorkingDay
            };

            const response = await fetch(`${API_DASHBOARD}/SubmitResignation`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json', 
                    'Authorization': `Bearer ${token}` 
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                NotificationHelper.showSuccess("Resignation submitted successfully.");
                router.push('/worker-dashboard');
            } else {
                let errorMsg = "Failed to submit resignation.";
                try {
                    const err = await response.json();
                    errorMsg = err.message || errorMsg;
                } catch (e) {
                    console.log("Server Error Response:", await response.text());
                }
                NotificationHelper.showError(errorMsg);
            }
        } catch (error) {
            console.error("Submission error:", error);
            NotificationHelper.showError("Network error. Please try again.");
        } finally {
            setIsSubmitting(false);
        }
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center h-screen bg-white">
                <Loader2 className="animate-spin text-[#1E64D3]" size={48} />
            </div>
        );
    }

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen relative overflow-hidden font-sans">
            <div className="absolute top-[-50px] left-[-50px] w-[200px] h-[200px] rounded-full bg-[#E3F2FD] -z-10" />

            <div className="p-[25px]">
                <div className="flex items-center justify-between mb-[30px]">
                    <button onClick={() => router.back()} className="p-[5px] active:scale-95 transition-transform">
                        <ArrowLeft size={24} color="#333" />
                    </button>
                    <h1 className="text-[22px] font-bold text-[#333]">Resign from Job</h1>
                    <img src="/images/logo.png" className="w-[40px] h-[40px]" alt="Logo" />
                </div>

                {jobData ? (
                    <>
                        <div className="bg-white rounded-[15px] p-[20px] mb-[25px] border border-[#EEE] shadow-[0_10px_20px_rgba(0,0,0,0.05)]">
                            <div>
                                <h2 className="text-[20px] font-bold text-[#333]">{jobData.employerName}</h2>
                                <p className="text-[14px] text-[#666] mt-[4px]">{jobData.employerAddress}</p>
                            </div>
                            <div className="bg-[#E3F2FD] inline-block px-[10px] py-[5px] rounded-[8px] mt-[15px]">
                                <span className="text-[#1E64D3] text-[12px] font-bold">Standard 1-Week Notice</span>
                            </div>
                        </div>

                        <div className="mb-[25px]">
                            <label className="block text-[16px] font-bold text-[#000] mb-[12px]">Proposed Last Working Day</label>
                            <div className="flex flex-row justify-between items-center border border-[#DDD] rounded-[12px] px-[15px] h-[55px] bg-[#F9F9F9]">
                                <input 
                                    type="date" 
                                    className="flex-1 bg-transparent text-[16px] text-[#333] outline-none"
                                    value={lastWorkingDay}
                                    min={new Date().toISOString().split('T')[0]}
                                    onChange={(e) => setLastWorkingDay(e.target.value)}
                                />
                                <CalendarClock size={24} color="#1E64D3" />
                            </div>
                        </div>

                        <div className="mb-[25px]">
                            <label className="block text-[16px] font-bold text-[#000] mb-[12px]">Reason for Leaving</label>
                            <textarea
                                className="w-full border border-[#DDD] rounded-[12px] p-[15px] min-h-[120px] bg-[#F9F9F9] text-[16px] resize-none outline-none focus:border-[#1E64D3]"
                                placeholder="Please explain why you are leaving..."
                                value={reason}
                                onChange={(e) => setReason(e.target.value)}
                            />
                        </div>

                        <button 
                            onClick={handleResign} 
                            disabled={isSubmitting}
                            className="w-full bg-[#E91E63] h-[55px] rounded-[15px] flex justify-center items-center mt-[10px] shadow-sm active:scale-95 transition-transform disabled:opacity-70 disabled:scale-100"
                        >
                            {isSubmitting ? (
                                <Loader2 className="animate-spin text-white" size={24} />
                            ) : (
                                <span className="text-white text-[18px] font-bold">Submit Resignation Notice</span>
                            )}
                        </button>
                    </>
                ) : (
                    <div className="flex flex-col items-center justify-center mt-20 text-center">
                        <div className="w-[80px] h-[80px] rounded-full bg-[#FFEBEE] flex items-center justify-center mb-5">
                            <CalendarClock size={40} className="text-[#E91E63]" />
                        </div>
                        <h2 className="text-[22px] font-bold text-[#333] mb-2">No Active Job</h2>
                        <p className="text-[16px] text-[#666] leading-relaxed px-5">
                            You currently do not have an active job record. You must be officially hired before you can submit a resignation.
                        </p>
                    </div>
                )}

                <button 
                    onClick={() => router.back()} 
                    className="mt-[20px] w-full flex justify-center items-center p-[10px] active:scale-95 transition-transform"
                >
                    <span className="text-[#666] text-[16px] font-medium">Go Back</span>
                </button>
            </div>
        </main>
    );
};

export default LeaveJobScreen;