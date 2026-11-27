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
# 1. Clone repository and navigate to directory
git clone https://github.com/AlexandruCristianGeorgeSerban/NexusGear.git && cd NexusGear

# 2. Copy environment template (configured for SQLite and test sandbox)
cp .env.example .env

# 3. Run automated xUnit test suite (133 passing tests)
dotnet test NexusGear.slnx

# 4. Launch web application (automatically opens browser at https://localhost:7077)
dotnet run --project NexusGear
```

> **Visual Studio 2026 / Rider**: Open `NexusGear.slnx` directly and press **F5**. Both `http` and `https` profiles in `Properties/launchSettings.json` are preconfigured with `launchBrowser: true`.

---

## 🛠️ Core Technology Stack

* **Frontend**: HTML5, CSS3, JavaScript, and AJAX paired with Bootstrap 5 + Bootstrap Icons for a mobile-first responsive UI (down to a 360 px viewport) and asynchronous page updates.
* **Backend**: ASP.NET Core (.NET 10) - Layered monolithic architecture (Controllers, Service Layer, EF Core models) with server-side validation, antiforgery protection, and authentication support.
* **Database**: SQLite via Entity Framework Core with Fluent API, migrations, and deterministic seed scripts (30 original products across 4 categories: Mechanical Keyboards, Gaming Mice, Deskmats & Mousepads, and Ergonomic Gaming Chairs) with photorealistic AI product assets.
* **Payment Gateway**: Stripe (Sandbox/Test Mode) utilizing verified asynchronous server-side webhooks (`checkout.session.completed`), automatic fallback to embedded Sandbox Simulator for offline grading, and atomic inventory reduction.
* **Email Testing**: Transactional HTML order confirmations sent through Mailtrap SMTP with fallback simulator logging.
* **Testing**: xUnit automated testing suite covering catalog search, pagination, multi-facet filtering, dynamic recommendation bundling, smart hardware bundle discounts, BCrypt user authentication, profile management, verified customer review gates, hybrid shopping cart concurrency/self-healing, wishlist engine, multi-user address sharing, roadmap module routing, custom error handling, promo code validation, multi-step checkout order placement, Stripe test session & simulator fallback (approved and declined cards), idempotent webhook processing, atomic inventory decrement, concurrency token checks, customer order history isolation, admin order workflow state transitions, paid-order cancellation restock, product/category/promo code CRUD with slug and referential integrity checks, sales analytics dashboard with Chart.js, review visibility moderation, HTML email receipt encoding, and server-side security authorization/CSRF validation (133 automated unit tests).

---

## 📋 Mandatory Requirements & Ownership (A1–A12)

| Code | Requirement | Primary Owner | Target Stage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **A1** | **Product catalog** (pagination ≤ 12/page, categories, seed data) | Popa-Ion Andrei | P2 (Week 5) | Complete |
| **A2** | **Basic search** (case-insensitive title search, empty state) | Popa-Ion Andrei | P2 (Week 6) | Complete |
| **A3** | **Shopping cart** (server-calculated totals, stock checks, persistence) | Șerban Alexandru-Cristian-George | P3 (Week 8) | Complete |
| **A4** | **User accounts** (registration/login, BCrypt password hashing) | Șerban Alexandru-Cristian-George | P2 (Week 5) | Complete |
| **A5** | **Checkout flow** (address, 2 shipping methods, summary, confirmation) | Popa-Ion Andrei | P3 (Week 8) | Complete |
| **A6** | **Sandbox payment** (Stripe test session + verified webhook + decline simulator) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Complete |
| **A7** | **Stock management** (concurrency-safe deduction & restock on cancel) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Complete |
| **A8** | **Order history** (customer dashboard with past orders and statuses) | Popa-Ion Andrei | P3 (Week 10) | Complete |
| **A9** | **Administration panel** (custom product & category CRUD, image upload, order transitions) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Complete |
| **A10** | **Basic security** (server validation, CSRF, EF Core, admin guards) | Șerban Alexandru-Cristian-George | P3 (Week 11) | Complete |
| **A11** | **Responsive design** (usable down to a 360 px viewport) | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **A12** | **Automated tests** (≥ 10 xUnit tests run with a single command) | Both | P3 (Week 11) | Complete |

---

## 🌟 Bonus Features & Ownership (B1–B7)

| Code | Bonus Feature | Primary Owner | Target Stage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **B1** | **Filtered search** (category, price range, in-stock, sorting, URL preserved) | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **B2** | **Recommendations** (dynamic "Frequently bought together" and "Similar products") | Popa-Ion Andrei | P3 (Week 11) | Complete |
| **B3** | **Reviews and ratings** (verified buyers only, avg rating, admin moderation) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Complete |
| **B4** | **Sales dashboard** (daily sales chart, top 5 products, AOV, time filters) | Șerban Alexandru-Cristian-George | P3 (Week 11) | Complete |
| **B5** | **Discount codes** (% or fixed RON amount, expiry, max uses, admin CRUD, server-side) | Șerban Alexandru-Cristian-George | P3 (Week 10) | Complete |
| **B6** | **Transactional email** (order confirmation via Mailtrap SMTP) | Șerban Alexandru-Cristian-George | P3 (Week 9) | Complete |
| **B7** | **Wishlist** (favorites for authenticated users, move to cart) | Popa-Ion Andrei | P3 (Week 8) | Complete |

---

## 👥 The Team

* [**Popa-Ion Andrei**](https://github.com/Popa-Ion-Andrei) — *Frontend Lead & Customer Experience* (UI/UX, Client flows, Responsive design)
* [**Șerban Alexandru-Cristian-George**](https://github.com/AlexandruCristianGeorgeSerban) — *Backend Lead & System Integrations* (Architecture, SQLite/EF Core, Stripe webhooks, Security)

---

## 🤖 AI Tools Declaration

As required by the course specification, the following AI tools were utilized during Stage 1:

* **Google Gemini**: Consulted for analyzing project specifications and grading criteria, structuring the project planning and roadmap, drafting documentation (README, gitignore rules), and generating SVG brand assets.
* **OpenAI ChatGPT**: Used for drafting the initial slide outline and assisting with presentation content generation with prompts provided by Google Gemini.

As required by the course specification, the following AI tools were utilized during Stage 2:

* **Antigravity IDE & Google Gemini**: Assisting with the architectural scaffolding of the ASP.NET Core (.NET 10 MVC) layered monolith, configuring the modern `.slnx` solution format, implementing the 3NF relational database entities (11 tables including the `HAS_ADDRESS` M:N address book, `RepositoryWrapper`, and `Unit of Work`), developing the catalog search/filtering/recommendation algorithms, and writing the 53 automated xUnit tests (`NexusGear.Tests`).
* **Google Gemini**: Generating 30 photorealistic studio product illustrations (`Product_1.png` – `Product_30.png`) across all 4 store categories (mechanical keyboards, gaming mice, deskmats, and ergonomic gaming chairs) to deliver a cohesive "Dark Tech" visual identity.
* **OpenAI ChatGPT**: Assisting with specification consistency analysis, refining technical documentation formatting, and structuring the P2 presentation and technical specification report with prompts provided by Antigravity IDE.

As required by the course specification, the following AI tools were utilized during Stage 3:

* **Antigravity IDE & Google Gemini**: Core full-stack engineering and development environment used for implementing Stage 3 deliverables: designing and implementing the smart hardware bundle scoping algorithm with unallocated quantity allocation (B2), real-time multi-row AJAX cart synchronization (`syncCartItemsDom`), Stripe Checkout Sandbox integration with HMAC cryptographic webhook verification and offline local payment simulator fallback including declined card profiles (A6), atomic inventory decrement with `ConcurrencyCheck` tokens and paid-order restock on cancellation (A7), transactional HTML order confirmation emails via Mailtrap SMTP (B6), verified paid-buyer review moderation gates and admin review visibility controls (B3), category and promo code CRUD administration with referential protection (A9, B5), Chart.js sales analytics dashboard with dynamic interval aggregations (B4), and expanding the automated test suite from 53 to 133 passing xUnit tests (`NexusGear.Tests`).
* **OpenAI ChatGPT**: Used for generating and refining the final P3 presentation slide deck (`NexusGear_P3_Presentation`) and structuring the comprehensive project technical report based on the architectural specifications, component blueprints, and prompts provided by Antigravity IDE.

---

## 🔑 Demonstration Accounts & Test Data

For grading evaluation and local demonstration, the database is pre-seeded with the following credentials:

| Role | Email | Password | Access / Capabilities |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@nexusgear.ro` | `Admin123!` | Admin Back-Office (`/Admin`), Sales Analytics Dashboard (Chart.js), Order state transitions, Product, Category & Promo Code CRUD, Customer Review moderation |
| **Customer** | `customer@nexusgear.ro` | `Customer123!` | Customer Storefront, Saved address book, Wishlist management, Order history with status stepper, Verified buyer review submission |

