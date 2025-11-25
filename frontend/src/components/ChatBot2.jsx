// ChatBot.jsx
import "./ChatBot.css";
import "highlight.js/styles/github.css";

export default function ChatBot2() {
  const handleShow = (_e) => {
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
