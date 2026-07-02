export interface Appointment {
  id: number;
  customerName: string;
  customerPhone: string;
  guestCount: number;
  status: string;
  specialRequests?: string;
}

export interface RecentActivity {
  id: number;
  callerPhone: string;
  status: string;
  startedAt: string;
  endedAt?: string;
}



export interface ConversationTranscript {
  id: number;
  callHistoryId: number;
  speaker: string;
  message: string;
  createdAt: string;
}



const API_URL = "http://localhost:5232/api";

const TOKEN =
  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AcmVzdGF1cmFudC5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImp0aSI6ImZjYjUxODk0LTUwZGUtNGQ4Ni1hMzNmLWI5YzExNDkzZDZhNiIsIm5iZiI6MTc4Mjk4NDQyNiwiZXhwIjoxNzgzMDEzMjI2LCJpc3MiOiJSZXN0YXVyYW50QVBJIiwiYXVkIjoiUmVzdGF1cmFudEFQSUNsaWVudHMifQ.ugWuipwis2ZCsK7SUky4VpLRWJUu9u2njAstGJiwiDw";

export async function getAppointments(): Promise<Appointment[]> {
  try {
    const response = await fetch(
      `${API_URL}/admin/appointments`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${TOKEN}`,
          "Content-Type": "application/json",
        },
      }
    );

    if (!response.ok) {
      throw new Error("Failed to fetch appointments");
    }

    const result = await response.json();

    console.log("Appointments:", result);

    return result.data.items ?? result.data ?? [];
  } catch (error) {
    console.error("Appointments Error:", error);
    return [];
  }
}

export async function getRecentActivities(): Promise<RecentActivity[]> {
  try {
    const response = await fetch(
      `${API_URL}/admin/appointments/recent-activities`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${TOKEN}`,
          "Content-Type": "application/json",
        },
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP Error: ${response.status}`);
    }

    const result = await response.json();

    console.log("Recent Activities:", result);

    return result.data ?? [];
  } catch (error) {
    console.error("Recent Activities Error:", error);
    return [];
  }
}

export const getConversationTranscript = async () => {
  const response = await fetch(
   `${API_URL}/admin/appointments/conversation-transcript`,
  );

  if (!response.ok) {
    throw new Error("Failed to fetch conversation transcript");
  }

  return response.json();
};