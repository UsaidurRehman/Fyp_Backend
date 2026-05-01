"use client";

import React, { useState, useEffect, useRef } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { SERVER_BASE, API_ACCOUNT } from '../../config';
import NotificationHelper from '../Notification/NotificationHelper';
import { 
  ArrowLeft, 
  User, 
  Users, 
  Camera, 
  Mail, 
  Phone, 
  MapPin, 
  Lock, 
  Eye, 
  EyeOff, 
  CreditCard, 
  DollarSign, 
  Calendar,
  CheckCircle2,
  PlusCircle,
  Plus,
  ChevronDown,
  Loader2,
  AlignLeft,
  CircleDot
} from 'lucide-react';

const FormInput = ({ 
  icon: IconComponent, 
  placeholder, 
  isPassword, 
  secure, 
  toggleSecure, 
  value, 
  onChangeText, 
  type = "text", 
  isButton, 
  onPress, 
  leftIconColor,
  multiline,
  rows
}) => (
  <div 
    onClick={isButton ? onPress : undefined}
    className={`flex flex-row items-center bg-white rounded-[20px] ${multiline ? 'min-h-[75px] items-start pt-[10px] pb-[10px]' : 'h-[55px]'} mb-[15px] px-[15px] shadow-[0_4px_10px_rgba(0,0,0,0.1)] ${isButton ? 'cursor-pointer hover:bg-gray-50 active:scale-[0.99] transition-transform' : ''}`}
  >
    <div 
      className="w-[35px] h-[35px] rounded-full flex items-center justify-center mr-[12px]"
      style={{ backgroundColor: leftIconColor ? `${leftIconColor}20` : '#F5F5F5' }}
    >
      <IconComponent size={20} color={leftIconColor || "#333"} />
    </div>
    
    {isButton ? (
      <div className={`flex-1 text-[16px] ${value ? 'text-[#333]' : 'text-[#999]'}`}>
        {value || placeholder}
      </div>
    ) : multiline ? (
      <textarea
        className="flex-1 text-[16px] text-[#333] outline-none bg-transparent resize-none leading-tight h-[55px]"
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChangeText(e.target.value)}
        rows={rows || 2}
      />
    ) : (
      <input
        className="flex-1 text-[16px] text-[#333] outline-none bg-transparent"
        placeholder={placeholder}
        type={secure ? "password" : type}
        value={value}
        onChange={(e) => onChangeText(e.target.value)}
      />
    )}

    {isPassword && (
      <button onClick={toggleSecure} className="bg-[#EEE] rounded-[15px] p-[4px] focus:outline-none">
        {secure ? <EyeOff size={22} color="#333" /> : <Eye size={22} color="#333" />}
      </button>
    )}
    
    {isButton && <Plus size={24} color="#333" />}
  </div>
);

