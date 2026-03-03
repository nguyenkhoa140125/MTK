using Microsoft.EntityFrameworkCore;
using TH1.Data;
using TH1.Models;

namespace TH1.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly DataContext _context;

        public ProductRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            return product != null && product.Stock >= quantity;
        }

        public async Task UpdateStockAsync(int productId, int quantityChange)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.Stock += quantityChange; // quantityChange âm nếu là trừ kho
                if (product.Stock < 0) product.Stock = 0;
            }
        }
    }
}