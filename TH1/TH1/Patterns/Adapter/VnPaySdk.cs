namespace TH1.Patterns.Adapter
{
    // Giả lập SDK bên thứ 3 (không cùng interface với hệ thống)
    public class VnPaySdk
    {
        public string MakePayment(double money)
        {
            if (money <= 0)
            {
                throw new InvalidOperationException("VNPay payment amount must be greater than 0.");
            }

            return $"VNPay SDK processed payment: {money:0.00}";
        }
    }
}

