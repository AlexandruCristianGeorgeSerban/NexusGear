# ⚡ NexusGear: Desk Experience & Gaming Peripherals

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Monolith-512BD4)](https://learn.microsoft.com/aspnet/core)
[![Database](https://img.shields.io/badge/SQLite-Entity%20Framework%20Core-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Frontend](https://img.shields.io/badge/Frontend-HTML5%20%7C%20CSS3%20%7C%20JS%20%7C%20AJAX-F7DF1E?logo=javascript&logoColor=black)](#)
[![UI](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![Payments](https://img.shields.io/badge/Payments-Stripe%20Test%20Mode-635BFF?logo=stripe&logoColor=white)](https://stripe.com/)

> **E-Commerce Project | Academic Year 2026-2027**  
> *A curated home for high-performance gaming peripherals and ergonomic desk setups.*

---

## 📖 Overview

**NexusGear** is a custom, full-stack e-commerce web application built from scratch to deliver a seamless shopping experience for gamers, programmers, and remote workers. Designed with a mobile-first philosophy, the store connects technical enthusiast hardware with ergonomic workspace equipment into a unified buying journey.

The platform provides a complete customer lifecycle, from catalog discovery and dynamic cart management to secure webhook-verified Stripe sandbox payments, paired with a custom administrative back-office for inventory and order management.

---

## 🚀 Quick Start Guide (≤ 5 Commands)

The application starts locally using standard .NET CLI commands. Ensure [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) is installed.

```bash
# 1. Clone repository
git clone https://github.com/AlexandruCristianGeorgeSerban/NexusGear.git
cd NexusGear

# 2. Copy environment template (configured for SQLite and test sandbox)
cp .env.example .env

# 3. Build solution (.slnx modern format)
dotnet build NexusGear.slnx

# 4. Run automated xUnit test suite
dotnet test NexusGear.slnx

# 5. Launch web application (automatically opens browser at https://localhost:7077)
dotnet run --project NexusGear
```

> **Visual Studio 2026 / Rider**: Open `NexusGear.slnx` directly and press **F5**. Both `http` and `https` profiles in `Properties/launchSettings.json` are preconfigured with `launchBrowser: true`.

---

## 🛠️ Core Technology Stack

* **Frontend**: HTML5, CSS3, JavaScript, and AJAX paired with Bootstrap 5 + Bootstrap Icons for a mobile-first responsive UI (down to a 360 px viewport) and asynchronous page updates.
* **Backend**: ASP.NET Core (.NET 10) - Layered monolithic architecture (Controllers, Service Layer, EF Core models) with server-side validation, antiforgery protection, and authentication support.
* **Database**: SQLite via Entity Framework Core with Fluent API, migrations, and deterministic seed scripts (30 original products across 4 categories: Mechanical Keyboards, Gaming Mice, Deskmats & Mousepads, and Ergonomic Gaming Chairs) with photorealistic AI product assets.
* **Payment Gateway**: Stripe (Sandbox/Test Mode) utilizing verified asynchronous server-side webhooks (`checkout.session.completed`) and atomic inventory reduction.
* **Email Testing**: Transactional HTML order confirmations sent through Mailtrap SMTP.
* **Testing**: xUnit automated testing suite covering catalog search, pagination, multi-facet filtering, dynamic recommendation bundling, BCrypt user authentication, profile management, verified customer review gates, hybrid shopping cart concurrency/self-healing, wishlist engine, multi-user address sharing, roadmap module routing, and custom error handling (53 automated unit tests).

---

## 📋 Mandatory Requirements & Ownership (A1–A12)

| Code | Requirement | Primary Owner | Target Stage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **A1** | **Product catalog** (pagination ≤ 12/page, categories, seed data) | Popa-Ion Andrei | P2 (Week 5) | Complete |
| **A2** | **Basic search** (case-insensitive title search, empty state) | Popa-Ion Andrei | P2 (Week 6) | Complete |
| **A3** | **Shopping cart** (server-calculated totals, stock checks, persistence) | Șerban Alexandru-Cristian-George | P3 (Week 8) | Complete |
| **A4** | **User accounts** (registration/login, BCrypt password hashing) | Șerban Alexandru-Cristian-George | P2 (Week 5) | Complete |
| **A5** | **Checkout flow** (address, 2 shipping methods, summary, confirmation) | Popa-Ion Andrei | P3 (Week 8) | Planned |
| **A6** | **Sandbox payment** (Stripe test session + verified webhook) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Planned |
| **A7** | **Stock management** (concurrency-safe deduction & restock on cancel) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Planned |
| **A8** | **Order history** (customer dashboard with past orders and statuses) | Popa-Ion Andrei | P3 (Week 10) | Planned |
| **A9** | **Administration panel** (custom CRUD, image upload, order transitions) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Planned |
| **A10** | **Basic security** (server validation, CSRF, EF Core, admin guards) | Șerban Alexandru-Cristian-George | P3 (Week 11) | Planned |
| **A11** | **Responsive design** (usable down to a 360 px viewport) | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **A12** | **Automated tests** (≥ 10 xUnit tests run with a single command) | Both | P3 (Week 11) | Complete |

---

## 🌟 Bonus Features & Ownership (B1–B7)

| Code | Bonus Feature | Primary Owner | Target Stage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **B1** | **Filtered search** (category, price range, in-stock, sorting, URL preserved) | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **B2** | **Recommendations** (dynamic "Frequently bought together" and "Similar products") | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **B3** | **Reviews and ratings** (verified buyers only, avg rating, admin moderation) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Complete |
| **B4** | **Sales dashboard** (daily sales chart, top 5 products, AOV, time filters) | Șerban Alexandru-Cristian-George | P3 (Week 11) | Planned |
| **B5** | **Discount codes** (% or fixed amount, expiry date, max uses, server-side) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Seeded / P3 |
| **B6** | **Transactional email** (order confirmation via Mailtrap SMTP) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Planned |
| **B7** | **Wishlist** (favorites for authenticated users, move to cart) | Popa-Ion Andrei | P3 (Week 8) | Complete |

---

## 👥 The Team

* [**Popa-Ion Andrei**](https://github.com/Popa-Ion-Andrei) — *Frontend Lead & Customer Experience* (UI/UX, Client flows, Responsive design)
* [**Șerban Alexandru-Cristian-George**](https://github.com/AlexandruCristianGeorgeSerban) — *Backend Lead & System Integrations* (Architecture, SQLite/EF Core, Stripe webhooks, Security)

---

## 🤖 AI Tools Declaration

As required by the course specification, the following AI tools were utilized during Stage 1:

* **Google Gemini**: Consulted for analyzing project specifications and grading criteria, structuring the project planning and roadmap, drafting documentation (README, gitignore rules), and generating SVG brand assets.
* **OpenAI ChatGPT**: Used for drafting the initial slide outline and assisting with presentation content generation.

As required by the course specification, the following AI tools were utilized during Stage 2:

* **Antigravity IDE & Google Gemini**: Assisting with the architectural scaffolding of the ASP.NET Core (.NET 10 MVC) layered monolith, configuring the modern `.slnx` solution format, implementing the 3NF relational database entities (11 tables including the `HAS_ADDRESS` M:N address book, `RepositoryWrapper`, and `Unit of Work`), developing the catalog search/filtering/recommendation algorithms, and writing the 53 automated xUnit tests (`NexusGear.Tests`).
* **Google Gemini**: Generating 30 photorealistic studio product illustrations (`Product_1.png` – `Product_30.png`) across all 4 store categories (mechanical keyboards, gaming mice, deskmats, and ergonomic gaming chairs) to deliver a cohesive "Dark Tech" visual identity.
* **OpenAI ChatGPT & Google Gemini**: Assisting with specification consistency analysis, refining technical documentation formatting, and structuring the P2 presentation and 8-page technical specification report.
