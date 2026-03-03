using TH1.DTOs;

namespace TH1.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrder(int userId, CreateOrderDto createOrderDto);
        Task<IEnumerable<OrderDto>> GetOrderHistory(int userId);
        Task<IEnumerable<OrderDto>> GetAllOrders();
    }
}
