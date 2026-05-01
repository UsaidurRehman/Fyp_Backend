"use client";

import React from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, AlertTriangle, User } from 'lucide-react';

export default function WorkerTerminatedScreen() {
    const router = useRouter();
    const data = {
        worker: { name: 'Muzammil Khan', role: 'Driver', address: 'Rawalpindi', phone: '0330 32232453', experience: '3 Years', avatar: 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png' },
        details: { status: 'Terminated', date: '20-02-2025', reason: 'Bad Driver Behaviour' },
        client: { name: 'Fatima Batool', address: '6th road, RWP', avatar: 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png' }
    };

    return (
        <main className="max-w-md mx-auto bg-white min-h-screen font-sans relative overflow-hidden">
            <div className="absolute top-[-40px] left-[-40px] w-[150px] h-[150px] rounded-full bg-[#E3F2FD] -z-10" />
            <div className="flex justify-between items-center p-5 pt-[10px]">
                <button onClick={() => router.back()} className="p-[5px]"><ArrowLeft size={24} color="#555" /></button>
                <p className="text-[16px] text-[#666]">Contract {'>'} Worker <span className="text-[#2C3BE0] font-bold">Terminated</span></p>
                <img src="/images/logo.png" className="w-[40px] h-[40px]" alt="Logo" />
            </div>

            <div className="px-[15px] pb-[30px]">
                <div className="flex items-center bg-[#F0F4C3] border border-[#D4E157] rounded-[12px] p-[15px] mb-5 shadow-sm">
                    <AlertTriangle size={30} color="#FF3D00" />
                    <span className="text-[18px] font-bold text-[#000] ml-[15px]">WORKER TERMINATED</span>
                </div>

                <div className="border border-[#999] rounded-[8px] overflow-hidden mb-5">
                    <div className="flex items-center p-[10px] border-b border-[#999]">
                        <User size={20} color="#999" />
                        <span className="text-[16px] text-[#666] ml-[10px] font-medium">WORKER DETAILS</span>
                    </div>
                    <div className="flex p-[15px] items-center">
                        <img src={data.worker.avatar} className="w-[100px] h-[100px] rounded-full border border-[#EEE] object-cover" alt={data.worker.name} />
                        <div className="ml-[15px] flex-1">
                            <p className="text-[14px] text-[#666] mb-[2px]">Name: <span className="font-bold text-[#000]">{data.worker.name}</span></p>
                            <p className="text-[14px] text-[#666] mb-[2px]">Job Role: {data.worker.role}</p>
                            <p className="text-[14px] text-[#666] mb-[2px]">Address: {data.worker.address}</p>
                            <p className="text-[14px] text-[#666] mb-[2px]">Phone: {data.worker.phone}</p>
                            <p className="text-[14px] text-[#666]">Experience: {data.worker.experience}</p>
                        </div>
                    </div>
                    <div className="h-[1px] bg-[#EEE] mx-[15px]" />
                    <div className="p-[15px]">
                        <p className="text-[16px] text-[#333] mb-2"><span className="font-bold">Status :</span> {data.details.status}</p>
                        <p className="text-[16px] text-[#333] mb-2"><span className="font-bold">Date:</span> {data.details.date}</p>
                        <p className="text-[16px] text-[#333]"><span className="font-bold">Reason:</span> {data.details.reason}</p>
                    </div>
                </div>

                <p className="text-[14px] text-[#999] text-center my-[15px]">You are terminated and no longer active.</p>
                <div className="px-[10px] mb-5">
                    <p className="text-[12px] text-[#333] italic mb-[2px]">• No further access to service</p>
                    <p className="text-[12px] text-[#333] italic">• Account deactivated and contract terminated</p>
                </div>

                <p className="text-[18px] font-bold text-[#333] mb-[15px]">Client Details</p>
                <div className="flex items-center p-[15px] rounded-[25px] border border-[#DDD] bg-white shadow-md">
                    <img src={data.client.avatar} className="w-[80px] h-[80px] rounded-full object-cover" alt={data.client.name} />
                    <div className="ml-[15px]">
                        <p className="text-[#2C3BE0] font-bold text-[14px] mb-[5px]">Client Terminated Worker</p>
                        <p className="text-[14px] text-[#666]">Name: <span className="font-bold text-[#000]">{data.client.name}</span></p>
                        <p className="text-[14px] text-[#666]">Address: {data.client.address}</p>
                    </div>
                </div>
            </div>
        </main>
    );
}
