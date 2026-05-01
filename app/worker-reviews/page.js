"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Star, Calendar, MessageSquare, Loader2 } from 'lucide-react';
import { API_DASHBOARD } from '../../config';

export default function WorkerReviewsScreen() {
    const router = useRouter();
    const [reviews, setReviews] = useState([]);
    const [averageRating, setAverageRating] = useState("0.0");
    const [reviewCount, setReviewCount] = useState(0);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        fetchReviews();
    }, []);

    const fetchReviews = async () => {
        setIsLoading(true);
        try {
            const workerId = localStorage.getItem('workerId');
            const token = localStorage.getItem('userToken');
            const response = await fetch(`${API_DASHBOARD}/GetWorkerReviews/${workerId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const data = await response.json();
                setReviews(data.reviews || []);
                setAverageRating(data.averageRating?.toString() || "0.0");
                setReviewCount(data.reviewCount || 0);
            } else {
                console.error("Failed to fetch reviews");
            }
        } catch (error) {
            console.error("Error fetching reviews:", error);
        } finally {
            setIsLoading(false);
        }
    };

    const renderStars = (rating, size = 18) => {
        return (
            <div className="flex">
                {[1, 2, 3, 4, 5].map((i) => (
                    <Star
                        key={i}
                        size={size}
                        className={i <= Math.round(parseFloat(rating)) ? "text-[#FFD700] fill-[#FFD700]" : "text-[#E0E0E0]"}
                    />
                ))}
            </div>
        );
    };

    if (isLoading) {
        return (
            <div className="flex flex-col justify-center items-center h-screen bg-white">
                <Loader2 className="animate-spin text-[#1E75EB]" size={48} />
                <p className="mt-4 text-[16px] text-[#1E75EB] font-medium">Loading your reviews...</p>
            </div>
        );
    }

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans px-5 py-8">
            {/* Overall Rating Header Card */}
            <div className="bg-white rounded-[25px] p-6 mb-8 shadow-xl border border-[#E0E0E0] animate-in fade-in duration-500">
                <p className="text-[18px] text-[#333] mb-3">Your Overall Rating</p>
                <div className="flex items-center">
                    <span className="text-[48px] font-bold text-[#1E4A84] mr-4">{averageRating}</span>
                    <div>
                        {renderStars(averageRating, 30)}
                        <p className="text-[16px] text-[#888] mt-1">Based on {reviewCount} Reviews</p>
                    </div>
                </div>
            </div>

            {/* Reviews List */}
            <div className="space-y-4">
                {reviews.length > 0 ? (
                    reviews.map((item, index) => (
                        <div 
                            key={item.id || index} 
                            className="bg-white rounded-[15px] p-4 border border-[#E0E0E0] shadow-sm hover:shadow-md transition-shadow animate-in slide-in-from-bottom-2 duration-300"
                            style={{ animationDelay: `${index * 100}ms` }}
                        >
                            <div className="flex justify-between items-start mb-3">
                                <div>
                                    <p className="text-[16px] font-bold text-black">{item.name}</p>
                                    <div className="flex items-center text-[#888] mt-1">
                                        <Calendar size={14} className="mr-1" />
                                        <span className="text-[12px]">{item.date}</span>
                                    </div>
                                </div>
                                {renderStars(item.rating)}
                            </div>
                            <p className="text-[13px] text-[#555] italic leading-relaxed bg-[#F9F9F9] p-3 rounded-lg border-l-4 border-[#1E75EB]">
                                "{item.comment}"
                            </p>
                        </div>
                    ))
                ) : (
                    <div className="flex flex-col items-center justify-center py-20 text-center opacity-50">
                        <MessageSquare size={60} className="text-[#DDD] mb-4" />
                        <p className="text-[16px] text-[#999] italic">You don't have any reviews yet.</p>
                    </div>
                )}
            </div>

            {/* Back Button */}
            <button
                onClick={() => router.back()}
                className="w-full bg-[#1E75EB] text-white rounded-full h-14 font-bold text-[18px] shadow-lg active:scale-[0.98] transition-transform mt-8 mb-4"
            >
                Back to Dashboard
            </button>
        </main>
    );
}
