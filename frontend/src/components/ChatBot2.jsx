// ChatBot.jsx
import React, { useState, useRef, useEffect } from "react";
import "./ChatBot.css";
import { Client } from "../../lib";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import "highlight.js/styles/github.css";

export default function ChatBot2() {
  const handleShow = (e) => {
    const bot = document.querySelector("zapier-interfaces-chatbot-embed");
    if (bot) bot.setAttribute("open", "true");
  };

  return (
    <div className="chatbot-wrapper">
      <button className="toggle-icon-button" onClick={() => handleShow(true)}>
        <i className="fas fa-robot fa-lg"></i>
      </button>
    </div>
  );
}
