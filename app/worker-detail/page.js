"use client";

import React, { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Star, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD, SERVER_BASE } from '../../config';

const WorkerContent = () => {
    const router = useRouter();
    const searchParams = useSearchParams();
    const workerId = searchParams.get('workerId');
    
    const [worker, setWorker] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const fetchWorker = async () => {
            if (!workerId) return;
            setIsLoading(true);
            try {
                const token = localStorage.getItem('userToken');
                const response = await fetch(`${API_DASHBOARD}/GetWorkerDetail/${workerId}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (response.ok) {
                    const data = await response.json();
                    setWorker(data);
                } else {
                    NotificationHelper.showError("Failed to fetch worker details.");
                    router.back();
                }
            } catch (err) {
                console.error("Fetch Error:", err);
                NotificationHelper.showError("Could not connect to server.");
            } finally {
                setIsLoading(false);
            }
        };
        fetchWorker();
    }, [workerId, router]);

    if (isLoading) {
        return (
            <div className="min-h-screen bg-white flex justify-center items-center">
                <Loader2 className="animate-spin" size={40} color="#1E64D3" />
            </div>
        );
    }

    if (!worker) return null;

    return (
        <main className="min-h-screen bg-white relative pb-[100px] font-sans">
            {/* Profile Image Section */}
            <div className="relative w-full h-[400px] z-0">
                <img
                    src={worker.picture?.startsWith('/') ? `${SERVER_BASE}${worker.picture}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png'}
                    alt={worker.name}
                    className="w-full h-full object-cover"
                />
                <button 
                    onClick={() => router.back()}
                    className="absolute top-5 left-5 bg-white/80 p-2 rounded-full z-10 shadow-md transition-colors hover:bg-white"
                >
                    <ArrowLeft size={24} color="#000" />
                </button>

                <div className="absolute bottom-4 left-4 bg-white flex flex-row items-center px-3 py-1.5 rounded-[20px] shadow-sm border border-[#EEE]">
                    <div className={`w-2 h-2 rounded-full mr-2 ${worker.availability === "Available 24/7" ? 'bg-[#4CAF50]' : 'bg-[#FF5722]'}`} />
                    <span className={`text-[14px] font-bold ${worker.availability === "Available 24/7" ? 'text-[#4CAF50]' : 'text-[#FF5722]'}`}>
                        {worker.availability === "Available 24/7" ? "ACTIVE" : "BOOKED"}
                    </span>
                </div>
            </div>

            {/* Content Section */}
            <div className="px-5 mt-5 max-w-3xl mx-auto">
                {/* Name and Rating */}
                <div className="flex flex-row justify-between items-start">
                    <div className="flex-1">
                        <h1 className="text-[26px] font-bold text-[#000] leading-tight">{worker.name}</h1>
                        <div className="flex flex-row items-center mt-1 flex-wrap">
                            <span className="text-[18px] text-[#1E64D3] font-bold mr-[15px]">{worker.role}</span>
                            <span className="text-[18px] text-[#4CAF50] font-bold">{worker.gender?.toUpperCase()} • {worker.age} Y/O</span>
                        </div>
                    </div>
                    <div className="flex flex-col items-end">
                        <div className="flex flex-row items-center">
                            <Star size={20} color="#FFD700" fill="#FFD700" />
                            <span className="text-[22px] font-bold ml-1 text-[#000]">{worker.rating}</span>
                        </div>
                        <span className="text-[12px] text-[#999]">({worker.reviewCount} Reviews)</span>
                    </div>
                </div>

                {/* Statistics Row */}
                <div className="flex flex-row justify-between bg-white rounded-[15px] p-[15px] my-5 shadow-[0_4px_10px_rgba(0,0,0,0.1)] border border-[#F0F0F0]">
                    <div className="flex flex-1 flex-col items-center">
                        <span className="text-[10px] text-[#999] mb-1 font-semibold">EXPERIENCE</span>
                        <span className="text-[14px] font-bold text-[#000]">{worker.experiences?.length > 0 ? worker.experiences[0].period : "N/A"}</span>
                    </div>
                    <div className="w-[1px] bg-[#EEE] h-auto mx-2" />
                    <div className="flex flex-1 flex-col items-center">
                        <span className="text-[10px] text-[#999] mb-1 font-semibold">LOCATION</span>
                        <span className="text-[14px] font-bold text-[#000] text-center">{worker.location?.toUpperCase()}</span>
                    </div>
                    <div className="w-[1px] bg-[#EEE] h-auto mx-2" />
                    <div className="flex flex-1 flex-col items-center">
                        <span className="text-[10px] text-[#999] mb-1 font-semibold">SALARY</span>
                        <span className="text-[14px] font-bold text-[#000]">Rs.{worker.salary}</span>
                    </div>
                </div>

                {/* About Section */}
                <h2 className="text-[20px] font-bold text-[#000] mt-[15px] mb-[10px]">About</h2>
                <p className="text-[14px] text-[#666] leading-relaxed mb-[10px]">
                    {worker.bio}
                </p>

                {/* Primary Skills Section */}
                <h2 className="text-[20px] font-bold text-[#000] mt-[15px] mb-[10px]">Skills</h2>
                <div className="flex flex-row flex-wrap mb-[15px]">
                    {worker.primarySkills && worker.primarySkills.length > 0 ? (
                        worker.primarySkills.map((skill, index) => (
                            <div key={index} className="bg-[#E0E0E0] px-[15px] py-[8px] rounded-[20px] mr-[10px] mb-[10px]">
                                <span className="text-[12px] font-[900] text-[#333]">{skill.toUpperCase()}</span>
                            </div>
                        ))
                    ) : (
                        <p className="text-[13px] text-[#999] mb-[10px] italic">No primary skills listed.</p>
                    )}
                </div>

                {/* Part-Time Section */}
                <h2 className="text-[20px] font-bold text-[#000] mt-[15px] mb-[10px]">Part-Time</h2>
                {worker.partTimeSkills && worker.partTimeSkills.length > 0 ? (
                    worker.partTimeSkills.map((item, index) => (
                        <div key={index} className="mb-[15px]">
                            <h3 className="text-[14px] font-bold text-[#1E64D3] mb-[8px] mt-[5px]">{item.categoryName.toUpperCase()}</h3>
                            <div className="flex flex-row flex-wrap">
                                {item.skills.map((skill, sIndex) => (
                                    <div key={sIndex} className="bg-[#E0E0E0] px-[15px] py-[8px] rounded-[20px] mr-[10px] mb-[10px]">
                                        <span className="text-[12px] font-[900] text-[#333]">{skill.toUpperCase()}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))
                ) : (
                    <p className="text-[13px] text-[#999] mb-[10px] italic">Not Available yet any</p>
                )}

                {/* Work Experience Timeline */}
                <h2 className="text-[20px] font-bold text-[#000] mt-[15px] mb-[10px]">Work Experience</h2>
                {worker.experiences && worker.experiences.length > 0 ? (
                    worker.experiences.map((exp, index) => {
                        const isActive = index === 0;
                        return (
                            <div key={index} className="flex flex-row min-h-[80px]">
                                <div className="flex flex-col items-center mr-[10px]">
                                    <div className={`w-[12px] h-[12px] rounded-full ${isActive ? 'bg-[#1E64D3]' : 'bg-[#DDD]'}`} />
                                    <div className="flex-1 w-[2px] bg-[#EEE]" />
                                </div>
                                <div className="flex-1 pb-[20px]">
                                    <div className="flex flex-row justify-between items-start">
                                        <h4 className="font-bold text-[14px] text-[#000]">{exp.title}</h4>
                                        <span className="text-[10px] text-[#999] ml-2 whitespace-nowrap">{exp.period}</span>
                                    </div>
                                    <p className="text-[12px] text-[#888] italic mt-1">• {exp.details}</p>
                                </div>
                            </div>
                        );
                    })
                ) : (
                    <p className="text-[13px] text-[#999] mb-[10px] italic">No experience history available.</p>
                )}

                {/* Reviews Section */}
                <div className="flex flex-row justify-between items-center mt-[15px] mb-[10px]">
                    <h2 className="text-[20px] font-bold text-[#000]">Recent Reviews</h2>
                    <button 
                        onClick={() => router.push(`/rating-and-reviews?workerId=${worker.id}&initialRating=${worker.rating}&initialReviewCount=${worker.reviewCount}`)}
                        className="text-[#1E64D3] font-bold hover:underline"
                    >
                        View all
                    </button>
                </div>
                {worker.reviews && worker.reviews.length > 0 ? (
                    worker.reviews.map((rev, index) => (
                        <div key={index} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#EEE] shadow-sm">
                            <div className="flex flex-row justify-between items-center">
                                <h4 className="font-bold text-[14px] text-[#000]">{rev.reviewerName}</h4>
                                <div className="flex flex-row">
                                    {[1, 2, 3, 4, 5].map(i => (
                                        <Star key={i} size={14} color={i <= Math.round(rev.rating) ? "#FFD700" : "#EEE"} fill={i <= Math.round(rev.rating) ? "#FFD700" : "transparent"} />
                                    ))}
                                </div>
                            </div>
                            <p className="text-[12px] text-[#666] my-[5px]">{rev.date}</p>
                            <p className="text-[13px] text-[#444] italic">"{rev.comment}"</p>
                        </div>
                    ))
                ) : (
                    <p className="text-[13px] text-[#999] mb-[10px] italic">No reviews yet.</p>
                )}

                {/* Booking Procedure */}
                <h2 className="text-[20px] font-bold text-[#000] mt-[15px] mb-[10px]">Booking Procedure</h2>
                <div className="pl-[10px] mb-[20px]">
                    <p className="text-[13px] text-[#666] mb-[8px]">• Send a booking request with your preferred date.</p>
                    <p className="text-[13px] text-[#666] mb-[8px]">• Wait for the worker to accept (usually within 30m).</p>
                    <p className="text-[13px] text-[#666] mb-[8px]">• Confirm the location and start the service.</p>
                </div>

            </div>

            {/* Sticky Bottom Button */}
            <div className="fixed bottom-0 left-0 right-0 p-5 bg-white/90 backdrop-blur-sm border-t border-[#EEE] z-20 flex justify-center">
                <button
                    disabled={worker.hasActiveInterview}
                    onClick={() => router.push(`/interview-selection?workerId=${worker.id}&workerName=${encodeURIComponent(worker.name)}`)}
                    className={`w-full max-w-3xl h-[50px] rounded-[25px] flex items-center justify-center transition-all shadow-md ${worker.hasActiveInterview ? 'bg-[#B0BEC5] cursor-not-allowed' : 'bg-[#1E64D3] hover:bg-[#1550a6] active:opacity-90'}`}
                >
                    <span className="text-white text-[18px] font-bold">
                        {worker.activeInterviewStatus === "Finalized" || worker.activeInterviewStatus === "Hired" 
                            ? "Worker Hired" 
                            : worker.hasActiveInterview 
                                ? "Interview Request Pending" 
                                : "Call For Interview"}
                    </span>
                </button>
            </div>
        </main>
    );
};

export default function WorkerDetailScreen() {
    return (
        <Suspense fallback={<div className="min-h-screen bg-white flex justify-center items-center"><Loader2 className="animate-spin" size={40} color="#1E64D3" /></div>}>
            <WorkerContent />
        </Suspense>
    );
}