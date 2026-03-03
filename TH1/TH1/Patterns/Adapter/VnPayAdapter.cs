namespace TH1.Patterns.Adapter
{
    public class VnPayAdapter : IPaymentService
    {
        private readonly VnPaySdk _vnPaySdk;

        public VnPayAdapter(VnPaySdk vnPaySdk)
        {
            _vnPaySdk = vnPaySdk;
        }

        public string Pay(decimal amount)
        {
            var money = (double)amount;

            // Gọi SDK giả lập để xử lý thanh toán
            var sdkMessage = _vnPaySdk.MakePayment(money);

            // Trả về thông báo thanh toán thành công thân thiện cho hệ thống
            return $"Thanh toán VNPay thành công với số tiền {amount:0.00}. ({sdkMessage})";
        }
    }
}

