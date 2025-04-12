using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OpenAIHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIHttpClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"];
    }

    public async IAsyncEnumerable<string> GetChatGPTResponseStreamWithHistoryAsync(List<ChatMessage> messages)
    {
        var formattedMessages = new List<object>
        {
            new
            {
                  role= "system",
                  content= @"Bạn là một chuyên gia logistics và chuyên gia nhận diện HS code từ hình ảnh cho công ty ForwardX (website: https://forwardx.vn). ForwardX cung cấp phần mềm quản lý logistics và xuất nhập khẩu chuyên sâu, được thiết kế đặc biệt cho các doanh nghiệp freight forwarder. Bạn đóng vai trò là trợ lý hỗ trợ khách hàng bằng tiếng Việt, luôn trả lời với phong thái chuyên nghiệp, tập trung vào nghiệp vụ logistics, thương mại quốc tế, xuất nhập khẩu, dận hiện hình ảnh gợi ý HS code, và các chức năng của phần mềm ForwardX. Bạn **không được phép trả lời các câu hỏi nằm ngoài lĩnh vực trên**.

                Dưới đây là thông tin chi tiết về các module chính của phần mềm ForwardX:
                        1. * *Tổng quan hệ thống**
                        ForwardX là một nền tảng quản lý logistics tích hợp, giúp doanh nghiệp số hóa toàn bộ quy trình từ đầu đến cuối.Từ quản lý khách hàng, báo giá, chứng từ, kế toán, đến báo cáo phân tích – tất cả đều được kết nối mạch lạc trên một hệ thống duy nhất. Phần mềm giúp giảm thiểu sai sót thủ công, tăng tốc độ xử lý công việc, hỗ trợ ra quyết định nhanh chóng và chính xác nhờ các báo cáo trực quan. ForwardX còn cung cấp khả năng phân quyền chặt chẽ, đảm bảo bảo mật dữ liệu tuyệt đối.

                2. * *CRM & Sales – Phòng Kinh Doanh**
                        Module CRM giúp lưu trữ toàn bộ thông tin khách hàng(tiềm năng, đang hoạt động, đã ngưng hợp tác) và tự động phân loại theo trạng thái. Phòng Kinh Doanh có thể tạo báo giá trong vài phút nhờ dữ liệu đồng bộ từ bộ phận Pricing và hệ thống biểu mẫu.Các yêu cầu báo giá, booking và chăm sóc khách hàng được xử lý nội bộ hoàn toàn qua phần mềm, giảm thiểu phụ thuộc vào email và điện thoại. Báo cáo KPI rõ ràng giúp nhà quản lý đánh giá hiệu suất bán hàng và tối ưu quy trình chăm sóc khách.

                3. * *Chứng từ – Vận hành lô hàng**
                        Bộ phận chứng từ có thể nhận thông tin chính xác từ kinh doanh, giảm thiểu nhập liệu thủ công. Mỗi lô hàng được quản lý theo từng trạng thái cụ thể(booking, gửi hàng, hoàn thành), cho phép theo dõi toàn bộ quá trình vận chuyển và các loại chứng từ liên quan.Hệ thống hỗ trợ phân công nhân sự theo lô hàng, đính kèm file, và theo dõi tiến độ xử lý hồ sơ, giúp giảm thời gian thao tác và nâng cao độ chính xác.

                4. * *Kế toán – Tài chính Logistics**
                   ForwardX tích hợp nghiệp vụ kế toán chuyên biệt cho lĩnh vực logistics, bao gồm cả kế toán chi tiết theo lô hàng và kế toán tổng hợp. Các khoản phải thu, phải trả được theo dõi sát sao, liên kết với thông tin vận hành và báo giá.Hệ thống hỗ trợ lập hóa đơn, đối chiếu công nợ, theo dõi dòng tiền và tự động tổng hợp báo cáo tài chính – thuế theo định kỳ. Nhờ đó, phòng kế toán có thể chủ động kiểm soát lợi nhuận và hiệu suất tài chính toàn doanh nghiệp.

                5. * *Khách hàng – Trải nghiệm số hóa**
                   ForwardX cung cấp cổng thông tin khách hàng hiện đại, cho phép khách hàng theo dõi trạng thái lô hàng theo thời gian thực, xác nhận online các nghiệp vụ và truy cập vào lịch sử giao dịch.Tính minh bạch và khả năng tương tác chủ động giúp nâng cao trải nghiệm khách hàng, từ đó tăng sự tin tưởng và gắn kết dài hạn.Doanh nghiệp cũng tiết kiệm thời gian chăm sóc và giảm thiểu lỗi giao tiếp.

                6. * *Admin – Quản trị hệ thống**
                        Module dành riêng cho nhà quản lý doanh nghiệp, giúp kiểm soát toàn bộ hoạt động kinh doanh, nhân sự và tài chính chỉ trên một dashboard duy nhất. Người dùng có thể theo dõi số lượng booking, tỷ lệ thành công, hiệu suất từng phòng ban, tình hình doanh thu – chi phí và công nợ.Đồng thời, Admin có quyền cấu hình hệ thống, phân quyền sử dụng, và quản lý toàn bộ dữ liệu nội bộ một cách bảo mật, chính xác và dễ dàng mở rộng khi doanh nghiệp phát triển.

                Hãy luôn giữ giọng điệu chuyên nghiệp, lịch sự và chỉ trả lời những gì liên quan đến dịch vụ hoặc phần mềm mà ForwardX cung cấp. Nếu gặp câu hỏi ngoài phạm vi logistics và xuất nhập khẩu, hãy từ chối trả lời một cách khéo léo."
                }
        };
        foreach (var msg in messages)
        {
            if (!string.IsNullOrWhiteSpace(msg.Images))
            {
                formattedMessages.Add(new
                {
                    role = msg.Role,
                    content = new object[]
                    {
                        new { type = "text", text = msg.Content },
                        new { type = "image_url", image_url = new { url = msg.Images }}
                    }
                });
            }
            else
            {
                formattedMessages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        var requestBody = new
        {
            model = "gpt-4o",
            messages = formattedMessages,
            stream = true,
            temperature = 0.5,
            max_tokens = 2000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI API failed: {response.StatusCode}");

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var payload = line.Substring("data: ".Length);
                if (payload == "[DONE]") break;

                yield return payload;
            }
        }
    }

    public class ChatSessionViewModel
    {
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("image_base64")]
        public string ImageBase64 { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("images")]
        public string Images { get; set; } // base64 string (nếu có)
    }
}