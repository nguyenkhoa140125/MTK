using TH1.DTOs;
using TH1.Models;
using TH1.Repositories;
using TH1.Patterns.Builder;
using TH1.Patterns.FactoryMethod;
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

        public async Task<OrderDto> CreateOrder(int userId, CreateOrderDto createOrderDto)
        {
            // Use Builder to create the order
            var order = _orderBuilder
                .SetCustomerInfo(userId)
                .SetShippingAddress(createOrderDto.ShippingAddress)
                .SetPaymentMethod(createOrderDto.PaymentMethod)
                .SetOrderItems(createOrderDto.OrderItems)
                .Build();

            // Use Factory Method to process payment
            PaymentServiceFactory paymentFactory = createOrderDto.PaymentMethod.ToLower() switch
            {
                "cash" => new CashPaymentFactory(),
                "paypal" => new PaypalPaymentFactory(),
                "vnpay" => new VNPayPaymentFactory(),
                _ => throw new NotSupportedException("Payment method not supported")
            };
            var paymentService = paymentFactory.CreatePaymentService();
            var paymentResult = paymentService.ProcessPayment(order.TotalPrice);
            LoggerService.Instance.Log(paymentResult);


            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // Use Abstract Factory to send notification
            var notification = _notificationFactory.CreateNotification();
            notification.SendOrderSuccessNotification("user@example.com"); // In a real app, get user's email
            LoggerService.Instance.Log($"Order {order.OrderId} created successfully for user {userId}.");

            return new OrderDto
            {
                OrderId = order.OrderId,
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
            };
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