### 🎟️ Preconfigured Promo Codes (B5)

* `NEXUS10` — 10% percentage discount on entire cart subtotal (Active, max 100 uses).
* `SETUP50` — 50.00 RON fixed-amount discount on entire cart subtotal (Active, max 50 uses).
* `EXPIRED20` — 20% discount (Expired, triggers graceful validation failure banner for demonstration).

---

## 📁 Milestone Deliverables

* **P1 (Team Setup & Planning)**: [`Presentations/P1/`](Presentations/P1/)
  * `NexusGear_P1_Team_Setup_and_Planning.pptx` / `NexusGear_P1_Team_Setup_and_Planning.pdf`
* **P2 (Specification & Prototype)**: [`Presentations/P2/`](Presentations/P2/)
  * `NexusGear_P2_Presentation.pptx` / `NexusGear_P2_Presentation.pdf`
  * `NexusGear_P2_Technical_Specification.docx` / `NexusGear_P2_Technical_Specification.pdf`
* **P3 (Final Delivery & Comprehensive Evaluation)**: [`Presentations/P3/`](Presentations/P3/)
  * `NexusGear_P3_Presentation_Final.pptx` / `NexusGear_P3_Presentation_Final.pdf`
  * `NexusGear_P3_Final_Report.docx` / `NexusGear_P3_Final_Report.pdf`
