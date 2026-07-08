"use client";

import { useEffect, useState } from "react";
import { getMenu, MenuCategory } from "../service/menuservice";

export function useMenu() {
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchMenu() {
      try {
        const data = await getMenu();
        setCategories(data);
      } catch (error) {
        console.error("Failed to fetch menu:", error);
      } finally {
        setLoading(false);
      }
    }

    fetchMenu();
  }, []);

  return {
    categories,
    loading,
  };
}