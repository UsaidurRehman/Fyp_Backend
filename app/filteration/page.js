"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, User, MapPin, AlignJustify, X, ChevronDown, Check, Loader2 } from 'lucide-react';
import { API_DASHBOARD } from '../../config';

const RECOMMENDED_CITIES = [
    'Islamabad', 'Rawalpindi', 'Lahore', 'Karachi',
    'Faisalabad', 'Peshawar', 'Multan', 'Quetta',
    'Gujranwala', 'Sialkot'
];

export default function FilterationScreen() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const [allCategories, setAllCategories] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [selectedGender, setSelectedGender] = useState('');
    const [selectedSkills, setSelectedSkills] = useState([]);
    const [selectedCity, setSelectedCity] = useState('');
    const [cityModalVisible, setCityModalVisible] = useState(false);
    const [subFilters, setSubFilters] = useState({});

    useEffect(() => {
        fetchFilterData();
    }, []);

    useEffect(() => {
        if (!isLoading) {
            const param = searchParams.get('initialFilters');
            if (param) {
                try {
                    const { gender, city, categories, subSkills } = JSON.parse(decodeURIComponent(param));
                    setSelectedGender(gender || '');
                    setSelectedCity(city || '');
                    setSelectedSkills(categories || []);
                    if (subSkills) {
                        setSubFilters(prev => {
                            const merged = { ...prev };
                            Object.keys(subSkills).forEach(k => { merged[k] = [...subSkills[k]]; });
                            return merged;
                        });
                    }
                } catch (e) { console.error(e); }
            }
        }
    }, [isLoading, searchParams]);

    const fetchFilterData = async () => {
        setIsLoading(true);
        try {
            const token = localStorage.getItem('userToken');
            const response = await fetch(`${API_DASHBOARD}/GetFiltersData`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (response.ok) {
                const data = await response.json();
                setAllCategories(data);
                const init = {};
                data.forEach(cat => { init[cat.categoryName] = []; });
                setSubFilters(init);
            }
        } catch (error) {
            console.error("Network error:", error);
        } finally {
            setIsLoading(false);
        }
    };

    const toggleSkill = (name) => {
        setSelectedSkills(prev =>
            prev.includes(name) ? prev.filter(s => s !== name) : [...prev, name]
        );
    };

    const toggleSubFilter = (category, value) => {
        setSubFilters(prev => {
            const current = prev[category] ? [...prev[category]] : [];
            return {
                ...prev,
                [category]: current.includes(value) ? current.filter(i => i !== value) : [...current, value]
            };
        });
    };

    const buildFilterParam = (gender, city, categories, sub) =>
        encodeURIComponent(JSON.stringify({ gender, city, categories, subSkills: sub }));

    const handleApply = () => {
        router.push(`/find-service?appliedFilters=${buildFilterParam(selectedGender, selectedCity, selectedSkills, subFilters)}`);
    };

    const handleReset = () => {
        const reset = {};
        allCategories.forEach(cat => { reset[cat.categoryName] = []; });
        router.push(`/find-service?appliedFilters=${buildFilterParam('', '', [], reset)}`);
    };

    if (isLoading) return (
        <div className="flex justify-center items-center h-screen bg-white">
            <Loader2 className="animate-spin text-[#1E64D3]" size={48} />
        </div>
    );

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen flex flex-col font-sans relative">

            {/* City Modal */}
            {cityModalVisible && (
                <div className="fixed inset-0 bg-black/50 z-50 flex items-end justify-center" onClick={() => setCityModalVisible(false)}>
                    <div className="bg-white rounded-t-[30px] p-5 w-full max-w-md max-h-[70vh] flex flex-col" onClick={e => e.stopPropagation()}>
                        <div className="flex justify-between items-center mb-5 pb-[10px] border-b border-[#EEE]">
                            <h2 className="text-[20px] font-bold text-[#333]">Select Recommended City</h2>
                            <button onClick={() => setCityModalVisible(false)}><X size={24} color="#333" /></button>
                        </div>
                        <div className="overflow-y-auto">
                            {RECOMMENDED_CITIES.map(city => (
                                <button key={city} className="w-full flex justify-between items-center py-[15px] border-b border-[#F5F5F5]"
                                    onClick={() => { setSelectedCity(city); setCityModalVisible(false); }}>
                                    <span className={`text-[16px] ${selectedCity === city ? 'text-[#1E64D3] font-bold' : 'text-[#666]'}`}>{city}</span>
                                    {selectedCity === city && <Check size={20} color="#1E64D3" />}
                                </button>
                            ))}
                        </div>
                    </div>
                </div>
            )}

            {/* Header */}
            <div className="flex items-center p-5 mt-5 z-10">
                <button onClick={() => router.back()} className="w-[35px] h-[35px] rounded-full bg-[#F0F0F0] flex items-center justify-center shadow">
                    <ArrowLeft size={20} color="#666" />
                </button>
                <h1 className="flex-1 text-center text-[24px] font-bold mr-[35px]">FILTERATION</h1>
            </div>

            <div className="flex-1 overflow-y-auto px-[15px] pb-[100px]">
                {/* Active Tags */}
                {(selectedGender || selectedCity || selectedSkills.length > 0) && (
                    <div className="flex flex-wrap gap-2 mb-5">
                        {selectedGender && (
                            <span className="flex items-center bg-[#D9D9D9] px-3 py-[6px] rounded-[10px] text-[13px] font-medium">
                                {selectedGender}
                                <button onClick={() => setSelectedGender('')} className="ml-[5px]"><X size={16} /></button>
                            </span>
                        )}
                        {selectedCity && (
                            <span className="flex items-center bg-[#D9D9D9] px-3 py-[6px] rounded-[10px] text-[13px] font-medium">
                                {selectedCity}
                                <button onClick={() => setSelectedCity('')} className="ml-[5px]"><X size={16} /></button>
                            </span>
                        )}
                        {selectedSkills.map(s => (
                            <span key={s} className="flex items-center bg-[#D9D9D9] px-3 py-[6px] rounded-[10px] text-[13px] font-medium">
                                {s}
                                <button onClick={() => toggleSkill(s)} className="ml-[5px]"><X size={16} /></button>
                            </span>
                        ))}
                    </div>
                )}

                {/* Gender */}
                <div className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#EEE] shadow-sm">
                    <div className="flex items-center mb-[15px]">
                        <User size={22} color="#000" />
                        <span className="text-[16px] font-bold ml-[10px] tracking-widest">GENDER</span>
                    </div>
                    <div className="flex gap-2">
                        {['Male', 'Female', 'Both'].map(g => (
                            <button key={g} onClick={() => setSelectedGender(g)}
                                className={`flex-1 h-[40px] rounded-[10px] border font-bold text-sm transition-all ${selectedGender === g ? 'bg-[#1E64D3] border-[#1E64D3] text-white' : 'bg-white border-[#DDD] text-[#333]'}`}>
                                {g}
                            </button>
                        ))}
                    </div>
                </div>

                {/* Skills */}
                <div className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#EEE] shadow-sm">
                    <div className="flex items-center mb-[15px]">
                        <User size={22} color="#000" />
                        <span className="text-[16px] font-bold ml-[10px] tracking-widest">SKILLS</span>
                    </div>
                    <div className="flex gap-2 overflow-x-auto pb-1">
                        {allCategories.map(cat => (
                            <button key={cat.categoryId} onClick={() => toggleSkill(cat.categoryName)}
                                className={`px-5 h-[40px] rounded-[10px] border font-bold text-sm whitespace-nowrap transition-all ${selectedSkills.includes(cat.categoryName) ? 'bg-[#1E64D3] border-[#1E64D3] text-white' : 'bg-white border-[#DDD] text-[#333]'}`}>
                                {cat.categoryName}
                            </button>
                        ))}
                    </div>
                </div>

                {/* City */}
                <div className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#EEE] shadow-sm">
                    <div className="flex items-center mb-[15px]">
                        <MapPin size={22} color="#000" />
                        <span className="text-[16px] font-bold ml-[10px] tracking-widest">CITY</span>
                    </div>
                    <button onClick={() => setCityModalVisible(true)}
                        className="w-full flex justify-between items-center border border-[#DDD] rounded-[12px] px-[15px] h-[50px]">
                        <span className={`text-[16px] ${selectedCity ? 'text-[#333]' : 'text-[#999]'}`}>
                            {selectedCity || 'Select Recommended City'}
                        </span>
                        <ChevronDown size={24} color="#000" />
                    </button>
                </div>

                {/* Sub-Categories */}
                {selectedSkills.length > 0 && (
                    <div className="bg-white rounded-[15px] p-[15px] border border-[#EEE] shadow-sm">
                        <div className="flex items-center mb-[15px]">
                            <AlignJustify size={20} color="#000" />
                            <span className="text-[16px] font-bold ml-[10px]">Sub-Category</span>
                        </div>
                        {allCategories
                            .filter(cat => selectedSkills.includes(cat.categoryName))
                            .map(cat => (
                                <div key={cat.categoryId} className="mb-5">
                                    <p className="text-[15px] font-bold underline mb-[10px]">{cat.categoryName}</p>
                                    <div className="flex flex-wrap gap-2">
                                        {cat.skills.map(opt => (
                                            <button key={opt} onClick={() => toggleSubFilter(cat.categoryName, opt)}
                                                className={`px-[15px] py-2 rounded-[15px] border text-[12px] transition-all ${(subFilters[cat.categoryName] || []).includes(opt) ? 'bg-[#1E64D3] border-[#1E64D3] text-white' : 'bg-white border-[#DDD] text-[#333]'}`}>
                                                {opt}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            ))}
                    </div>
                )}
            </div>

            {/* Footer */}
            <div className="fixed bottom-0 left-1/2 -translate-x-1/2 w-full max-w-md flex gap-4 p-5 bg-white border-t border-[#EEE] z-10">
                <button onClick={handleReset} className="flex-1 h-[50px] rounded-[25px] bg-[#555] flex items-center justify-center">
                    <span className="text-white text-[18px] font-bold">Reset</span>
                </button>
                <button onClick={handleApply} className="flex-1 h-[50px] rounded-[25px] bg-[#1E64D3] flex items-center justify-center">
                    <span className="text-white text-[18px] font-bold">Apply</span>
                </button>
            </div>
        </main>
    );
}
