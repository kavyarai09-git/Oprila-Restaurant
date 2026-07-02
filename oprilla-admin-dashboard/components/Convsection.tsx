import { useEffect, useState } from "react";
import ChatBubble from "./ChatBubble";
import {
  getConversationTranscript,
  ConversationTranscript,
} from "../Services/dashboard.service"; 

export default function ConversationSection() {
  const [conversations, setConversations] = useState<ConversationTranscript[]>([]);

  useEffect(() => {
    getConversationTranscript()
      .then((data: ConversationTranscript[]) => setConversations(data))
      .catch((error) => console.error(error));
  }, []);

  return (
    <div className="w-full bg-white border border-[#E6E1DA] rounded-xl p-4 md:p-6 overflow-hidden">
      <h2 className="text-lg md:text-xl font-bold text-[#1F2937] mb-4">
        Conversation Transcript
      </h2>

      <div className="space-y-3">
        {conversations.map((conversation) => (
          <ChatBubble
            key={conversation.id}
            sender={conversation.speaker}
            message={conversation.message}
          />
        ))}
      </div>
    </div>
  );
}