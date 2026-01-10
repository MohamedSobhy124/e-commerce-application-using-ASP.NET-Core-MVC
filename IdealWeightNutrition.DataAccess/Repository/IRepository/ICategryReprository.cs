using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface ICategryReprository :IRepository<Categry> 
    {
        void update(Categry obj);
       

    }
}
