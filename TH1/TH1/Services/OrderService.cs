using TH1.DTOs;
using TH1.Models;
using TH1.Repositories;
using TH1.Patterns.Builder;
using TH1.Patterns.AbstractFactory;
using TH1.Patterns.Singleton;

namespace TH1.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderBuilder _orderBuilder;
        private readonly INotificationFactory _notificationFactory;


        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IOrderBuilder orderBuilder, INotificationFactory notificationFactory)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _orderBuilder = orderBuilder;
            _notificationFactory = notificationFactory;
        }

        public Task<OrderDto> CreateOrder(int userId, CreateOrderDto createOrderDto)
        {
            var order = _orderBuilder
                .SetCustomerInfo(userId)
                .SetShippingAddress(createOrderDto.ShippingAddress)
                .SetPaymentMethod(createOrderDto.PaymentMethod)
                .SetOrderItems(createOrderDto.OrderItems)
                .Build();

            // Trả về đơn hàng đã tính tổng (Decorator sẽ bọc thêm VAT nếu được cấu hình).
            return Task.FromResult(new OrderDto
            {
                OrderId = 0,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });
        }

        public async Task<IEnumerable<OrderDto>> GetOrderHistory(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                ShippingAddress = o.ShippingAddress,
                PaymentMethod = o.PaymentMethod,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrders()
        {
            var orders = await _orderRepository.GetAllOrders(); 
            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                ShippingAddress = o.ShippingAddress,
                PaymentMethod = o.PaymentMethod,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });
        }
    }
}
