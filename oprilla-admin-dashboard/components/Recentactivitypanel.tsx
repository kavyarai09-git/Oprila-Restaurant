"use client";

import { useEffect, useState } from "react";
import {
  getRecentActivities,
  RecentActivity,
} from "../Services/dashboard.service";

export default function RecentActivityPanel() {
  const [activities, setActivities] = useState<RecentActivity[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadActivities() {
      try {
        const data = await getRecentActivities();
        setActivities(data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }

    loadActivities();
  }, []);

  if (loading) {
    return (
      <div className="w-[300px] p-6 bg-white rounded-2xl shadow">
        Loading...
      </div>
    );
  }

  return (
    <div className="w-[300px] bg-white rounded-2xl border border-[#ECE7E1] p-6">
      <h2 className="text-[34px] font-bold leading-9 text-[#111827]">
        Recent
        <br />
        Activity
      </h2>

      <p className="text-[12px] text-[#8B8B8B] mt-2 mb-6">
        AI Concierge logs for today
      </p>

      <div className="space-y-4">
        {activities.length === 0 ? (
          <p className="text-gray-500 text-sm">
            No recent activities
          </p>
        ) : (
          activities.map((activity) => (
            <div
              key={activity.id}
              className="bg-[#F9F5F0] rounded-xl p-4"
            >
              <h3 className="text-[24px] font-bold text-[#111827] leading-7">
                {activity.callerPhone}
              </h3>

              <div className="mt-2">
                <span
                  className={`text-[11px] font-semibold uppercase ${
                    activity.status === "Completed"
                      ? "text-green-600"
                      : activity.status === "Pending"
                      ? "text-yellow-600"
                      : activity.status === "Failed"
                      ? "text-red-600"
                      : "text-gray-600"
                  }`}
                >
                  {activity.status}
                </span>
              </div>

              <p className="text-[12px] text-[#6B7280] mt-2">
                Started:
                {" "}
                {new Date(activity.startedAt).toLocaleString()}
              </p>

              {activity.endedAt && (
                <p className="text-[12px] text-[#9CA3AF]">
                  Ended:
                  {" "}
                  {new Date(activity.endedAt).toLocaleString()}
                </p>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}