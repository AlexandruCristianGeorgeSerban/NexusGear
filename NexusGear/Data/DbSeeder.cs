using Microsoft.EntityFrameworkCore;
using NexusGear.Models;

namespace NexusGear.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created and schema applied
        await context.Database.EnsureCreatedAsync();

        if (await context.Categories.AnyAsync())
        {
            return; // Database already seeded
        }

        // ==========================================
        // 1. Categories (4 Categories)
        // ==========================================
        var catKeyboards = new Category
        {
            Name = "Mechanical Keyboards",
            Slug = "mechanical-keyboards",
            Description = "Enthusiast-grade mechanical keyboards featuring gasket mounts, hot-swappable PCB sockets, south-facing RGB, and factory-lubed switches."
        };

        var catMice = new Category
        {
            Name = "Gaming Mice",
            Slug = "gaming-mice",
            Description = "Ultralight wireless and wired esports mice equipped with sub-millisecond optical sensors, 8000Hz polling rates, and pure PTFE glide feet."
        };

        var catDeskmats = new Category
        {
            Name = "Deskmats & Mousepads",
            Slug = "deskmats-mousepads",
            Description = "Extended 900x400mm performance desk surfaces, hybrid high-speed Cordura pads, micro-etched tempered glass, and minimalist wool felt."
        };

        var catChairs = new Category
        {
            Name = "Ergonomic Gaming Chairs",
            Slug = "ergonomic-gaming-chairs",
            Description = "Engineered ergonomic seating featuring dynamic adaptive lumbar support, multi-layer breathable elastomer mesh, and 4D adjustable armrests."
        };

        context.Categories.AddRange(catKeyboards, catMice, catDeskmats, catChairs);
        await context.SaveChangesAsync();

        // ==========================================
        // 2. Products (30 100% Original NexusGear Products)
        // ==========================================
        var products = new List<Product>
        {
            // Mechanical Keyboards (8 items)
            new Product
            {
                Name = "NexusForge K-75 Pro Horizon",
                Description = "75% exploded layout custom mechanical keyboard with CNC aluminum top frame, sound-dampening silicone gasket mount, hot-swappable Kailh sockets, south-facing RGB, and factory-lubed linear switches.",
                Price = 189.99m,
                StockQuantity = 14,
                ImageUrl = "/images/products/product_1.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusForge K-60 CyberKey",
                Description = "60% ultra-compact mechanical keyboard engineered for competitive FPS gamers. Detachable braided USB-C cable, PBT double-shot keycaps, and pre-lubed tactile switches for snappy actuation.",
                Price = 119.99m,
                StockQuantity = 18,
                ImageUrl = "/images/products/product_2.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusGlide K-87 Tenkeyless",
                Description = "Classic 87-key TKL layout offering the perfect balance between desk real estate and dedicated function navigation keys. Equipped with dampening foam and south-facing per-key illumination.",
                Price = 149.99m,
                StockQuantity = 12,
                ImageUrl = "/images/products/product_3.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusMatrix K-96 Wireless",
                Description = "Compact 96% layout retaining full numeric keypad utility with reduced width. Tri-mode connectivity (2.4GHz wireless, Bluetooth 5.2, USB-C) with a massive 4000mAh rechargeable battery.",
                Price = 179.99m,
                StockQuantity = 10,
                ImageUrl = "/images/products/product_4.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusSplit ErgoCore Dual",
                Description = "Ergonomic split mechanical keyboard designed to prevent wrist fatigue during marathon coding and gaming sessions. Independent tilt angles, magnetic palm rests, and hot-swap PCB sockets.",
                Price = 229.99m,
                StockQuantity = 6,
                ImageUrl = "/images/products/product_5.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusPhantom K-65 LowProfile",
                Description = "Ultra-slim 65% mechanical keyboard featuring Kailh Choc V2 low-profile switches and aircraft-grade aluminum chassis. Effortless portability with whisper-quiet keystroke acoustic tuning.",
                Price = 139.99m,
                StockQuantity = 20,
                ImageUrl = "/images/products/product_6.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusVapor K-80 Linear Glow",
                Description = "80% polycarbonate translucent case keyboard delivering an ethereal underglow diffusion. Hand-tuned screw-in stabilizers and ultra-smooth linear switches with 45g actuation force.",
                Price = 159.99m,
                StockQuantity = 16,
                ImageUrl = "/images/products/product_7.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                CategoryId = catKeyboards.CategoryId
            },
            new Product
            {
                Name = "NexusTitan K-75 Limited Prototype",
                Description = "Collector's edition boutique mechanical keyboard machined from single-block titanium alloy with hand-polished bevels and bespoke artisan novelty keycap. Strictly limited production.",
                Price = 399.99m,
                StockQuantity = 1, // Stock = 1 for concurrency and race condition testing (A7)
                ImageUrl = "/images/products/product_8.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CategoryId = catKeyboards.CategoryId
            },

            // Gaming Mice (8 items)
            new Product
            {
                Name = "NexusPulse M-40 Ultralight",
                Description = "42g honeycomb composite wireless gaming mouse. True 26,000 DPI optical sensor, optical switches rated for 90 million clicks, and virgin PTFE glide skates for frictionless aiming.",
                Price = 99.99m,
                StockQuantity = 15,
                ImageUrl = "/images/products/product_9.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-27),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusGlide M-50 Precision",
                Description = "Symmetrical esports mouse featuring flawless 1:1 tracking at 30,000 DPI, zero motion smoothing, and lightweight 49g solid shell construction without perforated holes.",
                Price = 129.99m,
                StockQuantity = 22,
                ImageUrl = "/images/products/product_10.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-24),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusSpeed M-8K HyperDongle",
                Description = "Breakthrough 8000Hz hyper-polling wireless gaming mouse delivering sub-0.125ms click response time. Includes dedicated OLED wireless transmitter base with battery monitoring.",
                Price = 149.99m,
                StockQuantity = 11,
                ImageUrl = "/images/products/product_11.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-21),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusErgo M-60 Contour Grip",
                Description = "Right-handed palm grip ergonomic mouse featuring an integrated textured thumb rest, dual side scroll wheel, and whisper-silent secondary buttons for hybrid office and gaming setups.",
                Price = 89.99m,
                StockQuantity = 19,
                ImageUrl = "/images/products/product_12.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-19),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusMacro M-12 MultiButton",
                Description = "12-button side grid programmable MMO/productivity mouse. Mechanical microswitch tactile feedback, onboard hardware profile memory, and ergonomic ring-finger support rest.",
                Price = 79.99m,
                StockQuantity = 13,
                ImageUrl = "/images/products/product_13.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-16),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusCarbon M-45 Composite",
                Description = "Forged carbon fiber composite structural frame weighing only 39g. Magnetic charging cradle included, with custom low-friction sensor ring glide for ultimate flick speed.",
                Price = 169.99m,
                StockQuantity = 8,
                ImageUrl = "/images/products/product_14.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-13),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusChroma M-35 Minimal Wired",
                Description = "Ultra-flexible paracord wired mouse with zero cable drag. High-grade PixArt optical sensor, 58g lightweight build, and vibrant 360-degree bottom RGB halo lighting.",
                Price = 49.99m,
                StockQuantity = 25,
                ImageUrl = "/images/products/product_15.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CategoryId = catMice.CategoryId
            },
            new Product
            {
                Name = "NexusModular M-70 SwitchCraft",
                Description = "Innovative modular mouse allowing toolless hot-swapping of primary click switches and interchanging high-hump vs low-profile magnetic top covers for personalized grip styles.",
                Price = 119.99m,
                StockQuantity = 14,
                ImageUrl = "/images/products/product_16.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                CategoryId = catMice.CategoryId
            },

            // Deskmats & Mousepads (8 items)
            new Product
            {
                Name = "NexusMat Surface-90 CyberGrid",
                Description = "Extended 900x400x4mm micro-woven cloth deskmat. High-density edge stitching prevents fraying, paired with high-grip textured natural rubber base and futuristic cyber line art.",
                Price = 34.99m,
                StockQuantity = 25,
                ImageUrl = "/images/products/product_17.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-26),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat Surface-90 MinimalWool",
                Description = "Crafted from 100% natural 4mm high-density merino wool felt. Provides a soft, warm acoustic dampener for your desk while elevating your keyboard's acoustic sound profile.",
                Price = 44.99m,
                StockQuantity = 17,
                ImageUrl = "/images/products/product_18.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-23),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat ArmorCord Hybrid Speed",
                Description = "Engineered with water-resistant Cordura textile weave for lightning-fast horizontal glides and micro-stopping control. Resists liquid spills and dust accumulation effortlessly.",
                Price = 39.99m,
                StockQuantity = 20,
                ImageUrl = "/images/products/product_19.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat GlassGlaze Alumino XXL",
                Description = "Premium 480x400mm tempered aluminosilicate glass mouse surface. CNC rounded edges, micro-etched low-friction glide, and full silicone underlayer for unshakeable stability.",
                Price = 89.99m,
                StockQuantity = 9,
                ImageUrl = "/images/products/product_20.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-17),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat Surface-90 NeonFlux RGB",
                Description = "Extra-large 900x400mm deskmat with 360-degree stitched RGB optical light tubing. 14 addressable lighting modes with physical selector button and water-repellent surface finish.",
                Price = 49.99m,
                StockQuantity = 18,
                ImageUrl = "/images/products/product_21.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat Surface-90 DarkContour",
                Description = "Topographic elevation contour design printed on premium micro-knit fabric. Optimized for low-DPI precision aiming with uniform friction characteristics across X and Y axes.",
                Price = 36.99m,
                StockQuantity = 24,
                ImageUrl = "/images/products/product_22.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-11),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat PoronPrecision Control",
                Description = "Tournament-grade 490x420mm pad backed by authentic Japanese Poron foam. Remarkable suction effect on any desk surface and unmatched tactile stopping power for tactical shooters.",
                Price = 59.99m,
                StockQuantity = 12,
                ImageUrl = "/images/products/product_23.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                CategoryId = catDeskmats.CategoryId
            },
            new Product
            {
                Name = "NexusMat VeganLeather Executive",
                Description = "Double-sided premium PU leather deskmat. Water-resistant, oil-proof, and easy to wipe clean with a damp cloth. Perfect backdrop for minimalist mechanical keyboards.",
                Price = 29.99m,
                StockQuantity = 22,
                ImageUrl = "/images/products/product_24.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CategoryId = catDeskmats.CategoryId
            },

            // Ergonomic Gaming Chairs (6 items)
            new Product
            {
                Name = "NexusThrone ErgoAir Breathable Mesh",
                Description = "Full-body Korean high-tensile elastomer mesh ergonomic chair. Self-adjusting dynamic lumbar support, 4D multi-directional armrests, and 135-degree recline with multi-position lock.",
                Price = 449.99m,
                StockQuantity = 7,
                ImageUrl = "/images/products/product_25.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-27),
                CategoryId = catChairs.CategoryId
            },
            new Product
            {
                Name = "NexusThrone Sovereign Executive Dual",
                Description = "Premium high-density cold-cured foam cushioning wrapped in breathable hybrid leatherette. Includes magnetic memory foam headrest pillow and reinforced heavy-duty aluminum wheelbase.",
                Price = 399.99m,
                StockQuantity = 9,
                ImageUrl = "/images/products/product_26.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                CategoryId = catChairs.CategoryId
            },
            new Product
            {
                Name = "NexusThrone PostureMax ActiveLumbar",
                Description = "Engineered with an internal multi-axis mechanical lumbar dial that allows fine-tuning depth and curvature. Relieves spinal compression during extended 10+ hour workstation sessions.",
                Price = 479.99m,
                StockQuantity = 5,
                ImageUrl = "/images/products/product_27.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                CategoryId = catChairs.CategoryId
            },
            new Product
            {
                Name = "NexusThrone AeroWeave SoftFabric",
                Description = "Custom multi-layer breathable fabric weave engineered for maximum airflow and cooling. Resistant to pilling and static electricity, equipped with Class-4 heavy gas lift cylinder.",
                Price = 429.99m,
                StockQuantity = 8,
                ImageUrl = "/images/products/product_28.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                CategoryId = catChairs.CategoryId
            },
            new Product
            {
                Name = "NexusThrone CompactTask Studio",
                Description = "Streamlined ergonomic task chair suited for compact desk setups. Synchronous tilt mechanism, 3D flip-up armrests, and breathable dual-layer mesh backrest.",
                Price = 289.99m,
                StockQuantity = 12,
                ImageUrl = "/images/products/product_29.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-9),
                CategoryId = catChairs.CategoryId
            },
            new Product
            {
                Name = "NexusThrone CarbonRacer GT Sport",
                Description = "Motorsport-inspired ergonomic gaming chair featuring carbon fiber accent panels, cold-molded lumbar bolsters, and 165-degree full reclining mechanism for post-match relaxation.",
                Price = 369.99m,
                StockQuantity = 10,
                ImageUrl = "/images/products/product_30.svg",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                CategoryId = catChairs.CategoryId
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // ==========================================
        // 3. Preconfigured Accounts (BCrypt Hashed)
        // ==========================================
        var adminUser = new User
        {
            Email = "admin@nexusgear.ro",
            FullName = "Nexus Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var customerUser = new User
        {
            Email = "customer@nexusgear.ro",
            FullName = "Alexandru Customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Users.AddRange(adminUser, customerUser);
        await context.SaveChangesAsync();

        // ==========================================
        // 4. Default Address for Customer
        // ==========================================
        var customerAddress = new Address
        {
            Street = "Bulevardul Decebal Nr. 10, Bl. S2, Ap. 15",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200734",
            Phone = "+40 722 123 456",
            IsDefault = true,
            UserId = customerUser.UserId
        };

        context.Addresses.Add(customerAddress);
        await context.SaveChangesAsync();

        // ==========================================
        // 5. Preconfigured Discount Codes (B5)
        // ==========================================
        var discountPercentage = new DiscountCode
        {
            Code = "NEXUS10",
            DiscountType = "Percentage",
            DiscountValue = 10.00m,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            MaxUses = 100,
            UsedCount = 2
        };

        var discountFixed = new DiscountCode
        {
            Code = "SETUP50",
            DiscountType = "FixedAmount",
            DiscountValue = 50.00m,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            MaxUses = 50,
            UsedCount = 0
        };

        var discountExpired = new DiscountCode
        {
            Code = "EXPIRED20",
            DiscountType = "Percentage",
            DiscountValue = 20.00m,
            ExpiryDate = DateTime.UtcNow.AddDays(-30),
            MaxUses = 100,
            UsedCount = 0
        };

        context.DiscountCodes.AddRange(discountPercentage, discountFixed, discountExpired);
        await context.SaveChangesAsync();

        // ==========================================
        // 6. Pre-seeded Orders for Sales Analytics (B4) and Recommendations (B2)
        // ==========================================
        var order1 = new Order
        {
            OrderNumber = "NXG-2026-1001",
            OrderDate = DateTime.UtcNow.AddDays(-6),
            Status = "Paid",
            ShippingMethod = "Standard Courier",
            ShippingCost = 15.00m,
            DiscountAmount = 28.99m,
            TotalAmount = 275.99m,
            StripeSessionId = "cs_test_mock_session_1001",
            UserId = customerUser.UserId,
            AddressId = customerAddress.AddressId,
            DiscountCodeId = discountPercentage.DiscountCodeId
        };

        var order2 = new Order
        {
            OrderNumber = "NXG-2026-1002",
            OrderDate = DateTime.UtcNow.AddDays(-14),
            Status = "Delivered",
            ShippingMethod = "Express Next-Day",
            ShippingCost = 35.00m,
            DiscountAmount = 0.00m,
            TotalAmount = 519.98m,
            StripeSessionId = "cs_test_mock_session_1002",
            UserId = customerUser.UserId,
            AddressId = customerAddress.AddressId,
            DiscountCodeId = null
        };

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Order 1 Items (Keyboard + Mouse: co-occurrence for B2 recommendations!)
        var order1Item1 = new OrderItem
        {
            OrderId = order1.OrderId,
            ProductId = products[0].ProductId,
            Quantity = 1,
            UnitPrice = products[0].Price
        };

        var order1Item2 = new OrderItem
        {
            OrderId = order1.OrderId,
            ProductId = products[8].ProductId,
            Quantity = 1,
            UnitPrice = products[8].Price
        };

        // Order 2 Items (Deskmat + Chair)
        var order2Item1 = new OrderItem
        {
            OrderId = order2.OrderId,
            ProductId = products[16].ProductId,
            Quantity = 1,
            UnitPrice = products[16].Price
        };

        var order2Item2 = new OrderItem
        {
            OrderId = order2.OrderId,
            ProductId = products[24].ProductId,
            Quantity = 1,
            UnitPrice = products[24].Price
        };

        context.OrderItems.AddRange(order1Item1, order1Item2, order2Item1, order2Item2);
        await context.SaveChangesAsync();

        // ==========================================
        // 7. Pre-seeded Reviews from Customer (B3)
        // ==========================================
        var review1 = new Review
        {
            Rating = 5,
            Comment = "The K-75 Horizon is easily the cleanest keyboard I have ever typed on. The factory-lubed switches sound deep and creamy right out of the box, and the aluminum case feels ultra premium.",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UserId = customerUser.UserId,
            ProductId = products[0].ProductId
        };

        var review2 = new Review
        {
            Rating = 5,
            Comment = "Phenomenal build quality. The 42g weight makes micro-adjustments in tactical shooters feel effortless without feeling flimsy.",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UserId = customerUser.UserId,
            ProductId = products[8].ProductId
        };

        var review3 = new Review
        {
            Rating = 4,
            Comment = "Very smooth glide and high-density stitching. The cyber line art looks sleek on a dark desk setup.",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddDays(-12),
            UserId = customerUser.UserId,
            ProductId = products[16].ProductId
        };

        context.Reviews.AddRange(review1, review2, review3);
        await context.SaveChangesAsync();
    }
}

