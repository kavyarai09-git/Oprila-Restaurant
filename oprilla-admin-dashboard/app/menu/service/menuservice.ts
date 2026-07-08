const API_URL = "http://localhost:5232/api";

export interface MenuItem {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  isAvailable: boolean;
}

export interface MenuCategory {
  id: number;
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  items: MenuItem[];
  actionText: string;
}

export async function getMenu(): Promise<MenuCategory[]> {
  const response = await fetch(`${API_URL}/menu`);

  if (!response.ok) {
    throw new Error("Failed to fetch menu");
  }

  const result = await response.json();

  return result.data;
}