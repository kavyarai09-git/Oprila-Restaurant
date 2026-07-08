"use client";

import { useState } from "react";

import Sidebar from "../../components/Sidebar";
import TopBar from "../components/TopBar";
import MenuHeader from "../components/MenuHeader";
import CategorySection from "../components/CategorySection";

import { useMenu } from "./hooks/useMenu";

export default function MenuPage() {
  const { categories, loading } = useMenu();

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="min-h-screen bg-[#F7F6F3] xl:flex">
      {/* Sidebar */}
      <Sidebar
        isOpen={isSidebarOpen}
        setIsOpen={setIsSidebarOpen}
      />

      {/* Main Content */}
      <main className="flex-1 overflow-y-auto">
        <TopBar
          onMenuClick={() => setIsSidebarOpen(true)}
        />

        <div className="p-4 md:p-6">
          <MenuHeader />

          {categories.map((category) => (
  <CategorySection
    key={category.id}
    name={category.name}
    items={category.items}
  />
))}
        </div>
      </main>
    </div>
  );
}