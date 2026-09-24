/**
 * Authentication + shared layout.
 *
 * Keeps the JWT and the current user in localStorage, exposes role helpers
 * and builds the sidebar/topbar so every page shares one navigation.
 */
const Auth = {
  /* ----------------------------- token/user ----------------------------- */
  getToken() {
    return localStorage.getItem(window.CONFIG.TOKEN_KEY);
  },

  setToken(token) {
    localStorage.setItem(window.CONFIG.TOKEN_KEY, token);
  },

  getUser() {
    try {
      return JSON.parse(localStorage.getItem(window.CONFIG.USER_KEY));
    } catch {
      return null;
    }
  },

  setUser(user) {
    localStorage.setItem(window.CONFIG.USER_KEY, JSON.stringify(user));
  },

  isAuthenticated() {
    return Boolean(this.getToken());
  },

  role() {
    const user = this.getUser();
    return user ? user.role : null;
  },

  isAdmin() {
    return this.role() === "Admin";
  },

  /* ------------------------------ actions ------------------------------- */
  async login(email, password) {
    const response = await Api.post("/api/auth/login", { email, password });
    this.setToken(response.data.token);
    this.setUser(response.data.user);
    return response.data;
  },

  async register(fullName, email, password) {
    const response = await Api.post("/api/auth/register", {
      fullName,
      email,
      password
    });
    this.setToken(response.data.token);
    this.setUser(response.data.user);
    return response.data;
  },

  logout() {
    localStorage.removeItem(window.CONFIG.TOKEN_KEY);
    localStorage.removeItem(window.CONFIG.USER_KEY);
  },

  async refreshProfile() {
    const response = await Api.get("/api/auth/me");
    this.setUser(response.data);
    return response.data;
  },

  /* ------------------------------- guards ------------------------------- */
  requireAuth() {
    if (!this.isAuthenticated()) {
      window.location.href = "login.html";
      return false;
    }
    return true;
  },

  requireAdmin() {
    if (!this.requireAuth()) {
      return false;
    }
    if (!this.isAdmin()) {
      window.location.href = "dashboard.html";
      return false;
    }
    return true;
  },

  redirectIfAuthenticated() {
    if (this.isAuthenticated()) {
      window.location.href = "dashboard.html";
    }
  },

  /* ------------------------------- layout ------------------------------- */
  navGroups() {
    return [
      {
        label: "Main",
        items: [
          { href: "dashboard.html", text: "Dashboard" },
          { href: "products.html", text: "Products" },
          { href: "inventory.html", text: "Inventory" }
        ]
      },
      {
        label: "Stock movement",
        items: [
          { href: "purchases.html", text: "Purchases / Stock In" },
          { href: "sales.html", text: "Sales / Stock Out" }
        ]
      },
      {
        label: "Administration",
        items: [
          { href: "categories.html", text: "Categories", adminOnly: true },
          { href: "suppliers.html", text: "Suppliers", adminOnly: true },
          { href: "users.html", text: "Users", adminOnly: true },
          { href: "reports.html", text: "Reports" }
        ]
      },
      {
        label: "Account",
        items: [{ href: "profile.html", text: "Profile" }]
      }
    ];
  },

  renderLayout(activePage, pageTitle) {
    const user = this.getUser() || { fullName: "User", role: "Staff" };
    const sidebar = document.getElementById("sidebar");
    const topbar = document.getElementById("topbar");

    if (sidebar) {
      const groups = this.navGroups()
        .map(group => {
          const links = group.items
            .filter(item => !item.adminOnly || this.isAdmin())
            .map(item => {
              const active = item.href === activePage ? "active" : "";
              return `<a class="${active}" href="${item.href}">${item.text}</a>`;
            })
            .join("");
          return `<div class="nav-group">${group.label}</div>${links}`;
        })
        .join("");

      sidebar.innerHTML = `
        <div class="brand">Inventory<span>Pro</span></div>
        <nav>${groups}</nav>
        <div class="sidebar-footer">v1.0 &middot; ASP.NET Core 8</div>`;
    }

    if (topbar) {
      const roleClass = user.role === "Admin" ? "info" : "neutral";
      topbar.innerHTML = `
        <div style="display:flex;align-items:center;gap:12px;">
          <button class="menu-toggle" id="menuToggle" aria-label="Toggle menu">&#9776;</button>
          <h1>${pageTitle}</h1>
        </div>
        <div class="user-info">
          <span class="role badge ${roleClass}">${user.role}</span>
          <span>${user.fullName}</span>
          <button class="btn secondary small" id="logoutButton">Logout</button>
        </div>`;

      document.getElementById("logoutButton").addEventListener("click", () => {
        this.logout();
        window.location.href = "login.html";
      });

      const menuToggle = document.getElementById("menuToggle");
      menuToggle.addEventListener("click", () => {
        document.getElementById("sidebar").classList.toggle("open");
      });
    }
  }
};
