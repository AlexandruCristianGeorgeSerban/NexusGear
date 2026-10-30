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
* **Database**: SQLite via Entity Framework Core with Fluent API, migrations, and deterministic seed scripts (30 original products across 4 categories: Mechanical Keyboards, Gaming Mice, Deskmats & Mousepads, and Ergonomic Gaming Chairs).
* **Payment Gateway**: Stripe (Sandbox/Test Mode) utilizing verified asynchronous server-side webhooks (`checkout.session.completed`) and atomic inventory reduction.
* **Email Testing**: Transactional HTML order confirmations sent through Mailtrap SMTP.
* **Testing**: xUnit automated testing suite covering cart totals, concurrency-safe stock logic, webhook payment confirmations, and business rules.

---

## 📋 Mandatory Requirements & Ownership (A1–A12)

| Code | Requirement | Primary Owner | Target Stage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **A1** | **Product catalog** (pagination ≤ 12/page, categories, seed data) | Popa-Ion Andrei | P2 (Week 5) | Planned |
| **A2** | **Basic search** (case-insensitive title search, empty state) | Popa-Ion Andrei | P2 (Week 6) | Planned |
| **A3** | **Shopping cart** (server-calculated totals, stock checks, persistence) | Șerban Alexandru-Cristian-George | P3 (Week 8) | Planned |
| **A4** | **User accounts** (registration/login, BCrypt password hashing) | Șerban Alexandru-Cristian-George | P2 (Week 5) | Planned |
| **A5** | **Checkout flow** (address, 2 shipping methods, summary, confirmation) | Popa-Ion Andrei | P3 (Week 8) | Planned |
| **A6** | **Sandbox payment** (Stripe test session + verified webhook) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Planned |
| **A7** | **Stock management** (concurrency-safe deduction & restock on cancel) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Planned |
| **A8** | **Order history** (customer dashboard with past orders and statuses) | Popa-Ion Andrei | P3 (Week 10) | Planned |
| **A9** | **Administration panel** (custom CRUD, image upload, order transitions) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Planned |
| **A10** | **Basic security** (server validation, CSRF, EF Core, admin guards) | Șerban Alexandru-Cristian-George | P3 (Week 11) | Planned |
| **A11** | **Responsive design** (usable down to a 360 px viewport) | Popa-Ion Andrei | P3 (Week 11) | Planned |
| **A12** | **Automated tests** (≥ 10 xUnit tests run with a single command) | Both | P3 (Week 11) | Planned |

---

## 🌟 Bonus Features & Ownership (B1–B7)

| Code | Bonus Feature | Primary Owner | Target Stage | Points | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **B1** | **Filtered search** (category, price range, in-stock, sorting, URL preserved) | Popa-Ion Andrei | P3 (Week 11) | 5 pts | Planned |
| **B2** | **Recommendations** (dynamic "Frequently bought together" and "Similar products") | Popa-Ion Andrei | P3 (Week 11) | 4 pts | Planned |
| **B3** | **Reviews and ratings** (verified buyers only, avg rating, admin moderation) | Șerban Alexandru-Cristian-George | P3 (Week 10) | 4 pts | Planned |
| **B4** | **Sales dashboard** (daily sales chart, top 5 products, AOV, time filters) | Șerban Alexandru-Cristian-George | P3 (Week 11) | 3 pts | Planned |
| **B5** | **Discount codes** (% or fixed amount, expiry date, max uses, server-side) | Șerban Alexandru-Cristian-George | P3 (Week 10) | 3 pts | Planned |
| **B6** | **Transactional email** (order confirmation via Mailtrap SMTP) | Șerban Alexandru-Cristian-George | P3 (Week 9) | 5 pts | Planned |
| **B7** | **Wishlist** (favorites for authenticated users, move to cart) | Popa-Ion Andrei | P3 (Week 8) | 2 pts | Planned |

---

## 👥 The Team

* **Popa-Ion Andrei** ([GitHub](https://github.com/Popa-Ion-Andrei)) — *Frontend Lead & Customer Experience* (UI/UX, Client flows, Responsive design)
* **Șerban Alexandru-Cristian-George** ([GitHub](https://github.com/AlexandruCristianGeorgeSerban)) — *Backend Lead & System Integrations* (Architecture, SQLite/EF Core, Stripe webhooks, Security)

---

## 🤖 AI Tools Declaration

As required by the course specification, the following AI tools were utilized during Stage 1:

* **Google Gemini**: Consulted for analyzing project specifications and grading criteria, structuring the project planning and roadmap, drafting documentation (README, gitignore rules), and generating SVG brand assets.
* **OpenAI ChatGPT**: Used for drafting the initial slide outline and assisting with presentation content generation.

As required by the course specification, the following AI tools were utilized during Stage 2:

* **Google Gemini (Antigravity)**: Assisting with architectural scaffolding of the .NET 10 monolithic web solution, configuring modern `.slnx` solution format, designing relational database entities matching RM/ERD specifications, generating original SVG vector graphics for NexusGear product lines, and preparing local environment templates.
