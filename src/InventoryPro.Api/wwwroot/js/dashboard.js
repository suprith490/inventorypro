/* Dashboard controller: renders the Admin or Staff overview. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  const user = Auth.getUser();
  Auth.renderLayout("dashboard.html", "Dashboard");
  document.getElementById("welcomeTitle").textContent = `Welcome back, ${user.fullName}`;

  document.getElementById("refreshButton").addEventListener("click", load);

  async function load() {
    try {
      // Admins get the richer business overview; Staff get the operational one.
      const path = Auth.isAdmin() ? "/api/dashboard/admin" : "/api/dashboard/staff";
      const response = await Api.get(path);
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(data) {
    document.getElementById("dashboardSubtitle").textContent = Auth.isAdmin()
      ? "Business overview across catalog, stock and today's activity."
      : "Operational overview of today's stock movement.";

    const cards = [];

    if (Auth.isAdmin()) {
      cards.push(
        statCard("Products", data.totalProducts, `${data.activeProducts} active`, "primary"),
        statCard("Inventory value (retail)", "$" + UI.formatCurrency(data.totalStockRetailValue), "$" + UI.formatCurrency(data.totalStockCostValue) + " at cost", "success"),
        statCard("Low stock", data.lowStockCount, `${data.outOfStockCount} out of stock`, data.lowStockCount > 0 ? "danger" : "primary"),
        statCard("Users / Suppliers", `${data.totalUsers} / ${data.totalSuppliers}`, `${data.totalCategories} categories`, "primary")
      );
    }

    cards.push(
      statCard("Today's sales", "$" + UI.formatCurrency(data.todaySalesAmount), `${data.todaySalesCount} order(s)`, "success"),
      statCard("Today's purchases", "$" + UI.formatCurrency(data.todayPurchaseAmount), `${data.todayPurchaseCount} order(s)`, "warning"),
      statCard("Products", data.totalProducts, Auth.isAdmin() ? "" : "in catalog", "primary"),
      statCard("Low stock", data.lowStockCount, `${data.outOfStockCount} out of stock`, data.lowStockCount > 0 ? "danger" : "primary")
    );

    if (Auth.isAdmin()) {
      cards.push(
        statCard("This month sales", "$" + UI.formatCurrency(data.monthSalesAmount), "", "success"),
        statCard("This month purchases", "$" + UI.formatCurrency(data.monthPurchaseAmount), "", "warning"),
        statCard("Total units on hand", data.totalUnits, "", "primary"),
        statCard("Inventory value (cost)", "$" + UI.formatCurrency(data.totalStockCostValue), "", "primary")
      );
    }

    document.getElementById("statCards").innerHTML = cards.join("");

    renderActivity("recentSalesBody", data.recentSales);
    renderActivity("recentPurchasesBody", data.recentPurchases);
    renderLowStock(data.lowStockProducts);
  }

  function statCard(label, value, hint, tone) {
    return `
      <div class="stat ${tone}">
        <div class="label">${UI.escape(label)}</div>
        <div class="value">${UI.escape(String(value))}</div>
        ${hint ? `<div class="hint">${UI.escape(hint)}</div>` : ""}
      </div>`;
  }

  function renderActivity(tbodyId, items) {
    const body = document.getElementById(tbodyId);
    if (!items || items.length === 0) {
      body.innerHTML = UI.emptyRow(5, "No activity yet.");
      return;
    }
    body.innerHTML = items
      .map(
        item => `
        <tr>
          <td>${UI.escape(item.number)}</td>
          <td>${UI.escape(item.partyName)}</td>
          <td class="text-right">${item.totalQuantity}</td>
          <td class="text-right">$${UI.formatCurrency(item.totalAmount)}</td>
          <td class="nowrap">${UI.formatDate(item.occurredAt)}</td>
        </tr>`
      )
      .join("");
  }

  function renderLowStock(items) {
    const body = document.getElementById("lowStockBody");
    if (!items || items.length === 0) {
      body.innerHTML = UI.emptyRow(7, "No low-stock products. Well done!");
      return;
    }
    body.innerHTML = items
      .map(
        product => `
        <tr>
          <td>${UI.escape(product.sku)}</td>
          <td>${UI.escape(product.name)}</td>
          <td>${UI.escape(product.categoryName)}</td>
          <td class="text-right">${product.quantityInStock}</td>
          <td class="text-right">${product.reorderLevel}</td>
          <td class="text-right">${product.suggestedReorderQuantity}</td>
          <td>${product.isOutOfStock
            ? '<span class="badge danger">Out of stock</span>'
            : '<span class="badge warning">Low</span>'}</td>
        </tr>`
      )
      .join("");
  }

  await load();
});
