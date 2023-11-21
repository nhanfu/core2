namespace Core.ViewModels
{
    public class ChatGptVM
    {
        public string model { get; set; }
        public List<ChatGptMessVM> messages { get; set; }
    }


    public class ChatGptMessVM
    {
        public string role { get; set; }
        public string content { get; set; }
        public string name { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
        public string finish_reason { get; set; }
        public string index { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class RsChatGpt
    {
        public string id { get; set; }
        public string @object { get; set; }
        public string created { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
    }

    public class Usage
    {
        public string prompt_tokens { get; set; }
        public string completion_tokens { get; set; }
        public string total_tokens { get; set; }
    }
}
