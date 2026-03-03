using TH1.DTOs;
using TH1.Data;
using TH1.Models;
using TH1.Patterns.Adapter;
using TH1.Patterns.AbstractFactory;
using TH1.Patterns.Singleton;
using TH1.Repositories;
using TH1.Services;

namespace TH1.Patterns.Facade
{
    public interface IOrderFacade
    {
        Task<OrderResult> PlaceOrderFullProcess(int userId, CreateOrderDto dto);
    }

    public class OrderFacade : IOrderFacade
    {
        private readonly IOrderService _orderService;
        private readonly IProductRepository _productRepository;
        private readonly IPaymentService _paymentService;
        private readonly DataContext _dbContext;
        private readonly INotificationFactory _notificationFactory;

        public OrderFacade(
            IOrderService orderService,
            IProductRepository productRepository,
            IPaymentService paymentService,
            DataContext dbContext,
            INotificationFactory notificationFactory)
        {
            _orderService = orderService;
            _productRepository = productRepository;
            _paymentService = paymentService;
            _dbContext = dbContext;
            _notificationFactory = notificationFactory;
        }

        public async Task<OrderResult> PlaceOrderFullProcess(int userId, CreateOrderDto dto)
        {
            var steps = new List<string>();

            LoggerService.Instance.Log("Toàn bộ quy trình được quản lý bởi Facade");
            steps.Add("Facade Pattern: Toàn bộ quy trình đặt hàng được điều phối bởi OrderFacade.PlaceOrderFullProcess.");

            // 1. Kiểm tra kho bằng Repository mới cập nhật
            foreach (var item in dto.OrderItems)
            {
                if (!await _productRepository.IsInStockAsync(item.ProductId, item.Quantity))
                {
                    throw new Exception($"Sản phẩm ID {item.ProductId} không đủ hàng.");
                }
            }
            steps.Add("Repository Pattern: Đã kiểm tra tồn kho sản phẩm thông qua ProductRepository.");

            // 2. Tính tổng giá qua Decorator (OrderService được bọc bởi TaxOrderDecorator trong DI)
            var calculatedOrder = await _orderService.CreateOrder(userId, dto);
            LoggerService.Instance.Log("Đã tính giá qua Decorator (bao gồm thuế/giảm giá)");
            steps.Add("Builder Pattern: OrderBuilder đã tạo Order và OrderItems từ CreateOrderDto.");
            steps.Add("Decorator Pattern: TaxOrderDecorator đã tính TotalPrice (gồm thuế/giảm giá) dựa trên Order ban đầu.");

            // 3. Thanh toán thông qua Adapter
            var paymentResult = _paymentService.Pay(calculatedOrder.TotalPrice);
            LoggerService.Instance.Log("Đã gọi Adapter để kết nối VNPay");
            steps.Add($"Adapter Pattern: VnPayAdapter đã gọi VnPaySdk để thanh toán. Kết quả: {paymentResult}");

            if (string.IsNullOrWhiteSpace(paymentResult))
            {
                throw new Exception("Thanh toán thất bại.");
            }

            // 4. Lưu Orders + OrderItems vào DB thông qua DbContext
            var orderEntity = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalPrice = calculatedOrder.TotalPrice,
                ShippingAddress = dto.ShippingAddress,
                PaymentMethod = dto.PaymentMethod,
                OrderItems = dto.OrderItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.Orders.Add(orderEntity);
            await _dbContext.SaveChangesAsync();
            steps.Add("Persistence: Đã lưu Order và OrderItems vào database Th1Db (bảng Orders, OrderItems).");

            // 5. Trừ kho hàng thông qua Repository
            foreach (var item in dto.OrderItems)
            {
                await _productRepository.UpdateStockAsync(item.ProductId, -item.Quantity);
            }

            await _dbContext.SaveChangesAsync(); // Lưu tất cả thay đổi kho
            await transaction.CommitAsync();

            // 6. Gửi thông báo qua Abstract Factory
            var notification = _notificationFactory.CreateNotification();
            notification.SendOrderSuccessNotification("user@example.com"); // Demo: email giả lập
            var notificationMessage = "Abstract Factory: EmailNotificationFactory đã gửi email xác nhận đơn hàng.";
            steps.Add(notificationMessage);
            LoggerService.Instance.Log(notificationMessage);

            LoggerService.Instance.Log("Toàn bộ quy trình được quản lý bởi Facade");

            // 7. Trả về kết quả giàu thông tin cho UI
            var orderDto = new OrderDto
            {
                OrderId = orderEntity.OrderId,
                UserId = orderEntity.UserId,
                OrderDate = orderEntity.OrderDate,
                TotalPrice = orderEntity.TotalPrice,
                ShippingAddress = orderEntity.ShippingAddress,
                PaymentMethod = orderEntity.PaymentMethod,
                OrderItems = orderEntity.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            return new OrderResult
            {
                Order = orderDto,
                Steps = steps,
                PaymentMessage = paymentResult,
                NotificationMessage = notificationMessage
            };
        }
    }
}
