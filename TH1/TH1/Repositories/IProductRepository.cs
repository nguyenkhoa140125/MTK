using TH1.Models;

namespace TH1.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        // Kiểm tra xem sản phẩm còn đủ hàng trong kho không
        Task<bool> IsInStockAsync(int productId, int quantity);

        // Cập nhật số lượng tồn kho (giảm khi đặt hàng, tăng khi hủy hàng)
        Task UpdateStockAsync(int productId, int quantityChange);
    }
}