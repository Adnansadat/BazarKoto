using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BazarKoto.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static async Task SeedAsync(BazarKotoDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher, bool seedDemoData = false, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);
        await SeedAdminAsync(dbContext, configuration, passwordHasher, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (seedDemoData)
        {
            await SeedDevelopmentDemoDataAsync(dbContext, configuration, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCategoriesAsync(BazarKotoDbContext dbContext, CancellationToken cancellationToken)
    {
        await EnsureCategoryAsync(dbContext, "Rice", "চাল", "rice", "Rice and staple grains", "চাল ও প্রধান খাদ্যশস্য", 1, cancellationToken);
        await EnsureCategoryAsync(dbContext, "Vegetables", "সবজি", "vegetables", "Fresh vegetables", "তাজা সবজি", 2, cancellationToken);
        await EnsureCategoryAsync(dbContext, "Fish", "মাছ", "fish", "Fresh and frozen fish", "তাজা ও হিমায়িত মাছ", 3, cancellationToken);
        await EnsureCategoryAsync(dbContext, "Meat", "মাংস", "meat", "Meat and poultry", "মাংস ও পোলট্রি", 4, cancellationToken);
        await EnsureCategoryAsync(dbContext, "Groceries", "মুদি পণ্য", "groceries", "Packaged grocery items", "প্যাকেটজাত মুদি পণ্য", 5, cancellationToken);
    }

    private static async Task SeedAdminAsync(BazarKotoDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Email = email,
            PasswordHash = passwordHasher.HashPassword(password),
            Role = UserRole.Admin,
            IsActive = true
        });
    }

    private static async Task SeedDevelopmentDemoDataAsync(BazarKotoDbContext dbContext, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["AdminSeed:Email"];
        var adminUser = string.IsNullOrWhiteSpace(adminEmail)
            ? null
            : await dbContext.Users.FirstOrDefaultAsync(x => x.Email == adminEmail, cancellationToken);

        var vegetables = await EnsureCategoryAsync(dbContext, "Vegetables", "সবজি", "vegetables", "Fresh vegetables", "তাজা সবজি", 2, cancellationToken);
        var riceCategory = await EnsureCategoryAsync(dbContext, "Rice", "চাল", "rice", "Rice and staple grains", "চাল ও প্রধান খাদ্যশস্য", 1, cancellationToken);
        var division = await EnsureDivisionAsync(dbContext, "Dhaka", "ঢাকা", "dhaka", "30", cancellationToken);
        var district = await EnsureDistrictAsync(dbContext, division, "Tangail", "টাঙ্গাইল", "tangail", "93", cancellationToken);
        var upazila = await EnsureUpazilaAsync(dbContext, district, "Tangail Sadar", "টাঙ্গাইল সদর", "tangail-sadar", "95", cancellationToken);
        var ward = await EnsureUnionOrWardAsync(dbContext, upazila, "Ward 6", "৬ নম্বর ওয়ার্ড", "ward-6", "06", UnionOrWardType.Ward, cancellationToken);

        var approvedMarket = await EnsureMarketAsync(
            dbContext,
            division,
            district,
            upazila,
            ward,
            "Park Bazar",
            RecordStatus.Approved,
            "Victoria Road",
            "Tangail Central Park area",
            cancellationToken);

        var pendingMarket = await EnsureMarketAsync(
            dbContext,
            division,
            district,
            upazila,
            ward,
            "Nirala Mor Bazar",
            RecordStatus.Pending,
            "Nirala Mor",
            "Near Nirala Mor bus stand",
            cancellationToken);

        var potato = await EnsureProductAsync(
            dbContext,
            "Potato",
            "Alu",
            "আলু",
            "potato",
            vegetables,
            "kg",
            ProductState.Fresh,
            RecordStatus.Approved,
            cancellationToken);

        var onion = await EnsureProductAsync(
            dbContext,
            "Onion",
            "Peyaj",
            "পেঁয়াজ",
            "onion",
            vegetables,
            "kg",
            ProductState.Fresh,
            RecordStatus.Approved,
            cancellationToken);

        var miniketRice = await EnsureProductAsync(
            dbContext,
            "Miniket Rice",
            "Miniket Chal",
            "মিনিকেট চাল",
            "miniket-rice",
            riceCategory,
            "kg",
            ProductState.Dry,
            RecordStatus.Approved,
            cancellationToken);

        await EnsureContributorAsync(dbContext, cancellationToken);
        await EnsurePriceSubmissionAsync(dbContext, approvedMarket, potato, 45m, SubmissionStatus.Approved, adminUser, cancellationToken);
        await EnsurePriceSubmissionAsync(dbContext, pendingMarket, onion, 80m, SubmissionStatus.Pending, null, cancellationToken);
        await EnsurePriceSubmissionAsync(dbContext, approvedMarket, miniketRice, 68m, SubmissionStatus.Approved, adminUser, cancellationToken);
        await EnsureContactMessageAsync(dbContext, cancellationToken);
        await EnsurePageVisitsAsync(dbContext, cancellationToken);
        await EnsureAdminAuditLogAsync(dbContext, adminUser, approvedMarket, cancellationToken);
        await EnsureAdMetricAsync(dbContext, cancellationToken);
    }

    private static async Task<ProductCategory> EnsureCategoryAsync(
        BazarKotoDbContext dbContext,
        string nameEn,
        string nameBn,
        string slug,
        string descriptionEn,
        string descriptionBn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (category is not null)
        {
            return category;
        }

        category = new ProductCategory
        {
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            DescriptionEn = descriptionEn,
            DescriptionBn = descriptionBn,
            SortOrder = sortOrder,
            IsActive = true
        };

        dbContext.ProductCategories.Add(category);
        return category;
    }

    private static async Task<Market> EnsureMarketAsync(
        BazarKotoDbContext dbContext,
        Division division,
        District district,
        Upazila upazila,
        UnionOrWard? unionOrWard,
        string marketName,
        RecordStatus status,
        string area,
        string landmark,
        CancellationToken cancellationToken)
    {
        var market = await dbContext.Markets.FirstOrDefaultAsync(x => x.MarketName == marketName, cancellationToken);

        if (market is not null)
        {
            return market;
        }

        market = new Market
        {
            DivisionId = division.Id,
            Division = division,
            DistrictId = district.Id,
            District = district,
            UpazilaId = upazila.Id,
            Upazila = upazila,
            UnionOrWardId = unionOrWard?.Id,
            UnionOrWard = unionOrWard,
            Area = area,
            MarketName = marketName,
            VillageOrMoholla = "Tangail Sadar",
            Landmark = landmark,
            MarketType = MarketType.Retail,
            OperatingSchedule = OperatingSchedule.Daily,
            Status = status
        };

        dbContext.Markets.Add(market);
        return market;
    }

    private static async Task<Division> EnsureDivisionAsync(
        BazarKotoDbContext dbContext,
        string nameEn,
        string nameBn,
        string slug,
        string? bbsCode,
        CancellationToken cancellationToken)
    {
        var division = await dbContext.Divisions.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (division is not null)
        {
            return division;
        }

        division = new Division
        {
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            BbsCode = bbsCode,
            IsActive = true
        };

        dbContext.Divisions.Add(division);
        return division;
    }

    private static async Task<District> EnsureDistrictAsync(
        BazarKotoDbContext dbContext,
        Division division,
        string nameEn,
        string nameBn,
        string slug,
        string? bbsCode,
        CancellationToken cancellationToken)
    {
        var district = await dbContext.Districts.FirstOrDefaultAsync(x => x.DivisionId == division.Id && x.Slug == slug, cancellationToken);

        if (district is not null)
        {
            return district;
        }

        district = new District
        {
            DivisionId = division.Id,
            Division = division,
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            BbsCode = bbsCode,
            IsActive = true
        };

        dbContext.Districts.Add(district);
        return district;
    }

    private static async Task<Upazila> EnsureUpazilaAsync(
        BazarKotoDbContext dbContext,
        District district,
        string nameEn,
        string nameBn,
        string slug,
        string? bbsCode,
        CancellationToken cancellationToken)
    {
        var upazila = await dbContext.Upazilas.FirstOrDefaultAsync(x => x.DistrictId == district.Id && x.Slug == slug, cancellationToken);

        if (upazila is not null)
        {
            return upazila;
        }

        upazila = new Upazila
        {
            DistrictId = district.Id,
            District = district,
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            BbsCode = bbsCode,
            IsActive = true
        };

        dbContext.Upazilas.Add(upazila);
        return upazila;
    }

    private static async Task<UnionOrWard> EnsureUnionOrWardAsync(
        BazarKotoDbContext dbContext,
        Upazila upazila,
        string nameEn,
        string nameBn,
        string slug,
        string? bbsCode,
        UnionOrWardType type,
        CancellationToken cancellationToken)
    {
        var unionOrWard = await dbContext.UnionOrWards.FirstOrDefaultAsync(x => x.UpazilaId == upazila.Id && x.Slug == slug, cancellationToken);

        if (unionOrWard is not null)
        {
            return unionOrWard;
        }

        unionOrWard = new UnionOrWard
        {
            UpazilaId = upazila.Id,
            Upazila = upazila,
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            BbsCode = bbsCode,
            Type = type,
            IsActive = true
        };

        dbContext.UnionOrWards.Add(unionOrWard);
        return unionOrWard;
    }

    private static async Task<Product> EnsureProductAsync(
        BazarKotoDbContext dbContext,
        string nameEn,
        string legacyLocalName,
        string nameBn,
        string slug,
        ProductCategory category,
        string primaryUnit,
        ProductState productState,
        RecordStatus status,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.CategoryId == category.Id && x.Slug == slug, cancellationToken);

        if (product is not null)
        {
            return product;
        }

        product = new Product
        {
            NameEn = nameEn,
            NameBn = nameBn,
            Slug = slug,
            CategoryId = category.Id,
            Category = category,
            PrimaryUnit = primaryUnit,
            ProductState = productState,
            Status = status,
            IsActive = true,
            Notes = "Development demo product"
        };

        dbContext.Products.Add(product);
        return product;
    }

    private static async Task EnsureContributorAsync(BazarKotoDbContext dbContext, CancellationToken cancellationToken)
    {
        const string visitorId = "demo-visitor-tangail-001";

        if (await dbContext.Contributors.AnyAsync(x => x.VisitorId == visitorId, cancellationToken))
        {
            return;
        }

        dbContext.Contributors.Add(new Contributor
        {
            DisplayName = "Tangail Market Contributor",
            VisitorId = visitorId,
            SubmissionCount = 2,
            TrustScore = 4.75m,
            IsBlocked = false
        });
    }

    private static async Task EnsurePriceSubmissionAsync(
        BazarKotoDbContext dbContext,
        Market market,
        Product product,
        decimal pricePerUnit,
        SubmissionStatus status,
        User? submittedByUser,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PriceSubmissions.AnyAsync(
            x => x.MarketId == market.Id && x.ProductId == product.Id && x.Status == status,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.PriceSubmissions.Add(new PriceSubmission
        {
            MarketId = market.Id,
            Market = market,
            ProductId = product.Id,
            Product = product,
            Unit = product.PrimaryUnit,
            PricePerUnit = pricePerUnit,
            QuantityChecked = 1m,
            PriceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PriceTime = TimeOnly.FromDateTime(DateTime.UtcNow),
            SellerType = SellerType.Retail,
            PriceSource = PriceSource.ObservedInMarket,
            QualityGrade = QualityGrade.Standard,
            Notes = "Development demo price submission",
            SubmittedByUserId = submittedByUser?.Id,
            SubmittedByUser = submittedByUser,
            Status = status
        });
    }

    private static async Task EnsureContactMessageAsync(BazarKotoDbContext dbContext, CancellationToken cancellationToken)
    {
        const string email = "resident.tangail@example.com";
        const string subject = "Correction request for Park Bazar";

        if (await dbContext.ContactMessages.AnyAsync(x => x.Email == email && x.Subject == subject, cancellationToken))
        {
            return;
        }

        dbContext.ContactMessages.Add(new ContactMessage
        {
            Name = "Tangail Resident",
            Email = email,
            Subject = subject,
            Message = "Please verify the morning potato price at Park Bazar.",
            Status = ContactMessageStatus.New
        });
    }

    private static async Task EnsurePageVisitsAsync(BazarKotoDbContext dbContext, CancellationToken cancellationToken)
    {
        const string visitorPrefix = "demo-page-visitor-";

        if (await dbContext.PageVisits.AnyAsync(x => x.VisitorId.StartsWith(visitorPrefix), cancellationToken))
        {
            return;
        }

        var today = DateTime.UtcNow.Date;
        var visits = new[]
        {
            new { Path = "/", Title = "Home", VisitorId = "demo-page-visitor-001", Hour = 8 },
            new { Path = "/markets", Title = "Markets", VisitorId = "demo-page-visitor-002", Hour = 8 },
            new { Path = "/prices", Title = "Prices", VisitorId = "demo-page-visitor-003", Hour = 12 },
            new { Path = "/products", Title = "Products", VisitorId = "demo-page-visitor-004", Hour = 20 },
            new { Path = "/prices", Title = "Prices", VisitorId = "demo-page-visitor-005", Hour = 20 },
            new { Path = "/markets", Title = "Markets", VisitorId = "demo-page-visitor-006", Hour = 20 }
        };

        foreach (var visit in visits)
        {
            dbContext.PageVisits.Add(new PageVisit
            {
                Path = visit.Path,
                PageTitle = visit.Title,
                VisitorId = visit.VisitorId,
                IpHash = $"demo-ip-hash-{visit.VisitorId}",
                UserAgent = "BazarKoto Demo Browser",
                Referrer = "https://localhost:4200",
                DeviceType = "Desktop",
                Country = "Bangladesh",
                VisitedAt = today.AddHours(visit.Hour).AddMinutes(15)
            });
        }
    }

    private static async Task EnsureAdminAuditLogAsync(BazarKotoDbContext dbContext, User? adminUser, Market market, CancellationToken cancellationToken)
    {
        const string action = "SeedDemoData";

        if (await dbContext.AdminAuditLogs.AnyAsync(x => x.Action == action, cancellationToken))
        {
            return;
        }

        dbContext.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = adminUser?.Id,
            AdminUser = adminUser,
            Action = action,
            EntityName = nameof(Market),
            EntityId = market.Id.ToString(),
            OldValueJson = null,
            NewValueJson = "{\"status\":\"Approved\"}",
            IpHash = "demo-admin-ip-hash"
        });
    }

    private static async Task EnsureAdMetricAsync(BazarKotoDbContext dbContext, CancellationToken cancellationToken)
    {
        const string pagePath = "/prices";

        if (await dbContext.AdMetrics.AnyAsync(x => x.PagePath == pagePath, cancellationToken))
        {
            return;
        }

        dbContext.AdMetrics.Add(new AdMetric
        {
            PagePath = pagePath,
            Impressions = 120,
            Clicks = 6,
            Ctr = 5.00m,
            RecordedAt = DateTime.UtcNow
        });
    }
}
