using TH1.DTOs;
using TH1.Patterns.Singleton;
using TH1.Services;

namespace TH1.Patterns.Decorator
{
    // 1. Interface cho Decorator
    public interface IOrderServiceDecorator : IOrderService { }

    // 2. Lớp Decorator cơ bản
    public abstract class OrderDecorator : IOrderServiceDecorator
    {
        protected readonly IOrderService _innerService;
        public OrderDecorator(IOrderService innerService) => _innerService = innerService;

        public virtual async Task<OrderDto> CreateOrder(int userId, CreateOrderDto dto)
            => await _innerService.CreateOrder(userId, dto);

        public virtual async Task<IEnumerable<OrderDto>> GetOrderHistory(int userId)
            => await _innerService.GetOrderHistory(userId);

        public virtual async Task<IEnumerable<OrderDto>> GetAllOrders()
        {
            var orders = await _innerService.GetAllOrders();
            return orders;
        }
    }

    // 3. Decorator cụ thể: Thêm thuế VAT 10%
    public class TaxOrderDecorator : OrderDecorator
    {
        public TaxOrderDecorator(IOrderService innerService) : base(innerService) { }

        public override async Task<OrderDto> CreateOrder(int userId, CreateOrderDto dto)
        {
            var order = await base.CreateOrder(userId, dto);
            order.TotalPrice *= 1.1m; // Cộng 10% thuế (use decimal literal to match TotalPrice type)
            LoggerService.Instance.Log($"Đã áp dụng thuế VAT cho đơn hàng {order.OrderId}");
            return order;
        }
    }
}