const SignupScreen = () => {
  const router = useRouter();
  const searchParams = useSearchParams();
  const fileInputRef = useRef(null);

  const [role, setRole] = useState('Client'); 
  const [step, setStep] = useState(1);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  // Form States
  const [name, setName] = useState('');
  const [age, setAge] = useState('');
  const [phone, setPhone] = useState('');
  const [cnic, setCnic] = useState('');
  const [salary, setSalary] = useState('');
  const [email, setEmail] = useState('');
  const [address, setAddress] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [selectedImage, setSelectedImage] = useState(null); 
  const [previewUrl, setPreviewUrl] = useState(null); 
  const [hasAddedSkills, setHasAddedSkills] = useState(false);
  const [skillsData, setSkillsData] = useState(null);
  const [gender, setGender] = useState('Male');
  const [bio, setBio] = useState('');

  const isEdit = searchParams.get('isEdit') === 'true';

  useEffect(() => {
    // Restore session data for edit mode
    if (isEdit && !hasAddedSkills) {
      const editWorkerDataStr = sessionStorage.getItem('editWorkerData');
      if (editWorkerDataStr) {
        try {
          const data = JSON.parse(editWorkerDataStr);
          const targetRole = searchParams.get('role') || 'Worker';
          setRole(targetRole);
          
          setName(data.name || '');
          setPhone(data.phone || '');
          setAddress(data.location || '');
          setEmail(data.email || '');
          setBio(data.bio || '');
          const dbGender = data.gender ? data.gender.toLowerCase() : 'male';
          setGender(dbGender === 'female' ? 'Female' : 'Male');

          if (targetRole === 'Worker') {
            setAge(data.age?.toString() || '');
            setCnic(data.cnic || '');
            const rawSalary = data.salary ? data.salary.toString() : '0';
            setSalary(rawSalary.replace('Not Set', '0'));
            
            if (data.rawExperiences) {
              setSkillsData(data.rawExperiences);
              setHasAddedSkills(true);
            }
          }

          if (data.picture && typeof data.picture === 'string') {
            const picUrl = data.picture.startsWith('/') ? `${SERVER_BASE}${data.picture}` : data.picture;
            setPreviewUrl(picUrl);
          }
        } catch(e) {
          console.error("Error parsing edit data", e);
        }
      } else {
        // Just prefill from query params if available
        setRole(searchParams.get('role') || 'Client');
        setName(searchParams.get('name') || '');
        setPhone(searchParams.get('phone') || '');
        setAddress(searchParams.get('location') || '');
        setEmail(searchParams.get('email') || '');
        const pic = searchParams.get('picture');
        if (pic) {
          setPreviewUrl(pic.startsWith('/') ? `${SERVER_BASE}${pic}` : pic);
        }
      }
    }

    // Checking if returned from addSkills
    if (searchParams.get('skillsCompleted') === 'true') {
      setHasAddedSkills(true);
      // Restore other data from sessionStorage to avoid losing state when navigating to addSkills
      const savedState = sessionStorage.getItem('signupState');
      if (savedState) {
        const s = JSON.parse(savedState);
        setName(s.name); setAge(s.age); setPhone(s.phone); setCnic(s.cnic);
        setSalary(s.salary); setEmail(s.email); setAddress(s.address);
        setPassword(s.password); setConfirmPassword(s.confirmPassword);
        setRole(s.role); setGender(s.gender); setBio(s.bio);
        if (s.previewUrl) setPreviewUrl(s.previewUrl);
        if (s.experiencesJson) setSkillsData(JSON.parse(s.experiencesJson));
      }
      setStep(2);
    }
  }, [searchParams, isEdit, hasAddedSkills]);

  const goToSkills = () => {
    // Save current state before navigating away
    sessionStorage.setItem('signupState', JSON.stringify({
      name, age, phone, cnic, salary, email, address, password, confirmPassword,
      role, gender, bio, previewUrl,
      experiencesJson: skillsData ? JSON.stringify(skillsData) : null
    }));
    
    const params = new URLSearchParams(searchParams.toString());
    params.set('fromSignup', 'true');
    router.push(`/addSkills?${params.toString()}`);
  };

  const handleImagePick = (e) => {
    const file = e.target.files[0];
    if (file) {
      setSelectedImage(file);
      setPreviewUrl(URL.createObjectURL(file));
    }
  };

  const handleSignup = async () => {
    if (!isEdit && (password !== confirmPassword)) {
      NotificationHelper.showError("Passwords do not match.");
      return;
    }

    if (!selectedImage && !previewUrl) {
      NotificationHelper.showError("Please upload a profile picture.");
      return;
    }

    let endpoint = '';
    if (isEdit) {
      endpoint = role === 'Client' ? 'UpdateClient' : 'UpdateWorker';
    } else {
      endpoint = role === 'Client' ? 'SignupClient' : 'SignupWorker';
    }

    const url = `${API_ACCOUNT}/${endpoint}`;

    const formData = new FormData();
    if (isEdit) {
      const initialId = searchParams.get('id');
      if (!initialId) {
        NotificationHelper.showError("Session error: Missing profile ID.");
        return;
      }
      if (role === 'Client') {
        formData.append('ClientId', initialId);
      } else {
        formData.append('WorkerId', initialId);
      }
    }

    formData.append('Name', name);
    formData.append('Phone', phone);
    formData.append('Address', address);
    formData.append('Password', password || "********");
    formData.append('Email', email);

    if (role === 'Worker') {
      formData.append('Cnic', cnic);
      formData.append('Salary', salary || "0");
      formData.append('Age', age || "0");
      formData.append('CategoryId', "1"); // Default category
      formData.append('Gender', gender);
      formData.append('Bio', bio);

      const expJson = skillsData ? JSON.stringify(skillsData) : "[]";
      formData.append('experiencesJson', expJson);
    }

    if (selectedImage) {
      formData.append('PictureFile', selectedImage);
    }

    setIsLoading(true);
    try {
      const response = await fetch(url, {
        method: 'POST',
        body: formData,
      });
      
      const result = await response.json();
      
      if (response.ok) {
        NotificationHelper.showSuccess(result.message || "Operation successful!");
        if (isEdit) {
          if (role === 'Worker') {
            router.push('/worker-dashboard');
          } else {
            router.push('/user-dashboard');
          }
        } else {
          router.push('/login');
        }
      } else {
        NotificationHelper.showError(result.message || "Something went wrong.");
      }
    } catch (error) {
      console.error("Auth Action Error:", error);
      NotificationHelper.showError("Cannot connect to server.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-white flex flex-col font-sans">
      {/* Header */}
      <header className="flex flex-row items-center justify-between p-[20px] shadow-[0_2px_10px_rgba(0,0,0,0.1)] bg-white z-10">
        <button 
          onClick={() => step === 2 ? setStep(1) : router.back()} 
          className="p-[8px] bg-[#F0F0F0] rounded-[20px] active:scale-95 transition-transform"
        >
          <ArrowLeft size={24} color="#333" />
        </button>
        <h1 className="text-[22px] font-bold">{isEdit ? 'Edit Profile' : 'Create Account'}</h1>
        <img src="/images/logo.png" alt="Logo" className="w-[40px] h-[40px] object-contain" />
      </header>

      {/* Scrollable Content */}
      <div className="flex-1 overflow-y-auto px-[20px] pb-[40px] max-w-3xl mx-auto w-full pt-[20px]">
        
        {!isEdit && (
          <>
            <h2 className="text-[14px] font-bold mb-[10px] text-black">SELECT ROLE</h2>
            <div className="flex flex-row justify-between mb-[20px] gap-4">
              <button 
                className={`flex flex-row items-center justify-center w-1/2 h-[60px] rounded-[15px] border border-[#EEE] shadow-[0_2px_5px_rgba(0,0,0,0.05)] transition-colors ${role === 'Client' ? 'bg-[#E0DADA]' : 'bg-white'}`}
                onClick={() => { setRole('Client'); setStep(1); }}
              >
                <User size={24} color={role === 'Client' ? "#1E64D3" : "#000"} />
                <span className="ml-[10px] font-bold">Client</span>
              </button>
              <button 
                className={`flex flex-row items-center justify-center w-1/2 h-[60px] rounded-[15px] border border-[#EEE] shadow-[0_2px_5px_rgba(0,0,0,0.05)] transition-colors ${role === 'Worker' ? 'bg-[#E0DADA]' : 'bg-white'}`}
                onClick={() => setRole('Worker')}
              >
                <Users size={24} color={role === 'Worker' ? "#1E64D3" : "#000"} />
                <span className="ml-[10px] font-bold">Worker</span>
              </button>
            </div>
          </>
        )}

        {((role === 'Client') || (role === 'Worker' && step === 1)) && (
          <div className="flex flex-col">
            {/* Avatar Picker */}
            <div 
              className="self-center w-[110px] h-[110px] rounded-[55px] bg-[#F5F5F5] mb-[20px] shadow-[0_4px_10px_rgba(0,0,0,0.1)] overflow-hidden flex flex-col items-center justify-center border border-[#EEE] cursor-pointer"
              onClick={() => fileInputRef.current.click()}
            >
              <input 
                type="file" 
                ref={fileInputRef} 
                onChange={handleImagePick} 
                accept="image/*" 
                className="hidden" 
              />
              {previewUrl ? (
                <img src={previewUrl} className="w-full h-full object-cover" alt="Profile" />
              ) : (
                <div className="flex flex-col items-center">
                  <Camera size={30} color="#999" />
                  <span className="text-[10px] text-[#999] mt-1">Upload Photo</span>
                </div>
              )}
            </div>

            <FormInput icon={User} placeholder="Full Name" value={name} onChangeText={setName} />
            
            {role === 'Worker' && (
              <>
                <FormInput icon={Calendar} placeholder="Age" value={age} onChangeText={setAge} type="number" />
                <FormInput icon={CreditCard} placeholder="CNIC" value={cnic} onChangeText={setCnic} type="number" />
                <FormInput icon={DollarSign} placeholder="Salary" value={salary} onChangeText={setSalary} type="number" />
              </>
            )}

            <FormInput icon={Phone} placeholder="Phone no" value={phone} onChangeText={setPhone} type="tel" />

            {role === 'Worker' && (
              <FormInput icon={MapPin} placeholder="Address" value={address} onChangeText={setAddress} />
            )}

            {role === 'Client' && (
              <>
                <FormInput icon={Mail} placeholder="Email" value={email} onChangeText={setEmail} type="email" />
                <FormInput icon={MapPin} placeholder="Address" value={address} onChangeText={setAddress} />
                <FormInput icon={Lock} placeholder="Password" isPassword secure={!showPassword} toggleSecure={() => setShowPassword(!showPassword)} value={password} onChangeText={setPassword} />
                <FormInput icon={Lock} placeholder="Confirm Password" isPassword secure={!showConfirmPassword} toggleSecure={() => setShowConfirmPassword(!showConfirmPassword)} value={confirmPassword} onChangeText={setConfirmPassword} />
              </>
            )}

            <div className="flex flex-row justify-between mt-[20px] gap-4">
              <button 
                onClick={() => router.back()} 
                className="w-[47%] h-[50px] rounded-[25px] bg-[#1E64D3] text-white font-bold text-[18px] shadow-[0_4px_10px_rgba(0,0,0,0.2)] active:scale-95 transition-transform flex items-center justify-center"
              >
                Back
              </button>
              <button 
                onClick={() => role === 'Client' ? handleSignup() : setStep(2)} 
                className="w-[47%] h-[50px] rounded-[25px] bg-[#1E64D3] text-white font-bold text-[18px] shadow-[0_4px_10px_rgba(0,0,0,0.2)] active:scale-95 transition-transform flex items-center justify-center"
              >
                {isLoading ? <Loader2 className="animate-spin" /> : (role === 'Client' ? (isEdit ? 'Update Profile' : 'Signup') : 'Next')}
              </button>
            </div>
          </div>
        )}

        {role === 'Worker' && step === 2 && (
          <div className="flex flex-col">
            <h2 className="text-[14px] font-bold mb-[10px] text-black">PROFESSIONAL DESCRIPTION</h2>
            <FormInput 
              icon={AlignLeft} 
              placeholder="Briefly describe your work experience and skills..." 
              value={bio} 
              onChangeText={setBio} 
              multiline={true}
              rows={2}
            />

            <FormInput icon={Mail} placeholder="Email" value={email} onChangeText={setEmail} type="email" />
            
            <FormInput 
              icon={hasAddedSkills ? CheckCircle2 : PlusCircle} 
              leftIconColor={hasAddedSkills ? "#008000" : "#1E64D3"} 
              placeholder="Add Skills" 
              isButton={true} 
              value={hasAddedSkills ? "Skills Added" : ""} 
              onPress={goToSkills} 
            />

            <h2 className="text-[14px] font-bold mb-[10px] text-black">SELECT GENDER</h2>
            <div className="flex flex-row mb-[15px] gap-3">
              <button
                className={`flex flex-row items-center px-[20px] py-[10px] rounded-[20px] border border-[#EEE] transition-colors ${gender === 'Male' ? 'bg-[#1E64D3] border-[#1E64D3]' : 'bg-[#F5F5F5]'}`}
                onClick={() => setGender('Male')}
              >
                <User size={20} color={gender === 'Male' ? "#FFF" : "#333"} />
                <span className={`ml-[8px] font-bold ${gender === 'Male' ? 'text-white' : 'text-[#333]'}`}>Male</span>
              </button>
              <button
                className={`flex flex-row items-center px-[20px] py-[10px] rounded-[20px] border border-[#EEE] transition-colors ${gender === 'Female' ? 'bg-[#1E64D3] border-[#1E64D3]' : 'bg-[#F5F5F5]'}`}
                onClick={() => setGender('Female')}
              >
                <User size={20} color={gender === 'Female' ? "#FFF" : "#333"} />
                <span className={`ml-[8px] font-bold ${gender === 'Female' ? 'text-white' : 'text-[#333]'}`}>Female</span>
              </button>
            </div>

            <FormInput icon={Lock} placeholder="Password" isPassword secure={!showPassword} toggleSecure={() => setShowPassword(!showPassword)} value={password} onChangeText={setPassword} />
            <FormInput icon={Lock} placeholder="Confirm Password" isPassword secure={!showConfirmPassword} toggleSecure={() => setShowConfirmPassword(!showConfirmPassword)} value={confirmPassword} onChangeText={setConfirmPassword} />
            
            <div className="flex flex-row justify-between mt-[20px] gap-4">
              <button 
                onClick={() => setStep(1)} 
                className="w-[47%] h-[50px] rounded-[25px] bg-[#1E64D3] text-white font-bold text-[18px] shadow-[0_4px_10px_rgba(0,0,0,0.2)] active:scale-95 transition-transform flex items-center justify-center"
              >
                Back
              </button>
              <button 
                onClick={handleSignup} 
                className="w-[47%] h-[50px] rounded-[25px] bg-[#1E64D3] text-white font-bold text-[18px] shadow-[0_4px_10px_rgba(0,0,0,0.2)] active:scale-95 transition-transform flex items-center justify-center"
              >
                {isLoading ? <Loader2 className="animate-spin" /> : (isEdit ? 'Update Profile' : 'Submit')}
              </button>
            </div>
          </div>
        )}
      </div>
    </main>
  );
};

export default SignupScreen;