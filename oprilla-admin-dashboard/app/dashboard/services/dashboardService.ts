const API_URL = "http://localhost:5232/api";

export const getRecentActivities = async () => {
  const res = await fetch(`${API_URL}/dashboard/recent-activity`);

  if (!res.ok) {
    throw new Error("Failed to fetch recent activities");
  }

  const json = await res.json();

  // backend response: { success, message, data }
  return json.data;
};