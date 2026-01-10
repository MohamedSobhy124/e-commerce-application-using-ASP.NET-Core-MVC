using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.Services
{
    public interface IStockService
    {
        /// <summary>
        /// Decreases stock for products in an order and sends notifications if needed
        /// </summary>
        Task ProcessOrderStockDeduction(int orderId);
        
        /// <summary>
        /// Checks if stock is low or out and sends notifications
        /// </summary>
        Task CheckAndNotifyStockLevels(int productId);
        
        /// <summary>
        /// Manually decrease stock for a product
        /// </summary>
        Task<bool> DecreaseStock(int productId, int quantity);
        
        /// <summary>
        /// Manually increase stock for a product (e.g., returns/refunds)
        /// </summary>
        Task<bool> IncreaseStock(int productId, int quantity);
        
        /// <summary>
        /// Restores stock for products in a return request (reverse of ProcessOrderStockDeduction)
        /// </summary>
        Task ProcessReturnStockRestoration(int returnRequestId);
    }
}

