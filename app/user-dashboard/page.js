"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
  MapPin, Phone, User, Calendar,
  Users, Clock, ArrowLeft, Loader2
} from 'lucide-react';
import { SERVER_BASE, API_DASHBOARD } from '../../config';
import NotificationHelper from '../Notification/NotificationHelper';

const UserDashboard = () => {
  const router = useRouter();
  const [workers, setWorkers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  // States to hold user info
  const [userName, setUserName] = useState("Client User");
  const [userPicture, setUserPicture] = useState("");
  const [userAddress, setUserAddress] = useState("Your Address");
  const [userPhone, setUserPhone] = useState("03XXXXXXX");
  const [userEmail, setUserEmail] = useState("");
  const [userId, setUserId] = useState(null);
  const [hiredCount, setHiredCount] = useState(0);
  const [pendingInterviewsCount, setPendingInterviewsCount] = useState(0);

  useEffect(() => {
    loadUserInfo();
    fetchWorkers();
  }, []);

  const loadUserInfo = () => {
    try {
      const name = localStorage.getItem('userName');
      const pic = localStorage.getItem('userPicture');
      const addr = localStorage.getItem('userAddress');
      const phone = localStorage.getItem('userPhone');
      const email = localStorage.getItem('userEmail');
      const id = localStorage.getItem('clientId');

      if (name) setUserName(name);
      if (pic) setUserPicture(pic);
      if (addr) setUserAddress(addr);
      if (phone) setUserPhone(phone);
      if (email) setUserEmail(email);
      if (id) setUserId(id);
    } catch (error) {
      console.error("Error loading user info:", error);
    }
  };

  const handleEditProfile = () => {
    const params = new URLSearchParams({
      isEdit: 'true',
      role: 'Client',
      id: userId || '',
      name: userName,
      email: userEmail,
      phone: userPhone,
      location: userAddress,
      picture: userPicture,
    });
    router.push(`/signup?${params.toString()}`);
  };

  const fetchWorkers = async () => {
    setIsLoading(true);
    try {
      const token = localStorage.getItem('userToken');
      const response = await fetch(`${SERVER_BASE}/api/Dashboard/GetClientDashboard`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setWorkers(data.hiredWorkers || []);
        setHiredCount(data.hiredCount || 0);
        setPendingInterviewsCount(data.pendingInterviewsCount || 0);
      } else if (response.status === 401) {
        NotificationHelper.showError("Session Expired. Please login again.");
        router.replace('/');
      } else {
        console.error("Failed to fetch dashboard data", await response.text());
      }
    } catch (error) {
      console.error("Network error fetching dashboard data:", error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    router.replace('/');
  };

  const getWorkerImage = (picture) => {
    return picture && picture.startsWith('/')
      ? `${SERVER_BASE}${picture}`
      : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';
  };

  const getProfileImage = () => {
    return userPicture && userPicture.startsWith('/')
      ? `${SERVER_BASE}${userPicture}`
      : 'https://cdn-icons-png.flaticon.com/512/3135/3135768.png';
  };

  if (isLoading) {
    return (
        <div className="min-h-screen bg-white flex justify-center items-center">
            <Loader2 className="animate-spin" size={40} color="#1E64D3" />
        </div>
    );
  }

  return (
    <main className="min-h-screen bg-white pb-[30px] font-sans">
      <div className="px-5 max-w-3xl mx-auto">
        
        {/* Back Button */}
        <button onClick={() => router.back()} className="p-[10px] mt-5 -ml-[10px] mb-[5px] active:opacity-70 transition-opacity">
          <ArrowLeft size={24} color="#555" />
        </button>

        {/* Profile Header */}
        <div className="flex flex-row justify-between items-center mb-[20px]">
          <div className="flex-1">
            <p className="text-[13px] text-[#666] font-medium">Good Morning</p>
            <h1 className="text-[24px] font-bold text-[#000] mt-[2px]">{userName}</h1>
            <div className="flex flex-row mt-[15px] gap-2">
              <button 
                onClick={handleLogout}
                className="bg-[#1E64D3] px-[16px] py-[8px] rounded-[20px] shadow-[0_2px_5px_rgba(30,100,211,0.3)] hover:bg-[#1550a6] transition-colors"
              >
                <span className="text-white text-[12px] font-bold">Logout</span>
              </button>
              <button 
                onClick={handleEditProfile}
                className="bg-[#1E64D3] px-[16px] py-[8px] rounded-[20px] shadow-[0_2px_5px_rgba(30,100,211,0.3)] hover:bg-[#1550a6] transition-colors"
              >
                <span className="text-white text-[12px] font-bold">Edit Profile</span>
              </button>
            </div>
          </div>
          <img
            src={getProfileImage()}
            alt="profile"
            className="w-[90px] h-[90px] rounded-full border-4 border-white shadow-[0_4px_10px_rgba(0,0,0,0.1)] object-cover bg-[#f0f0f0]"
            onError={(e) => { e.target.src = 'https://cdn-icons-png.flaticon.com/512/3135/3135768.png'; }}
          />
        </div>

        {/* Address Card */}
        <div className="bg-white rounded-[25px] p-[18px] mb-[25px] border border-[#F0F0F0] shadow-[0_4px_10px_rgba(0,0,0,0.1)]">
          <div className="flex flex-row items-center mb-[12px]">
            <MapPin size={20} color="#E91E63" />
            <span className="ml-[15px] text-[#333] text-[14px] font-medium">{userAddress}</span>
          </div>
          <div className="flex flex-row items-center">
            <Phone size={20} color="#333" />
            <span className="ml-[15px] text-[#333] text-[14px] font-medium">{userPhone}</span>
          </div>
        </div>

        {/* Grid Menu */}
        <div className="flex flex-row flex-wrap justify-between mb-[10px] gap-y-4">
          <button onClick={() => router.push('/find-service')} className="bg-[#1E64D3] w-[48%] h-[48px] rounded-[25px] flex items-center justify-center shadow-[0_3px_5px_rgba(30,100,211,0.2)] hover:bg-[#1550a6] transition-colors">
            <span className="text-white font-bold text-[14px]">Services</span>
          </button>
          <button onClick={() => router.push('/active-requests')} className="bg-[#1E64D3] w-[48%] h-[48px] rounded-[25px] flex items-center justify-center shadow-[0_3px_5px_rgba(30,100,211,0.2)] hover:bg-[#1550a6] transition-colors">
            <span className="text-white font-bold text-[14px]">Interview Requests</span>
          </button>
          <button onClick={() => router.push('/worker-decision')} className="bg-[#1E64D3] w-[48%] h-[48px] rounded-[25px] flex items-center justify-center shadow-[0_3px_5px_rgba(30,100,211,0.2)] hover:bg-[#1550a6] transition-colors">
            <span className="text-white font-bold text-[14px]">Job Requests</span>
          </button>
          <button onClick={() => router.push('/resignations')} className="bg-[#1E64D3] w-[48%] h-[48px] rounded-[25px] flex items-center justify-center shadow-[0_3px_5px_rgba(30,100,211,0.2)] hover:bg-[#1550a6] transition-colors">
            <span className="text-white font-bold text-[14px]">Resignations</span>
          </button>
        </div>

        {/* Status Counters */}
        <h2 className="text-[22px] font-bold my-[18px] text-[#000]">Current Status</h2>
        <div className="flex flex-row justify-between mb-[20px]">
          <div className="bg-white w-[48%] p-[18px] rounded-[25px] flex flex-row items-center shadow-[0_4px_8px_rgba(0,0,0,0.1)] border border-[#F0F0F0]">
            <Users size={24} color="#333" />
            <span className="flex-1 ml-[12px] text-[13px] text-[#444] font-bold leading-tight">Workers</span>
            <span className="font-bold text-[18px] text-[#000]">{hiredCount}</span>
          </div>
          <div className="bg-white w-[48%] p-[18px] rounded-[25px] flex flex-row items-center shadow-[0_4px_8px_rgba(0,0,0,0.1)] border border-[#F0F0F0]">
            <Clock size={24} color="#333" />
            <span className="flex-1 ml-[12px] text-[13px] text-[#444] font-bold leading-tight">interview pending</span>
            <span className="font-bold text-[18px] text-[#000]">{pendingInterviewsCount}</span>
          </div>
        </div>

        <h2 className="text-[22px] font-bold my-[18px] text-[#000]">Current Worker</h2>

        {/* Worker Cards */}
        {workers.length === 0 ? (
          <p className="text-center mt-5 italic text-[#999]">You haven't hired any workers yet.</p>
        ) : (
          workers.map((item) => {
            const isAlert = item.type === 'alert' || item.status === 'Resigned';
            const type = item.type || 'active';
            
            let statusBg = '#E8F0FE';
            let statusBorder = '#1E64D3';
            let statusTextCol = '#0056B3';
            
            if (type === 'resigned') {
              statusBg = '#FFF9C4'; statusBorder = '#FFB300'; statusTextCol = '#F57F17';
            } else if (type === 'terminated') {
              statusBg = '#FFEBEE'; statusBorder = '#E53935'; statusTextCol = '#D32F2F';
            }
            
            return (
              <div
                key={item.interviewId || item.id}
                onClick={() => router.push(`/worker-detail?workerId=${item.id}`)}
                className={`relative bg-white rounded-[30px] p-[18px] mb-[20px] shadow-[0_6px_12px_rgba(0,0,0,0.1)] border cursor-pointer hover:shadow-lg transition-shadow ${isAlert ? 'border-[2.5px] border-[#FFD600]' : 'border-[#F0F0F0]'}`}
              >
                {isAlert && (
                  <div className="absolute top-[-2px] right-[30px] bg-[#FFF9C4] px-[12px] py-[5px] rounded-b-[15px] border border-t-0 border-[#FBC02D] z-10">
                    <span className="text-[11px] font-bold text-[#F57F17] tracking-wide">Resignation Alert</span>
                  </div>
                )}

                <div className="flex flex-row items-center">
                  <div className="relative">
                    <img
                      src={getWorkerImage(item.picture)}
                      alt={item.name}
                      className="w-[70px] h-[70px] rounded-[35px] bg-[#F8F8F8] border border-[#EEE] object-cover"
                    />
                    <div className="absolute bottom-0 right-0 bg-[#333] w-[20px] h-[20px] rounded-[10px] flex justify-center items-center border-[1.5px] border-white">
                      <User size={12} color="#FFF" />
                    </div>
                  </div>

                  <div className="ml-[18px] flex-1">
                    <h3 className="text-[18px] font-bold text-[#000]">{item.name}</h3>
                    <div className="inline-block border border-[#1E64D3] rounded-[12px] px-[10px] py-[3px] bg-[#F0F7FF] mt-1">
                      <span className="text-[10px] text-[#1E64D3] font-[800]">{item.role}</span>
                    </div>
                    <div className="flex flex-row items-center mt-[6px]">
                      <MapPin size={16} color="#E91E63" />
                      <span className="text-[13px] text-[#777] ml-[6px] font-medium">{item.location}</span>
                    </div>
                    <div className="flex flex-row items-center mt-[4px]">
                      <Calendar size={14} color="#888" />
                      <span className="text-[12px] text-[#888] ml-[6px]">Joined on {item.date}</span>
                    </div>
                  </div>
                </div>

                <div className="flex flex-row justify-between items-center mt-[20px]">
                  <div 
                    className="px-[18px] py-[6px] rounded-[12px] border-[1.5px] min-w-[90px] flex items-center justify-center"
                    style={{ backgroundColor: statusBg, borderColor: statusBorder }}
                  >
                    <span className="text-[12px] font-bold" style={{ color: statusTextCol }}>
                      {item.status || 'On Work'}
                    </span>
                  </div>

                  {(item.type === 'active' || item.type === 'alert') && (
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        router.push(`/terminate-contract?workerId=${item.id}&interviewId=${item.interviewId}`);
                      }}
                      className="bg-[#1E64D3] px-[30px] py-[10px] rounded-[15px] shadow-[0_2px_5px_rgba(0,0,0,0.2)] hover:bg-[#1550a6] transition-colors"
                    >
                      <span className="text-white font-bold text-[14px]">Terminate</span>
                    </button>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>
    </main>
  );
};

export default UserDashboard;