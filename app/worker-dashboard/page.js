"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
  UserX, XCircle, Mail, Bell, Star, Loader2, ArrowLeft,
} from 'lucide-react';
import { SERVER_BASE } from '../../config';
import NotificationHelper from '../Notification/NotificationHelper';

const WorkerDashboardScreen = () => {
  const router = useRouter();
  const [isDutyOn, setIsDutyOn] = useState(true);
  const [worker, setWorker] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchWorkerDetails();
  }, []);

  const fetchWorkerDetails = async () => {
    setIsLoading(true);
    try {
      const workerId = localStorage.getItem('workerId');
      const token = localStorage.getItem('userToken');

      if (!workerId || !token) {
        NotificationHelper.showError('Session not found. Please login again.');
        router.replace('/');
        return;
      }

      const url = `${SERVER_BASE}/api/Dashboard/GetWorkerDetail/${workerId}`;
      console.log('Fetching Worker Dashboard from:', url);

      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setWorker(data);
      } else if (response.status === 401) {
        NotificationHelper.showError('Session expired. Please login again.');
        router.replace('/');
      } else {
        let errorMessage = 'Could not fetch dashboard data.';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch (e) {
          const text = await response.text();
          if (text) errorMessage = text.substring(0, 100);
        }
        console.error('Dashboard Fetch Failed:', response.status, errorMessage);
        NotificationHelper.showError(`Error ${response.status}: ${errorMessage}`);
      }
    } catch (error) {
      console.error('Dashboard Fetch Error (Network):', error);
      NotificationHelper.showError('Network error. Verify connection and API IP.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    router.replace('/');
  };

  const handleEditProfile = () => {
    const params = new URLSearchParams({
      isEdit: 'true',
      id: worker?.id || worker?.workerId || '',
      name: worker?.name || '',
      email: worker?.email || '',
      phone: worker?.phone || '',
      location: worker?.location || '',
      picture: worker?.picture || '',
    });
    sessionStorage.setItem('editWorkerData', JSON.stringify(worker));
    router.push(`/signup?${params.toString()}`);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-white flex justify-center items-center">
        <Loader2 className="animate-spin" size={40} color="#1E64D3" />
      </div>
    );
  }

  if (!worker) return null;

  const profileImg =
    worker.picture && worker.picture.startsWith('/')
      ? `${SERVER_BASE}${worker.picture}`
      : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';

  return (
    <main className="min-h-screen bg-white pb-[40px] font-sans">
      <div className="px-5 max-w-3xl mx-auto pt-5">
        
        {/* Header Section */}
        <div className="flex flex-row justify-between mb-5">
          <div className="flex-1">
            <p className="text-[14px] text-[#888]">Good Afternoon,</p>
            <h1 className="text-[22px] font-bold text-[#000]">{worker.name}</h1>
            <div className="flex flex-row items-center mt-1">
              <span className="text-[28px] text-[#1E64D3] font-bold">{worker.role}</span>
              <span className="text-[16px] text-[#444] font-semibold ml-2">• {worker.age} Years Old</span>
            </div>
            <div className="flex flex-row mt-3 gap-[10px]">
              <button 
                onClick={handleLogout}
                className="bg-[#1E64D3] px-[20px] py-[8px] rounded-[20px] active:opacity-80 transition-opacity"
              >
                <span className="text-white font-bold text-[14px]">Logout</span>
              </button>
              <button 
                onClick={handleEditProfile}
                className="bg-[#1E64D3] px-[20px] py-[8px] rounded-[20px] active:opacity-80 transition-opacity"
              >
                <span className="text-white font-bold text-[14px]">Edit Profile</span>
              </button>
            </div>
          </div>
          <img 
            src={profileImg} 
            className="w-[80px] h-[80px] rounded-[40px] object-cover bg-[#f0f0f0]" 
            alt={worker.name} 
            onError={(e) => { e.target.src = 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png'; }}
          />
        </div>

        {/* Employment Actions */}
        <div className="bg-[#E0E0E0] rounded-[15px] p-[15px] mb-[15px]">
          <div className="flex flex-row items-center mb-[5px]">
            <UserX size={24} color="#FF4D4D" />
            <span className="text-[18px] font-bold ml-[10px] text-[#000]">Employment Actions</span>
          </div>
          <p className="text-[14px] text-[#555] mb-[15px]">
            Manage your job status and termination requests
          </p>
          <button
            onClick={() => router.push('/leave-job')}
            className="bg-white flex flex-row items-center justify-center h-[45px] rounded-[25px] border border-[#FF4D4D] active:bg-[#fff9f9] transition-colors"
          >
            <XCircle size={24} color="#FF4D4D" />
            <span className="text-[#FF4D4D] font-bold ml-[10px]">Resign from Job</span>
          </button>
        </div>

        {/* Duty Status */}
        <div className="bg-white rounded-[15px] p-[15px] mb-[15px] shadow-[0_3px_10px_rgba(0,0,0,0.1)] border border-[#F0F0F0]">
          <div className="flex flex-row justify-between items-center">
            <div>
              <p className="text-[18px] font-bold text-[#000]">Duty Status</p>
              <p className="text-[12px] text-[#888]">
                {isDutyOn
                  ? 'You are visible to customers'
                  : 'You are currently hidden'}
              </p>
            </div>
            {/* Custom Toggle Switch */}
            <div 
                className={`w-[50px] h-[30px] rounded-[15px] flex items-center px-[3px] cursor-pointer transition-colors duration-300 ${isDutyOn ? 'bg-[#4CAF50]' : 'bg-[#767577]'}`}
                onClick={() => setIsDutyOn(!isDutyOn)}
            >
                <div className={`w-[24px] h-[24px] bg-white rounded-full shadow-sm transform transition-transform duration-300 ${isDutyOn ? 'translate-x-[20px]' : 'translate-x-0'}`}></div>
            </div>
          </div>
        </div>

        {/* Notification Tabs */}
        <NotificationTab
          icon={<Mail size={24} color="#333" />}
          title="Interview Requests"
          subtitle={`Pending: ${worker.pendingRequestCount || 0}`}
          count={worker.pendingRequestCount || '0'}
          onClick={() => router.push('/worker-active-requests')}
        />
        <NotificationTab
          icon={<Bell size={24} color="#333" />}
          title="Job Notifications"
          subtitle="Job Confirmations and Rejections"
          count={worker.jobNotificationCount || '0'}
          onClick={() => router.push('/job-confirmation')}
        />

        <button
          className="bg-[#FF0000] h-[45px] rounded-[25px] flex justify-center items-center my-[10px] w-full active:bg-[#cc0000] transition-colors"
          onClick={() => router.push('/worker-termination-status')}
        >
          <span className="text-white font-bold text-[14px]">
            Check Termination Status{' '}
            {worker.terminationCount > 0 ? `(${worker.terminationCount})` : ''}
          </span>
        </button>

        {/* Profile Details */}
        <h2 className="text-[18px] font-bold mt-[20px] mb-[10px] text-[#000]">Profile Details</h2>
        <DetailRow label="Salary Expectation" value={`PKR ${worker.salary || 'N/A'}`} />
        <DetailRow label="Gender" value={worker.gender?.toUpperCase() || 'N/A'} />
        <DetailRow label="City" value={worker.location || 'N/A'} />

        {/* Experience History */}
        <h2 className="text-[18px] font-bold mt-[20px] mb-[10px] text-[#000]">Experience History</h2>
        {worker.experiences && worker.experiences.length > 0 ? (
          worker.experiences.map((exp, index) => (
            <ExperienceItem
              key={index}
              title={exp.title}
              bullets={[exp.details]}
              period={exp.period}
              isActive={index === 0}
            />
          ))
        ) : (
          <p className="text-[#999] italic mt-[5px] text-[13px]">No experiences recorded.</p>
        )}

        {/* Skills */}
        <h2 className="text-[18px] font-bold mt-[20px] mb-[10px] text-[#000]">My Specialized Skills</h2>
        <div className="flex flex-row flex-wrap gap-[10px] mt-[5px]">
          {worker.primarySkills && worker.primarySkills.length > 0 ? (
            worker.primarySkills.map((skill) => (
              <div key={skill} className="bg-[#DDD] px-[20px] py-[10px] rounded-[20px]">
                <span className="font-bold text-[14px] text-[#000]">{skill.toUpperCase()}</span>
              </div>
            ))
          ) : (
            <p className="text-[#999] italic mt-[5px] text-[13px] w-full">No specialized skills listed.</p>
          )}
          <button 
            onClick={() => router.push('/addSkills')}
            className="px-[20px] py-[10px] rounded-[20px] border border-[#1E64D3] hover:bg-[#F0F7FF] transition-colors"
          >
            <span className="text-[#1E64D3] font-bold">+ Edit Skills</span>
          </button>
        </div>

        {/* Reviews */}
        <h2 className="text-[18px] font-bold mt-[20px] mb-[10px] text-[#000]">Recent Client Reviews</h2>
        <div className="flex flex-row justify-between items-center mt-[5px]">
          <div className="flex flex-row items-center bg-[#F5F5F5] p-[10px] rounded-[20px]">
            <Star size={24} color="#FFD700" fill="#FFD700" />
            <span className="text-[20px] font-bold ml-[8px] text-[#000]">{worker.rating || '0.0'}</span>
          </div>
          <button
            className="bg-[#1E64D3] px-[30px] py-[12px] rounded-[25px] active:bg-[#1550a6] transition-colors"
            onClick={() => {
              const wId = worker.id || worker.workerId;
              router.push(
                `/worker-reviews?workerId=${wId}&initialRating=${worker.rating}&initialReviewCount=${worker.reviewCount}`
              );
            }}
          >
            <span className="text-white font-bold text-[14px]">
              View {worker.reviewCount || 0} Reviews
            </span>
          </button>
        </div>
      </div>
    </main>
  );
};

/* Sub-components */
const NotificationTab = ({ icon, title, subtitle, count, onClick }) => (
  <button 
    className="w-full flex flex-row items-center bg-white rounded-[15px] p-[12px] mb-[10px] border border-[#DDD] active:bg-[#f9f9f9] transition-colors text-left" 
    onClick={onClick}
  >
    <div className="bg-[#F0F0F0] p-[8px] rounded-[10px] flex-shrink-0">
        {icon}
    </div>
    <div className="flex-1 ml-[15px]">
      <p className="font-bold text-[16px] text-[#000]">{title}</p>
      <p className="text-[11px] text-[#888]">{subtitle}</p>
    </div>
    <div className="bg-[#D32F2F] w-[24px] h-[24px] rounded-[12px] flex justify-center items-center flex-shrink-0">
      <span className="text-white font-bold text-[12px]">{count}</span>
    </div>
  </button>
);

const DetailRow = ({ label, value }) => (
  <div className="flex flex-row justify-between py-[12px] border-b border-[#EEE]">
    <span className="text-[#555]">{label}</span>
    <span className="font-bold text-[#000]">{value}</span>
  </div>
);

const ExperienceItem = ({ title, bullets, period, isActive }) => (
  <div className="flex flex-row min-h-[100px]">
    <div className="flex flex-col items-center mr-[15px] w-[12px]">
      <div className={`w-[12px] h-[12px] rounded-[6px] flex-shrink-0 z-10 ${isActive ? 'bg-[#2196F3]' : 'bg-[#CCC]'}`} />
      <div className="w-[2px] bg-[#EEE] flex-1 -mt-[2px]" />
    </div>
    <div className="flex-1 pb-[20px]">
      <p className="font-bold text-[15px] text-[#000]">{title}</p>
      {bullets.map((b, i) => (
        <p key={i} className="text-[13px] text-[#777] italic mt-[3px]">
          • {b}
        </p>
      ))}
      <p className="text-[11px] text-[#999] mt-[5px] ml-auto w-fit">{period}</p>
    </div>
  </div>
);

export default WorkerDashboardScreen;