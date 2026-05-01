"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Star, CalendarRange, MessageSquareDashed, Loader2 } from 'lucide-react';
import { API_DASHBOARD } from '../../config';

function ReviewsPage({ titleText, backLabel }) {
    const router = useRouter();
    const searchParams = useSearchParams();
    const workerId = searchParams.get('workerId');
    const initialRating = searchParams.get('initialRating') || '0.0';
    const initialReviewCount = parseInt(searchParams.get('initialReviewCount') || '0');

    const [reviews, setReviews] = useState([]);
    const [averageRating, setAverageRating] = useState(initialRating);
    const [reviewCount, setReviewCount] = useState(initialReviewCount);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const fetchReviews = async () => {
            setIsLoading(true);
            try {
                const token = localStorage.getItem('userToken');
                const res = await fetch(`${API_DASHBOARD}/GetWorkerReviews/${workerId}`, { headers: { 'Authorization': `Bearer ${token}` } });
                if (res.ok) {
                    const data = await res.json();
                    setReviews(data.reviews || []);
                    setAverageRating(data.averageRating?.toString() || '0.0');
                    setReviewCount(data.reviewCount || 0);
                }
            } catch (e) { console.error(e); }
            finally { setIsLoading(false); }
        };
        if (workerId) fetchReviews();
    }, [workerId]);

    const renderStars = (rating, size = 18) =>
        [1,2,3,4,5].map(i => <Star key={i} size={size} color={i <= Math.round(rating) ? "#FFD700" : "#E0E0E0"} fill={i <= Math.round(rating) ? "#FFD700" : "none"} />);

    if (isLoading) return (
        <div className="flex flex-col justify-center items-center h-screen">
            <Loader2 className="animate-spin text-[#1E75EB]" size={48} />
            <p className="mt-[10px] text-[16px] text-[#1E75EB]">Loading reviews...</p>
        </div>
    );

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans">
            <div className="p-5">
                <div className="bg-white rounded-[25px] p-5 mb-[25px] border border-[#E0E0E0] shadow-md">
                    <p className="text-[18px] text-[#333] mb-[10px]">{titleText}</p>
                    <div className="flex items-center">
                        <span className="text-[48px] font-bold text-[#1E4A84] mr-[15px]">{averageRating}</span>
                        <div className="flex">{renderStars(parseFloat(averageRating), 30)}</div>
                    </div>
                    <p className="text-[16px] text-[#888] mt-[5px]">({reviewCount} Reviews)</p>
                </div>

                {reviews.length > 0 ? reviews.map(item => (
                    <div key={item.id} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#E0E0E0] shadow-sm">
                        <div className="flex justify-between items-start mb-[10px]">
                            <div>
                                <p className="text-[16px] font-bold text-[#000]">{item.name}</p>
                                <div className="flex items-center mt-1">
                                    <CalendarRange size={14} color="#888" />
                                    <span className="text-[12px] text-[#888] ml-1">{item.date}</span>
                                </div>
                            </div>
                            <div className="flex">{renderStars(item.rating)}</div>
                        </div>
                        <p className="text-[13px] text-[#555] italic leading-[18px]">"{item.comment}"</p>
                    </div>
                )) : (
                    <div className="flex flex-col items-center py-[50px]">
                        <MessageSquareDashed size={50} color="#DDD" />
                        <p className="mt-[10px] text-[16px] text-[#999] italic">No reviews available yet.</p>
                    </div>
                )}

                <button onClick={() => router.back()} className="w-full h-[50px] rounded-[25px] bg-[#1E75EB] flex items-center justify-center mt-5 shadow-md">
                    <span className="text-white text-[18px] font-bold">{backLabel}</span>
                </button>
            </div>
        </main>
    );
}

export default function RateWorkerPage() {
    return <ReviewsPage titleText="Overall Rating" backLabel="Back" />;
}
