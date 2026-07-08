"use client";

import { Bell, Moon, Plus, Menu } from "lucide-react";
import { Wand2 } from "lucide-react";

interface TopBarProps {
  onMenuClick: () => void;
}

export default function TopBar({ onMenuClick }: TopBarProps) {
  return (
    <div className="h-16 bg-white border-b flex items-center justify-between px-4 lg:px-8">

      {/* Left Side */}
      <div className="flex items-center gap-4">
        {/* Hamburger - Mobile & Tablet */}
        <button
          onClick={onMenuClick}
          className="xl:hidden text-[#B36A2E]"
        >
          <Menu size={28} />
        </button>

        {/* Logo */}
        <div className="xl:hidden">
          <h1 className="text-xl font-serif font-semibold">
            Maître D' Pro
          </h1>
          <p className="text-[10px] tracking-[3px] uppercase text-gray-400">
            PREMIUM ADMIN
          </p>
        </div>
      </div>

      {/* Right Side */}
      <div className="flex items-center gap-4">
        <div className="relative cursor-pointer">
          <Bell className="w-5 h-5 text-[#1F1F1F]" />
          <span className="absolute -top-1 -right-1 w-2.5 h-2.5 bg-red-500 rounded-full border border-white"></span>
        </div>

        <Moon className="w-5 h-5 text-[#1F1F1F] cursor-pointer" />

        {/* Desktop Buttons */}
        <button className="hidden xl:flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded-md text-black font-medium">
          <Plus className="w-4 h-4" />
          Add Category
        </button>

        <button className="hidden xl:flex items-center gap-2 h-10 px-5 bg-black text-white rounded-lg text-sm font-medium">
          <Wand2 className="w-4 h-4" />
          Add New Item
        </button>
      </div>
    </div>
  );
}