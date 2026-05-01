"use client";

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { 
  Mail, 
  Lock, 
  Eye, 
  EyeOff, 
  Square, 
  CheckSquare,
  User,
  Users,
  CreditCard,
  Loader2
} from 'lucide-react';
import NotificationHelper from './Notification/NotificationHelper';
import { API_AUTH } from '../config';

export default function LoginScreen() {
  const router = useRouter();
  
  // State management
  const [role, setRole] = useState('Client'); // 'Client' or 'Worker'
  const [emailOrCnic, setEmailOrCnic] = useState('');
  const [password, setPassword] = useState('');
  const [isPasswordVisible, setPasswordVisible] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleLogin = async (e) => {
    e.preventDefault();

    if (!emailOrCnic || !password) {
      NotificationHelper.showError("Please enter your credentials.");
      return;
    }

    setIsLoading(true);
    const url = `${API_AUTH}/Login`;

    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({
          Role: role,
          EmailOrCnic: emailOrCnic,
          Password: password
        })
      });

      const result = await response.json();

      if (response.ok) {
        // Save the token and role locally (localStorage replaces AsyncStorage)
        localStorage.setItem('userToken', result.token);
        localStorage.setItem('userRole', result.role);
        // Save name and picture for use across screens
        localStorage.setItem('userName', result.name || '');
        localStorage.setItem('userPicture', result.picture || '');
        localStorage.setItem('userAddress', result.address || '');
        localStorage.setItem('userPhone', result.phone || '');
        if (result.clientId) localStorage.setItem('clientId', result.clientId.toString());
        if (result.email) localStorage.setItem('userEmail', result.email);
        if (result.workerId) localStorage.setItem('workerId', result.workerId.toString());

        NotificationHelper.showSuccess("Login Successful!");

        // Navigate based on role — Client goes to FindService, Worker goes to Dashboard
        if (result.role === 'Client') {
          router.replace('/find-service');
        } else {
          router.replace('/worker-dashboard');
        }
      } else {
        NotificationHelper.showError(result.message || "Invalid credentials.");
      }
    } catch (error) {
      console.error(error);
      NotificationHelper.showError("Cannot reach the server.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-[#F8FBFF] flex flex-col items-center px-[30px] relative overflow-hidden font-sans">
      
      {/* Background Decorative Circle */}
      <div className="absolute -top-10 -left-10 w-[180px] h-[180px] rounded-full bg-[#D6EAF8] -z-10" />

      {/* Logo Section */}
      <div className="flex flex-col items-center mt-[40px] mb-[20px]">
        <div className="w-[120px] h-[120px] rounded-[25px] flex justify-center items-center overflow-hidden bg-white shadow-[0_5px_6px_rgba(0,0,0,0.3)]">
          <img 
            src="/images/logo.png" 
            alt="Logo"
            className="w-[90%] h-[90%] rounded-[25px] object-contain"
          />
        </div>
        <p className="mt-[15px] text-[20px] font-bold text-[#2C437E]">
          Maid & Servant Online
        </p>
      </div>

      <div className="w-full max-w-[400px]">

        <form onSubmit={handleLogin}>

          {/* Role Selection Tabs */}
          <div className="flex flex-row justify-between mb-[20px] gap-4">
            <button 
              type="button"
              onClick={() => setRole('Client')}
              className={`flex-1 flex items-center justify-center gap-2 h-[50px] rounded-[15px] border border-[#EEE] shadow-[0_2px_5px_rgba(0,0,0,0.1)] transition-all ${
                role === 'Client' 
                ? 'bg-[#E0DADA]' 
                : 'bg-white'
              }`}
            >
              <User size={24} color={role === 'Client' ? "#1E64D3" : "#000"} />
              <span className="ml-[10px] font-bold">Client</span>
            </button>

            <button 
              type="button"
              onClick={() => setRole('Worker')}
              className={`flex-1 flex items-center justify-center gap-2 h-[50px] rounded-[15px] border border-[#EEE] shadow-[0_2px_5px_rgba(0,0,0,0.1)] transition-all ${
                role === 'Worker' 
                ? 'bg-[#E0DADA]' 
                : 'bg-white'
              }`}
            >
              <Users size={24} color={role === 'Worker' ? "#1E64D3" : "#000"} />
              <span className="ml-[10px] font-bold">Worker</span>
            </button>
          </div>

          {/* Email/CNIC Input */}
          <div className="flex flex-row items-center bg-white rounded-[15px] px-[15px] h-[60px] shadow-[0_2px_5px_rgba(0,0,0,0.1)]">
            <div className="mr-[12px]">
              {role === 'Client' ? (
                <Mail size={24} color="#1E64D3" />
              ) : (
                <CreditCard size={24} color="#1E64D3" />
              )}
            </div>
            <input
              className="flex-1 text-[16px] text-[#333] outline-none bg-transparent"
              placeholder={role === 'Client' ? "Email" : "CNIC Without Dashes"}
              type={role === 'Client' ? "email" : "text"}
              inputMode={role === 'Client' ? "email" : "numeric"}
              value={emailOrCnic}
              onChange={(e) => setEmailOrCnic(e.target.value)}
              autoCapitalize="none"
              disabled={isLoading}
            />
          </div>

          {/* Password Input */}
          <div className="flex flex-row items-center bg-white rounded-[15px] px-[15px] h-[60px] shadow-[0_2px_5px_rgba(0,0,0,0.1)] mt-[15px]">
            <div className="mr-[12px]">
              <Lock size={24} color="#1E64D3" />
            </div>
            <input
              className="flex-1 text-[16px] text-[#333] outline-none bg-transparent"
              placeholder="Password"
              type={isPasswordVisible ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isLoading}
            />
            <button 
              type="button"
              onClick={() => setPasswordVisible(!isPasswordVisible)}
              className="p-[5px] focus:outline-none"
            >
              {isPasswordVisible ? (
                <EyeOff size={22} color="#666" />
              ) : (
                <Eye size={22} color="#666" />
              )}
            </button>
          </div>

          {/* Remember Me & Forgot Password */}
          <div className="flex flex-row justify-between items-center mt-[15px] mb-[25px]">
            <button 
              type="button"
              className="flex flex-row items-center focus:outline-none" 
              onClick={() => setRememberMe(!rememberMe)}
            >
              {rememberMe ? (
                <CheckSquare size={22} color="#1E64D3" />
              ) : (
                <Square size={22} color="#1E64D3" />
              )}
              <span className="ml-[8px] text-[#555] text-[14px]">Remember Me</span>
            </button>
            <button type="button" className="text-[#1E64D3] text-[14px] font-semibold">
              Forgot Password?
            </button>
          </div>

          {/* Sign In Button */}
          <button 
            type="submit"
            disabled={isLoading}
            className="w-full bg-white h-[55px] rounded-[30px] flex justify-center items-center border border-[#1E64D3] shadow-[0_3px_5px_rgba(0,0,0,0.1)] active:opacity-80 transition-opacity disabled:opacity-50"
          >
            {isLoading ? (
              <Loader2 className="animate-spin" color="#1E64D3" />
            ) : (
              <span className="text-[#1E64D3] text-[18px] font-bold">Sign in</span>
            )}
          </button>

          <p className="text-center my-[15px] text-[#888] text-[14px]">OR</p>

          {/* Signup Button */}
          <button 
            type="button"
            className="w-full bg-[#1E64D3] h-[55px] rounded-[30px] flex justify-center items-center shadow-[0_5px_5px_rgba(0,0,0,0.1)] active:opacity-90 transition-opacity"
            onClick={() => router.push('/signup')}
          >
            <span className="text-white text-[18px] font-bold">Signup</span>
          </button>
        </form>
      </div>
    </main>
  );
}