// ChatBot.jsx
import React, { useState, useRef, useEffect } from "react";
import "./ChatBot.css";
import { Client } from "../../lib";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import "highlight.js/styles/github.css";

export default function ChatBot() {
  const [showChat, setShowChat] = useState(false);
  const [userInput, setUserInput] = useState("");
  const [messages, setMessages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedImage, setSelectedImage] = useState(null);
  const fileInputRef = useRef(null);
  const responseRef = useRef(null);

  useEffect(() => {
    if (responseRef.current) {
      responseRef.current.scrollTop = responseRef.current.scrollHeight;
    }
  }, [messages]);

  const handleImageUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onloadend = () => {
      setSelectedImage(reader.result);
    };
    reader.readAsDataURL(file);
  };

  const removeImage = () => {
    setSelectedImage(null);
    fileInputRef.current.value = "";
  };

  const handleSend = async () => {
    if (!userInput.trim() && !selectedImage) return;

    const newMessage = {
      role: "user",
      content: userInput,
      ...(selectedImage && { images: selectedImage }), // để backend xử lý images
    };

    const newMessages = [...messages, newMessage];
    setMessages(newMessages);
    setUserInput("");
    setSelectedImage(null);
    setLoading(true);

    try {
      const response = await fetch(
        Client.api + "/api/DeepSeek/chat-stream-gpt",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${Client.Token?.AccessToken}`,
          },
          body: JSON.stringify({ messages: newMessages }),
        }
      );

      const reader = response.body.getReader();
      const decoder = new TextDecoder("utf-8");
      let done = false;
      let buffer = "";
      fileInputRef.current.value = "";
      while (!done) {
        const { value, done: doneReading } = await reader.read();
        done = doneReading;

        buffer += decoder.decode(value || new Uint8Array(), { stream: true });

        const lines = buffer.split("\n");
        buffer = lines.pop() || ""; // giữ lại dòng chưa trọn vẹn

        for (let line of lines) {
          line = line.trim();
          if (!line.startsWith("data:")) continue;

          const content = line.slice("data:".length).trim();
          if (content === "[DONE]") return;

          try {
            const parsed = JSON.parse(content);
            const delta = parsed?.choices?.[0]?.delta;

            if (delta?.role === "assistant") {
              setMessages((prev) => [
                ...prev,
                { role: "assistant", content: "" },
              ]);
            }

            if (delta?.content) {
              setMessages((prev) => {
                const updated = [...prev];
                const last = updated[updated.length - 1];
                if (last?.role === "assistant") {
                  last.content += delta.content;
                } else {
                  updated.push({ role: "assistant", content: delta.content });
                }
                return updated;
              });
            }
          } catch (err) {
            console.error("❌ JSON parse error:", err.message);
            console.warn("🚨 Raw content:", content);
          }
        }
      }
    } catch (error) {
      console.error("API call failed:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="chatbot-wrapper">
      {!showChat && (
        <button
          className="toggle-icon-button"
          onClick={() => setShowChat(true)}
        >
          <i className="fas fa-robot fa-lg"></i>
        </button>
      )}
      {showChat && (
        <div className="chatbot-container">
          <div className="chatbot-header">
            <span className="chatbot-title">ForwardX AI</span>
            <button className="close-button" onClick={() => setShowChat(false)}>
              <i className="fas fa-times"></i>
            </button>
          </div>

          <div className="chatbot-response" ref={responseRef}>
            {messages.map((msg, idx) => (
              <div key={idx} className={`chat-message ${msg.role}`}>
                {msg.images && (
                  <div className="message-image">
                    <img
                      src={msg.images}
                      alt="Uploaded"
                      style={{ maxWidth: "100%", maxHeight: "200px" }}
                    />
                  </div>
                )}
                <ReactMarkdown
                  children={msg.content}
                  remarkPlugins={[remarkGfm]}
                  rehypePlugins={[rehypeHighlight]}
                />
              </div>
            ))}
            {loading && (
              <div className="chat-message assistant">
                <span className="typing-indicator">...</span>
              </div>
            )}
          </div>

          <div className="chatbot-input-box">
            {selectedImage && (
              <div className="image-preview">
                <img
                  src={selectedImage}
                  alt="Preview"
                  style={{ maxWidth: "53px", maxHeight: "53px" }}
                />
                <button onClick={removeImage} className="remove-image-btn">
                  <i className="fas fa-times"></i>
                </button>
              </div>
            )}

            <div className="input-controls">
              <input
                type="file"
                accept="image/*"
                onChange={handleImageUpload}
                ref={fileInputRef}
                style={{ display: "none" }}
              />
              <button
                className="upload-btn"
                onClick={() => fileInputRef.current.click()}
              >
                <i className="fas fa-image"></i>
              </button>

              <input
                type="text"
                className="chatbot-input"
                placeholder="Type your message..."
                value={userInput}
                onChange={(e) => setUserInput(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleSend()}
                disabled={loading}
              />

              <button
                className="send-button"
                onClick={handleSend}
                disabled={loading}
              >
                {loading ? (
                  <span>Responding…</span>
                ) : (
                  <i className="far fa-chevron-right"></i>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
