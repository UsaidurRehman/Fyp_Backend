"use client";

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { 
  ArrowLeft, 
  ChefHat, 
  Car, 
  Brush, 
  Calendar, 
  MapPin, 
  FileText,
  Loader2
} from 'lucide-react';
import { API_ACCOUNT } from '../../config';
import NotificationHelper from '../Notification/NotificationHelper';

const API_BASE_URL = API_ACCOUNT;

const ExpertiseSection = ({ title, icon: IconComponent, isActive, categoryId, onExperienceAdded }) => {
  const [dateText, setDateText] = useState('');
  const [workAt, setWorkAt] = useState('');
  const [description, setDescription] = useState('');
  const [subCategories, setSubCategories] = useState([]);
  const [selectedSubSkillIds, setSelectedSubSkillIds] = useState([]);
  const [isFetching, setIsFetching] = useState(false);

  useEffect(() => {
    if (isActive && categoryId) {
      setIsFetching(true);
      fetch(`${API_BASE_URL}/GetSkillsByCategory?categoryId=${categoryId}`)
        .then(res => res.json())
        .then(data => {
          if (Array.isArray(data)) {
            const formattedData = data.map((item, index) => ({
              id: item.id ?? item.SkillsId ?? item.skillsId ?? `skill-${index}`,
              name: item.name ?? item.SkillName ?? item.skillName ?? "Missing Name"
            }));
            setSubCategories(formattedData);
          } else {
            setSubCategories([]);
          }
        })
        .catch(err => {
          console.error("Error fetching skills:", err);
          setSubCategories([]); 
        })
        .finally(() => setIsFetching(false));
    }
  }, [isActive, categoryId]);

  const calculateDuration = (startDate) => {
    const start = new Date(startDate);
    const end = new Date();
    let years = end.getFullYear() - start.getFullYear();
    let months = end.getMonth() - start.getMonth();
    if (months < 0) { years--; months += 12; }

    if (years > 0 && months > 0) return `${years} Years, ${months} Months`;
    if (years > 0) return `${years} ${years === 1 ? 'Year' : 'Years'}`;
    return `${months} ${months === 1 ? 'Month' : 'Months'}`;
  };

  const toggleSkillSelection = (id) => {
    if (selectedSubSkillIds.includes(id)) {
      setSelectedSubSkillIds(selectedSubSkillIds.filter(item => item !== id));
    } else {
      setSelectedSubSkillIds([...selectedSubSkillIds, id]);
    }
  };

  const handleLocalAdd = () => {
    if (!workAt || !description || !dateText || selectedSubSkillIds.length === 0) {
      NotificationHelper.showError("Please select at least one sub-category and fill all fields.");
      return;
    }

    selectedSubSkillIds.forEach(skillId => {
      const newExp = {
        WorkAt: workAt,
        ExpDetail: description,
        Duration: calculateDuration(dateText),
        CategoryId: categoryId,
        SkillsId: skillId
      };
      onExperienceAdded(newExp);
    });

    setWorkAt('');
    setDescription('');
    setDateText('');
    setSelectedSubSkillIds([]);
    NotificationHelper.showSuccess(`${selectedSubSkillIds.length} Experience(s) added to your submission list.`);
  };

  return (
    <div className="bg-white rounded-[25px] p-[20px] mb-[20px] border border-[#EEE] shadow-[0_4px_10px_rgba(0,0,0,0.1)]">
      <div className="flex items-center mb-[15px]">
        <div className="w-[45px] h-[45px] rounded-[22.5px] flex items-center justify-center mr-[15px] bg-[#D7E6FF]">
          <IconComponent size={24} color="#1E64D3" />
        </div>
        <h3 className="text-[16px] font-bold text-[#000]">{title} Expertise</h3>
      </div>

      <p className="text-[12px] text-[#666] font-bold mt-[10px] mb-[5px] uppercase">SUB CATEGORIES (Tap to select)</p>
      <div className="flex flex-wrap gap-2 mb-[10px]">
        {isFetching ? (
          <Loader2 className="animate-spin text-[#1E64D3]" size={20} />
        ) : (
          subCategories.map((skill) => (
            <button
              key={skill.id}
              onClick={() => toggleSkillSelection(skill.id)}
              className={`px-[12px] py-[6px] rounded-[10px] text-[12px] transition-colors border ${selectedSubSkillIds.includes(skill.id) ? 'bg-[#1E64D3] border-[#1E64D3] text-white font-bold' : 'bg-[#EEE] border-[#EEE] text-[#333]'}`}
            >
              {skill.name}
            </button>
          ))
        )}
      </div>

      <p className="text-[12px] text-[#666] font-bold mt-[10px] mb-[5px] uppercase">WORKING SINCE</p>
      <div className="flex items-center bg-white rounded-[12px] px-[12px] h-[45px] mb-[10px] border border-[#DDD]">
        <input 
          type="date" 
          max={new Date().toISOString().split("T")[0]}
          className="flex-1 text-[14px] outline-none bg-transparent"
          value={dateText}
          onChange={(e) => setDateText(e.target.value)}
        />
        <Calendar size={24} color="#333" />
      </div>

      <div className="flex items-center bg-white rounded-[12px] px-[12px] h-[45px] mb-[10px] border border-[#DDD]">
        <MapPin size={20} color="#333" className="mr-[10px]" />
        <input 
          className="flex-1 text-[14px] outline-none text-[#333]" 
          placeholder="Where did you work?" 
          value={workAt} 
          onChange={(e) => setWorkAt(e.target.value)} 
        />
      </div>

      <div className="flex items-center bg-white rounded-[12px] px-[12px] h-[45px] mb-[10px] border border-[#DDD]">
        <FileText size={20} color="#333" className="mr-[10px]" />
        <input 
          className="flex-1 text-[14px] outline-none text-[#333]" 
          placeholder="Describe your role" 
          value={description} 
          onChange={(e) => setDescription(e.target.value)} 
        />
      </div>
      
      <button 
        onClick={handleLocalAdd}
        className="ml-auto flex items-center bg-[#1E64D3] px-[25px] py-[10px] rounded-[15px] mt-[10px] active:scale-95 transition-transform"
      >
        <span className="text-white font-bold text-[14px]">+ Add Experience</span>
      </button>
    </div>
  );
};

