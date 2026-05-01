"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import {
  ArrowLeft,
  Search,
  Star,
  MapPin,
  ChevronRight,
  ListFilter,
  ArrowUp,
  UserSearch,
  User,
  Loader2
} from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { API_DASHBOARD, SERVER_BASE } from '../../config';

const API_BASE = API_DASHBOARD;

export default function FindServiceScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [search, setSearch] = useState('');
  const [categoriesList, setCategoriesList] = useState(['All']);
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [workers, setWorkers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [clientName, setClientName] = useState('Client');
  const [clientPicture, setClientPicture] = useState('');

  // Unified Advanced Filters
  const [allFilters, setAllFilters] = useState({
    gender: '',
    city: '',
    categories: [],
    subSkills: {} // format: {CategoryName: [SubSkill1, SubSkill2]}
  });

  // Load saved data and dynamic categories
  useEffect(() => {
    const name = localStorage.getItem('userName');
    const picture = localStorage.getItem('userPicture');
    if (name) setClientName(name);
    if (picture) setClientPicture(picture);
    fetchCategories();
  }, []);

  const fetchCategories = async () => {
    try {
      const token = localStorage.getItem('userToken');
      const response = await fetch(`${API_BASE}/GetFiltersData`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        const names = data.map(c => c.categoryName);
        setCategoriesList(['All', ...names]);
      }
    } catch (error) {
      console.error("Failed to fetch top categories:", error);
    }
  };

  // Listen for filter params coming back from FilterationScreen (via query string)
  useEffect(() => {
    const appliedFiltersParam = searchParams.get('appliedFilters');
    if (appliedFiltersParam) {
      try {
        const parsed = JSON.parse(decodeURIComponent(appliedFiltersParam));
        setAllFilters(parsed);
      } catch (err) {
        console.error("Failed to parse filters:", err);
      }
    }
  }, [searchParams]);

  const fetchWorkers = useCallback(
    async (categoryTab, searchText, currentFilters) => {
      setIsLoading(true);
      try {
        const token = localStorage.getItem('userToken');

        // Build query string
        let url = `${API_BASE}/GetWorkersForClient?`;

        // Add search text if available
        if (searchText && searchText.trim()) {
          url += `search=${encodeURIComponent(searchText.trim())}&`;
        }

        // Combine top tab selection with filters from filteration screen
        const combinedCategories = [...currentFilters.categories];
        if (categoryTab && categoryTab !== 'All' && !combinedCategories.includes(categoryTab)) {
          combinedCategories.push(categoryTab);
        }

        combinedCategories.forEach(cat => {
          url += `categories=${encodeURIComponent(cat)}&`;
        });

        if (currentFilters.gender && currentFilters.gender !== 'Both') {
          url += `gender=${encodeURIComponent(currentFilters.gender)}&`;
        }

        if (currentFilters.city) {
          url += `city=${encodeURIComponent(currentFilters.city)}&`;
        }

        // Sub-skills AND logic
        Object.keys(currentFilters.subSkills).forEach(catName => {
          currentFilters.subSkills[catName].forEach(skill => {
            url += `subSkills=${encodeURIComponent(skill)}&`;
          });
        });

        const response = await fetch(url, {
          headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.ok) {
          const data = await response.json();
          setWorkers(data);
        } else if (response.status === 401) {
          NotificationHelper.showError("Session Expired. Please login again.");
          router.replace('/');
        } else {
          console.error("Failed to fetch workers:", await response.text());
        }
      } catch (err) {
        console.error("Network error:", err);
      } finally {
        setIsLoading(false);
      }
    },
    [router]
  );

  // Initial load + whenever filters change
  useEffect(() => {
    fetchWorkers(selectedCategory, search, allFilters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedCategory, allFilters]);

  // Search on text change with optimized delay for better responsiveness
  useEffect(() => {
    const timer = setTimeout(() => {
      fetchWorkers(selectedCategory, search, allFilters);
    }, 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const extractCity = (address) => {
    if (!address || address === 'N/A') return 'N/A';
    const parts = address.split(',');
    return parts.length > 1 ? parts[parts.length - 1].trim() : address.trim();
  };

  const renderWorkerCard = (item) => {
    const city = extractCity(item.city);
    const imageUrl = item.picture && item.picture.startsWith('/')
      ? `${SERVER_BASE}${item.picture}`
      : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';

    return (
      <div
        key={item.id}
        className="bg-white rounded-[24px] p-4 mb-5 border border-[#F0F0F0] shadow-[0_4px_10px_rgba(0,0,0,0.1)]"
      >
        <div className="flex flex-row">
          {/* Image with rating badge */}
          <div className="relative">
            <img
              src={imageUrl}
              alt={item.name}
              className="w-[85px] h-[85px] rounded-[20px] bg-[#F8F9FA] object-cover"
            />
            <div className="absolute bottom-[19px] -right-[5px] flex flex-row items-center bg-white px-[6px] py-[2px] rounded-[8px] shadow-[0_2px_4px_rgba(0,0,0,0.1)]">
              <Star size={12} color="#FFD700" fill="#FFD700" />
              <span className="text-[10px] font-bold ml-[2px] text-[#333]">
                {item.rating || "0.0"}
              </span>
            </div>
          </div>

          {/* Main info */}
          <div className="flex-1 ml-4 flex flex-col justify-center">
            <div className="flex flex-row justify-between items-start">
              <p className="text-[18px] font-bold text-[#1A1C1E] flex-1 mr-2 truncate">
                {item.name}
              </p>
              <span className="text-[15px] font-bold text-[#00B14F] whitespace-nowrap">
                {item.salary || 'N/A'}
              </span>
            </div>
            <p className="text-[15px] font-bold text-gray-500">{item.gender || 'N/A'}</p>
            <p className="text-[#666] text-[13px] mb-1">{item.role}</p>

            <div className="flex flex-row items-center mb-2">
              <MapPin size={14} color="#666" />
              <span className="text-[12px] text-[#5F6368] ml-1 truncate">{city}</span>
            </div>

            <div className="flex flex-row gap-[6px] flex-wrap">
              {item.categories && item.categories.slice(0, 2).map((cat, index) => (
                <div
                  key={index}
                  className="px-[10px] py-[4px] rounded-[8px]"
                  style={{ backgroundColor: index === 0 ? '#E8F0FE' : '#F1F3F4' }}
                >
                  <span
                    className="text-[11px] font-semibold"
                    style={{ color: index === 0 ? '#1E64D3' : '#5F6368' }}
                  >
                    {cat}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Action Button */}
        <button
          onClick={() => router.push(`/worker-detail?workerId=${item.id}`)}
          className="w-full bg-[#1E64D3] rounded-[16px] h-[48px] flex flex-row items-center justify-center mt-4 active:opacity-90 transition-opacity"
        >
          <span className="text-white font-bold text-[15px] mr-1">
            View Profile & Interview
          </span>
          <ChevronRight size={20} color="#FFF" />
        </button>
      </div>
    );
  };

  return (
    <main className="min-h-screen bg-white flex flex-col">
      {/* Header */}
      <div className="px-5 pt-[45px] pb-[10px] z-10">
        <div className="flex flex-row items-center mb-[5px]">
          <button
            onClick={() => router.back()}
            className="p-[5px] bg-[#F0F0F0] rounded-[20px] w-9 h-9 flex justify-center items-center shadow-[0_2px_5px_rgba(0,0,0,0.1)]"
          >
            <ArrowLeft size={24} color="#555" />
          </button>
          <p className="text-[22px] font-bold text-[#001F3F] ml-[10px]">
            Welcome, {clientName}
          </p>
        </div>
        <div className="flex flex-row justify-between">
          <div>
            <p className="text-[13px] font-bold text-[#333] mt-[2px]">FIND SERVICE</p>
            <p className="text-[13px] text-[#666]">What would you like to do?</p>
          </div>
          <button
            className="relative"
            onClick={() => router.push('/user-dashboard')}
          >
            <img
              src={
                clientPicture && clientPicture.startsWith('/')
                  ? `${SERVER_BASE}${clientPicture}`
                  : 'https://cdn-icons-png.flaticon.com/512/3135/3135768.png'
              }
              alt="Profile"
              className="w-[55px] h-[55px] rounded-full bg-[#EEE] object-cover"
              onError={() => console.log('Client image failed')}
            />
            <div className="absolute bottom-0 right-0 bg-[#EEE] rounded-[10px] p-[2px] border border-white shadow-sm">
              <User size={14} color="#000" />
            </div>
          </button>
        </div>
      </div>

      {/* Search Bar */}
      <div className="flex flex-row bg-white mx-5 mb-[15px] rounded-[15px] px-[15px] items-center border border-[#DDD] shadow-[0_2px_5px_rgba(0,0,0,0.05)]">
        <input
          className="flex-1 h-[45px] bg-transparent outline-none text-[14px]"
          placeholder="Search by Name"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Search size={24} color="#333" className="ml-[10px]" />
      </div>

      {/* Category Tabs */}
      <div className="flex flex-row px-[15px] mb-[15px] flex-wrap">
        {categoriesList.map((cat) => (
          <button
            key={cat}
            className={`px-[15px] py-[7px] rounded-[20px] mr-2 mb-[5px] border transition-all ${
              selectedCategory === cat
                ? 'bg-[#1E64D3] border-[#1E64D3]'
                : 'bg-[#F5F5F5] border-[#DDD]'
            }`}
            onClick={() => setSelectedCategory(cat)}
          >
            <span
              className={`font-bold text-[13px] ${
                selectedCategory === cat ? 'text-white' : 'text-[#333]'
              }`}
            >
              {cat}
            </span>
          </button>
        ))}
      </div>

      {/* Filter & Sort Row */}
      <div className="flex flex-row justify-around px-5 mb-[5px]">
        <button
          className="flex flex-row items-center bg-[#F5F5F5] px-[30px] py-2 rounded-[12px] border border-[#DDD]"
          onClick={() =>
            router.push(
              `/filteration?initialFilters=${encodeURIComponent(JSON.stringify(allFilters))}`
            )
          }
        >
          <span className="mr-2 text-[#333] font-medium">Filter</span>
          <ListFilter size={18} color="#666" />
        </button>
        <button className="flex flex-row items-center bg-[#F5F5F5] px-[30px] py-2 rounded-[12px] border border-[#DDD]">
          <span className="mr-2 text-[#333] font-medium">Sort</span>
          <ArrowUp size={18} color="#666" />
        </button>
      </div>

      <p className="self-end mr-5 text-[12px] text-[#999] my-2">
        {workers.length} Total Results
      </p>

      {/* Workers List */}
      {isLoading ? (
        <div className="flex justify-center mt-[50px]">
          <Loader2 className="animate-spin" size={40} color="#1E64D3" />
        </div>
      ) : workers.length === 0 ? (
        <div className="flex flex-col items-center mt-[60px]">
          <UserSearch size={60} color="#CCC" />
          <p className="text-[#999] text-[16px] mt-[10px]">No workers found</p>
        </div>
      ) : (
        <div className="px-5 pb-[30px]">
          {workers.map(renderWorkerCard)}
        </div>
      )}
    </main>
  );
}