using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IComboOfferRepository : IRepository<ComboOffer>
    {
        void Update(ComboOffer comboOffer);
        void Remove(ComboOffer comboOffer);
        IEnumerable<ComboOffer> GetActiveComboOffers();
        ComboOffer? GetComboOfferWithItems(int id);
    }
}