const SkillBox = ({ icon: IconComponent, label, selected, disabled, onPress }) => (
    <button 
      disabled={disabled}
      onClick={onPress}
      className={`w-[30%] h-[100px] flex flex-col items-center justify-center rounded-[15px] shadow-[0_4px_10px_rgba(0,0,0,0.1)] transition-all duration-200 
        ${selected ? 'bg-[#008000]' : 'bg-white'} 
        ${disabled ? 'opacity-30 cursor-not-allowed' : 'cursor-pointer active:scale-95'}`}
    >
      <div className={`w-[50px] h-[50px] rounded-full flex items-center justify-center mb-[5px] 
        ${selected ? 'bg-white/20' : 'bg-[#F0EAF8]'}`}>
        <IconComponent size={30} color={selected ? "#FFF" : "#333"} />
      </div>
      <span className={`font-bold text-[13px] ${selected ? 'text-white' : 'text-black'}`}>{label}</span>
    </button>
);

export default function AddSkillsScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [primary, setPrimary] = useState(null);
  const [secondary, setSecondary] = useState(null);
  const [experienceList, setExperienceList] = useState([]);

  const categoryMap = { 'Cleaning': 2, 'Cooking': 1, 'Driving': 3 };
  const reverseCategoryMap = { 2: 'Cleaning', 1: 'Cooking', 3: 'Driving' };
  const iconMap = { 'Cooking': ChefHat, 'Driving': Car, 'Cleaning': Brush };

  useEffect(() => {
    const stateStr = sessionStorage.getItem('signupState');
    if (stateStr) {
      try {
        const state = JSON.parse(stateStr);
        if (state.experiencesJson && experienceList.length === 0) {
          const exps = typeof state.experiencesJson === 'string' ? JSON.parse(state.experiencesJson) : state.experiencesJson;
          setExperienceList(exps);

          if (exps.length > 0) {
            const catId = exps[0].CategoryId || exps[0].categoryId;
            if (catId && reverseCategoryMap[catId]) {
              setPrimary(reverseCategoryMap[catId]);
            }
          }
        }
      } catch (e) {
        console.error("Error parsing state", e);
      }
    }
  }, []);

  const handlePrimarySelect = (skill) => {
    setPrimary(skill);
    if (secondary === skill) setSecondary(null);
  };

  const handleFinalSave = () => {
    if (experienceList.length === 0) {
      NotificationHelper.showError("Please add at least one experience before saving.");
      return;
    }

    const stateStr = sessionStorage.getItem('signupState');
    if (stateStr) {
      const state = JSON.parse(stateStr);
      state.experiencesJson = JSON.stringify(experienceList);
      sessionStorage.setItem('signupState', JSON.stringify(state));
    }

    const params = new URLSearchParams(searchParams.toString());
    params.set('skillsCompleted', 'true');
    params.delete('fromSignup');
    
    router.push(`/signup?${params.toString()}`);
  };

  return (
    <div className="min-h-screen bg-white font-sans">
      {/* Header Nav */}
      <div className="flex items-center justify-between p-[15px] border-b border-[#EEE] sticky top-0 bg-white z-10">
        <button onClick={() => router.back()} className="p-[5px] bg-[#F0F0F0] rounded-[20px] shadow-sm active:scale-95 transition-transform">
          <ArrowLeft size={24} color="#333" />
        </button>
        <h1 className="text-[18px] font-bold">Add Skills</h1>
        <div className="w-[40px]" /> 
      </div>

      <div className="max-w-3xl mx-auto p-[20px]">
        <h2 className="text-[16px] font-bold my-[15px] text-[#000]">Select Your Primary Skills</h2>
        <div className="flex justify-between mb-[20px]">
          <SkillBox icon={Brush} label="Cleaning" selected={primary === 'Cleaning'} onPress={() => handlePrimarySelect('Cleaning')} />
          <SkillBox icon={ChefHat} label="Cooking" selected={primary === 'Cooking'} onPress={() => handlePrimarySelect('Cooking')} />
          <SkillBox icon={Car} label="Driving" selected={primary === 'Driving'} onPress={() => handlePrimarySelect('Driving')} />
        </div>

        <h2 className="text-[16px] font-bold my-[15px] text-[#000]">
          Select Your Secondary Skills <span className="font-normal text-[#999] text-[12px]">(Optional)</span>
        </h2>
        <div className="flex justify-between mb-[20px]">
          <SkillBox icon={Brush} label="Cleaning" selected={secondary === 'Cleaning'} disabled={primary === 'Cleaning'} onPress={() => setSecondary('Cleaning')} />
          <SkillBox icon={ChefHat} label="Cooking" selected={secondary === 'Cooking'} disabled={primary === 'Cooking'} onPress={() => setSecondary('Cooking')} />
          <SkillBox icon={Car} label="Driving" selected={secondary === 'Driving'} disabled={primary === 'Driving'} onPress={() => setSecondary('Driving')} />
        </div>

        {primary && (
          <ExpertiseSection 
            title={primary} 
            icon={iconMap[primary]} 
            isActive={true} 
            categoryId={categoryMap[primary]} 
            onExperienceAdded={(exp) => setExperienceList([...experienceList, exp])}
          />
        )}
        
        {secondary && (
          <ExpertiseSection 
            title={secondary} 
            icon={iconMap[secondary]} 
            isActive={true} 
            categoryId={categoryMap[secondary]} 
            onExperienceAdded={(exp) => setExperienceList([...experienceList, exp])}
          />
        )}

        <button 
          onClick={handleFinalSave}
          className="w-full bg-[#1E64D3] h-[55px] rounded-[25px] flex items-center justify-center shadow-[0_4px_10px_rgba(0,0,0,0.2)] mt-[20px] mb-[30px] active:scale-95 transition-transform"
        >
          <span className="text-white text-[16px] font-bold">Save and Continue</span>
        </button>
      </div>
    </div>
  );
}