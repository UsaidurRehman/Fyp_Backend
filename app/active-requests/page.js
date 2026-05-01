"use client";

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Search, Loader2 } from 'lucide-react';
import NotificationHelper from '../Notification/NotificationHelper';
import { SERVER_BASE } from '../../config';

export default function ActiveRequestScreen() {
    const router = useRouter();
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [activeTab, setActiveTab] = useState('All');

    useEffect(() => { fetchRequests(); }, []);

    const fetchRequests = async () => {
        try {
            setLoading(true);
            const clientId = localStorage.getItem('clientId');
            const token = localStorage.getItem('userToken');
            const response = await fetch(`${SERVER_BASE}/api/Dashboard/GetActiveRequests/${clientId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (response.ok) setRequests(await response.json());
        } catch { NotificationHelper.showError("Failed to load requests."); }
        finally { setLoading(false); }
    };

    const handleHiringDecision = async (interviewId, decision) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${SERVER_BASE}/api/Dashboard/UpdateHiringStatus/${interviewId}`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify({ hiringDecision: decision })
            });
            if (res.ok) { NotificationHelper.showSuccess(`Interview ${decision}!`); fetchRequests(); }
            else NotificationHelper.showError("Update failed.");
        } catch { NotificationHelper.showError("Network error."); }
    };

    const handleDelete = async (interviewId) => {
        try {
            const token = localStorage.getItem('userToken');
            const res = await fetch(`${SERVER_BASE}/api/Dashboard/DeleteInterviewRequest/${interviewId}`, {
                method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) { NotificationHelper.showSuccess("Request deleted."); setRequests(p => p.filter(r => r.interviewId !== interviewId)); }
            else NotificationHelper.showError("Failed to delete.");
        } catch { NotificationHelper.showError("Network error."); }
    };

    const imgUrl = (img) => img && img.startsWith('/') ? `${SERVER_BASE}${img}` : 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png';

    const renderCard = (item) => {
        const { workerDecision, hiringDecision, workerImage, workerName, workerSkill, interviewId } = item;
        const src = imgUrl(workerImage);
        const workerAvatar = <img src={src} className="w-[70px] h-[70px] rounded-full border border-[#CCC] object-cover mr-[15px]" alt={workerName} />;
        const deleteBtn = <button onClick={() => handleDelete(interviewId)} className="text-[#F44336] font-bold text-[13px] mt-2">Delete</button>;

        if (workerDecision === 'Rejected') return (
            <div key={interviewId} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#666]">
                <div className="flex justify-end"><div className="flex items-center gap-1"><div className="w-2 h-2 rounded-full bg-[#4CAF50]" /><span className="text-[11px] font-bold">Interview Rejected</span></div></div>
                <div className="flex items-center mt-1">{workerAvatar}<div className="flex-1"><p className="font-bold">{workerName}</p><span className="inline-block bg-[#E0E0E0] border border-[#CCC] px-3 py-[3px] rounded-[10px] text-[11px]">Cancel</span><p className="text-[13px] text-[#999] my-1">{workerSkill}</p><p className="text-[12px] text-[#E65100]">Not Available right now</p></div>
                <div className="flex flex-col items-center gap-2"><button disabled className="bg-[#E0E0E0] px-3 py-2 rounded-[20px] w-[80px] text-white font-bold text-[13px]">Approve</button>{deleteBtn}</div></div>
            </div>
        );

        if (workerDecision === 'Accepted' && hiringDecision === 'Accepted') return (
            <div key={interviewId} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#666]">
                <div className="flex justify-end"><span className="text-[11px] font-bold">Interview Accepted</span></div>
                <div className="flex items-center mt-1">{workerAvatar}<div className="flex-1"><p className="font-bold">{workerName}</p><span className="inline-block bg-[#4CAF50] px-3 py-[3px] rounded-[10px] text-[11px] text-white font-bold">Accepted</span><p className="text-[13px] text-[#999] my-1">{workerSkill}</p><p className="text-[12px] text-[#4CAF50]">Verified</p></div>
                <div className="flex flex-col items-center gap-2"><button onClick={() => handleHiringDecision(interviewId,'Rejected')} className="bg-[#90CAF9] px-3 py-2 rounded-[20px] w-[80px] text-[#B71C1C] font-bold text-[13px]">Reject</button>{deleteBtn}</div></div>
            </div>
        );

        if (workerDecision === 'Accepted') return (
            <div key={interviewId} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#666]">
                <div className="flex items-center">{workerAvatar}<div className="flex-1"><p className="font-bold">{workerName}</p><span className="inline-block bg-[#D4E157] px-3 py-[3px] rounded-[10px] text-[11px] text-[#B71C1C] font-bold">Pending</span><p className="text-[13px] text-[#999] my-1">{workerSkill}</p><p className="text-[12px] text-[#EF6C00]">Awaiting Approbation</p></div>
                <div className="flex flex-col items-center gap-2"><button onClick={() => handleHiringDecision(interviewId,'Accepted')} className="bg-[#4CAF50] px-3 py-2 rounded-[20px] w-[80px] text-white font-bold text-[13px] mb-[15px]">Approve</button>{deleteBtn}</div></div>
            </div>
        );

        return (
            <div key={interviewId} className="bg-white rounded-[15px] p-[15px] mb-[15px] border border-[#666]">
                <div className="flex justify-end"><div className="flex items-center gap-1"><div className="w-2 h-2 rounded-full bg-[#4CAF50]" /><span className="text-[11px] font-bold">Inprocess</span></div></div>
                <div className="flex items-center mt-1">{workerAvatar}<div className="flex-1"><p className="font-bold">{workerName}</p><span className="inline-block bg-[#D4E157] px-3 py-[3px] rounded-[10px] text-[11px] text-[#B71C1C] font-bold">Pending</span><p className="text-[13px] text-[#999] my-1">{workerSkill}</p><p className="text-[12px] text-[#E65100]">Worker response pending</p></div>
                <div className="flex flex-col items-center gap-2"><button disabled className="bg-[#E0E0E0] px-3 py-2 rounded-[20px] w-[80px] text-white font-bold text-[13px]">Approve</button>{deleteBtn}</div></div>
            </div>
        );
    };

    const filtered = requests.filter(item => {
        if (searchQuery && !item.workerName?.toLowerCase().includes(searchQuery.toLowerCase()) && !item.workerSkill?.toLowerCase().includes(searchQuery.toLowerCase())) return false;
        if (activeTab === 'Pending' && item.workerDecision !== 'Pending' && item.hiringDecision !== 'Pending') return false;
        if (activeTab === 'Approved' && item.hiringDecision !== 'Accepted') return false;
        return true;
    });

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans">
            <div className="flex items-center p-5">
                <button onClick={() => router.back()} className="p-[5px] bg-[#E3EAF5] rounded-full w-10 h-10 flex items-center justify-center shadow">
                    <ArrowLeft size={24} color="#555" />
                </button>
                <h1 className="text-[24px] font-bold ml-[15px]">Interview List</h1>
                <img src="/images/logo.png" className="w-[50px] h-[50px] ml-auto" alt="Logo" />
            </div>
            <div className="flex items-center bg-white mx-5 rounded-[25px] px-[15px] border border-[#DDD] shadow-sm h-[50px]">
                <Search size={24} color="#888" className="mr-[10px]" />
                <input className="flex-1 outline-none text-[16px]" placeholder="Search by name or skills" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
            </div>
            <div className="flex justify-center gap-[10px] mt-5 mb-[15px]">
                {['All','Pending','Approved'].map(tab => (
                    <button key={tab} onClick={() => setActiveTab(tab)} className={`px-[25px] py-2 rounded-[20px] border font-bold transition-all ${activeTab===tab ? 'bg-[#1E64D3] border-[#1E64D3] text-white' : 'bg-white border-[#CCC] text-[#555]'}`}>{tab}</button>
                ))}
            </div>
            {loading ? <div className="flex justify-center mt-[50px]"><Loader2 className="animate-spin text-[#1E64D3]" size={40} /></div> :
                <div className="px-5 pb-5">{filtered.length > 0 ? filtered.map(renderCard) : <p className="text-center text-[#888] mt-10">No requests match your criteria.</p>}</div>
            }
        </main>
    );
}
