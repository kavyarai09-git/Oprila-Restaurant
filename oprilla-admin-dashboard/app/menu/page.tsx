"use client";

import { useState } from "react";

import Sidebar from "../../components/Sidebar";
import TopBar from "../components/TopBar";
import MenuHeader from "../components/MenuHeader";
import CategorySection from "../components/CategorySection";
import MainsSection from "../components/MainSection";

import { useMenu } from "./hooks/useMenu";

export default function MenuPage() {
  const { categories, loading } = useMenu();

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  if (loading) {
    return <div>Loading...</div>;
  }

  const starters = categories.find(
    (category) => category.name === "Starters"
  );

  const mains = categories.find(
    (category) => category.name === "Mains"
  );

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

          {starters && (
            <CategorySection items={starters.items} />
          )}

          {mains && (
            <MainsSection items={mains.items} />
          )}
        </div>
      </main>
    </div>
  );
}