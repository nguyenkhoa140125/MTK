using TH1.DTOs;
using TH1.Repositories;
using TH1.Services;

namespace TH1.Patterns.Facade
{
    public interface IOrderFacade
    {
        Task<OrderDto> PlaceOrderFullProcess(int userId, CreateOrderDto dto);
    }

    public class OrderFacade : IOrderFacade
    {
        private readonly IOrderService _orderService;
        private readonly IProductRepository _productRepository;

        public OrderFacade(IOrderService orderService, IProductRepository productRepository)
        {
            _orderService = orderService;
            _productRepository = productRepository;
        }

        public async Task<OrderDto> PlaceOrderFullProcess(int userId, CreateOrderDto dto)
        {
            // 1. Kiểm tra kho bằng Repository mới cập nhật
            foreach (var item in dto.OrderItems)
            {
                if (!await _productRepository.IsInStockAsync(item.ProductId, item.Quantity))
                {
                    throw new Exception($"Sản phẩm ID {item.ProductId} không đủ hàng.");
                }
            }

            // 2. Thực hiện tạo đơn hàng (Sử dụng OrderService đã có Decorator)
            var result = await _orderService.CreateOrder(userId, dto);

            // 3. Trừ kho hàng thông qua Repository
            foreach (var item in dto.OrderItems)
            {
                await _productRepository.UpdateStockAsync(item.ProductId, -item.Quantity);
            }

            await _productRepository.SaveChangesAsync(); // Lưu tất cả thay đổi kho

            return result;
        }
    }
}
