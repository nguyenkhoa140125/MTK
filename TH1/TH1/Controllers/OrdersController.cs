using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TH1.DTOs;
using TH1.Services;
using TH1.Patterns.Singleton;
using TH1.Patterns.Facade;

namespace TH1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderFacade _orderFacade;

        public OrdersController(IOrderService orderService, IOrderFacade orderFacade)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _orderFacade = orderFacade ?? throw new ArgumentNullException(nameof(orderFacade));
            LoggerService.Instance.Log("OrdersController initialized.");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto createOrderDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            try
            {
                var result = await _orderFacade.PlaceOrderFullProcess(userId, createOrderDto);
                LoggerService.Instance.Log($"Order {result.Order.OrderId} created for user {userId}.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.Log($"Order creation failed for user {userId}: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("history")]    
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var orders = await _orderService.GetOrderHistory(userId);
            return Ok(orders);
        }

        [HttpGet("all")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllOrders()
        {
            LoggerService.Instance.Log($"Admin {User.Identity.Name} đang truy vấn toàn bộ danh sách đơn hàng.");

            // Gọi trực tiếp qua Service (hoặc Facade nếu bạn có logic tổng hợp phức tạp)
            var orders = await _orderService.GetAllOrders();

            if (orders == null || !orders.Any())
            {
                return NotFound("Không có đơn hàng nào trong hệ thống.");
            }

            return Ok(orders);
        }

        [HttpPost("checkout")]   
        public async Task<IActionResult> Checkout(CreateOrderDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Chỉ gọi 1 phương thức duy nhất từ Facade
            var result = await _orderFacade.PlaceOrderFullProcess(userId, dto);

            return Ok(result);
        }
    }
}
