using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IComboOfferItemRepository : IRepository<ComboOfferItem>
    {
        void Update(ComboOfferItem comboOfferItem);
        void Remove(ComboOfferItem comboOfferItem);
        IEnumerable<ComboOfferItem> GetItemsByComboOfferId(int comboOfferId);
        IEnumerable<ComboOfferItem> GetItemsWithProducts(int comboOfferId);
    }
}














