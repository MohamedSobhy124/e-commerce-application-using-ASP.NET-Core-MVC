# Repository Files Location Guide

## ⚠️ Important: These files go in SEPARATE PROJECTS

Based on your project structure, you have:
- `BulkyBook.Models` project (for models)
- `BulkyBook.DataAccess` project (for repositories)

## 📁 Model Files Location

**Place these files in:** `BulkyBook.Models` project

The model files I created are in the `Models/` folder of this project, but they should be moved/copied to:
```
BulkyBook.Models/
  ├── ServiceSubscription.cs
  ├── ServiceOffer.cs
  └── ServicePurchase.cs
```

## 📁 Repository Files Location

**Place these files in:** `BulkyBook.DataAccess` project

### Repository Interfaces
**Location:** `BulkyBook.DataAccess/Repository/IRepository/`

Create these files:
1. `IServiceSubscriptionRepository.cs`
2. `IServiceOfferRepository.cs`
3. `IServicePurchaseRepository.cs`

### Repository Implementations
**Location:** `BulkyBook.DataAccess/Repository/`

Create these files:
1. `ServiceSubscriptionRepository.cs`
2. `ServiceOfferRepository.cs`
3. `ServicePurchaseRepository.cs`

## 📝 Files to Create

I've created the model files in the `Models/` folder. Below are the repository files you need to create:

---

## Repository Interface Files

### 1. IServiceSubscriptionRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/IRepository/IServiceSubscriptionRepository.cs`

```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceSubscriptionRepository : IRepository<ServiceSubscription>
    {
        void Update(ServiceSubscription obj);
    }
}
```

### 2. IServiceOfferRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/IRepository/IServiceOfferRepository.cs`

```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceOfferRepository : IRepository<ServiceOffer>
    {
        void Update(ServiceOffer obj);
    }
}
```

### 3. IServicePurchaseRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/IRepository/IServicePurchaseRepository.cs`

```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServicePurchaseRepository : IRepository<ServicePurchase>
    {
        void Update(ServicePurchase obj);
    }
}
```

---

## Repository Implementation Files

### 1. ServiceSubscriptionRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/ServiceSubscriptionRepository.cs`

```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServiceSubscriptionRepository : Repository<ServiceSubscription>, IServiceSubscriptionRepository
    {
        private ApplicationDBContext _db;
        public ServiceSubscriptionRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceSubscription obj)
        {
            _db.ServiceSubscriptions.Update(obj);
        }
    }
}
```

### 2. ServiceOfferRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/ServiceOfferRepository.cs`

```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServiceOfferRepository : Repository<ServiceOffer>, IServiceOfferRepository
    {
        private ApplicationDBContext _db;
        public ServiceOfferRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceOffer obj)
        {
            _db.ServiceOffers.Update(obj);
        }
    }
}
```

### 3. ServicePurchaseRepository.cs
**Location:** `BulkyBook.DataAccess/Repository/ServicePurchaseRepository.cs`

```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServicePurchaseRepository : Repository<ServicePurchase>, IServicePurchaseRepository
    {
        private ApplicationDBContext _db;
        public ServicePurchaseRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServicePurchase obj)
        {
            _db.ServicePurchases.Update(obj);
        }
    }
}
```

---

## Next Steps

1. **Copy model files** from `Models/` folder to `BulkyBook.Models` project
2. **Create repository interfaces** in `BulkyBook.DataAccess/Repository/IRepository/`
3. **Create repository implementations** in `BulkyBook.DataAccess/Repository/`
4. **Update IUnitOfWork** interface (add the three repository properties)
5. **Update UnitOfWork** class (implement the three repository properties)
6. **Update ApplicationDBContext** (add the three DbSet properties)

See `SERVICE_SUBSCRIPTION_SETUP_GUIDE.md` for complete instructions.

