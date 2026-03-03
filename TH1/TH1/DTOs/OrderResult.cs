using System.Collections.Generic;

namespace TH1.DTOs
{
    /// <summary>
    /// Kết quả đặt hàng dùng để hiển thị rõ các bước Pattern trên UI.
    /// </summary>
    public class OrderResult
    {
        public OrderDto Order { get; set; } = new OrderDto();

        /// <summary>
        /// Danh sách các bước/bản mô tả Pattern đã thực hiện (Facade, Builder, Decorator, Adapter, Abstract Factory,...).
        /// </summary>
        public List<string> Steps { get; set; } = new List<string>();

        public string PaymentMessage { get; set; } = string.Empty;

        public string NotificationMessage { get; set; } = string.Empty;
    }
}

