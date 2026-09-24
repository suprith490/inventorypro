/* Reports page: sales, purchase and inventory valuation reports. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("reports.html", "Reports");

  if (Auth.isAdmin()) {
    document.getElementById("valuationSection").style.display = "block";
  }

  document.getElementById("rangeForm").addEventListener("submit", event => {
    event.preventDefault();
    load();
  });
  document.getElementById("clearRange").addEventListener("click", () => {
    document.getElementById("from").value = "";
    document.getElementById("to").value = "";
    load();
  });

  function rangeQuery() {
    return UI.queryString({
      from: document.getElementById("from").value,
      to: document.getElementById("to").value
    });
  }

  async function load() {
    const tasks = [loadSales(), loadPurchases()];
    if (Auth.isAdmin()) {
      tasks.push(loadValuation());
    }
    await Promise.all(tasks);
  }

  async function loadSales() {
    try {
      const response = await Api.get("/api/reports/sales" + rangeQuery());
      const report = response.data;

      document.getElementById("salesStats").innerHTML = [
        stat("Orders", report.totalOrders, ""),
        stat("Units sold", report.totalUnitsSold, ""),
        stat("Revenue", "$" + UI.formatCurrency(report.totalRevenue), ""),
        stat("Gross profit", "$" + UI.formatCurrency(report.grossProfit), "")
      ].join("");

      const daily = document.getElementById("dailySalesBody");
      daily.innerHTML = report.dailyBreakdown.length
        ? report.dailyBreakdown
            .map(
              day => `
              <tr>
                <td>${UI.formatDateOnly(day.date)}</td>
                <td class="text-right">${day.orderCount}</td>
                <td class="text-right">${day.unitsSold}</td>
                <td class="text-right">$${UI.formatCurrency(day.revenue)}</td>
              </tr>`
            )
            .join("")
        : UI.emptyRow(4, "No sales in this period.");

      const top = document.getElementById("topProductsBody");
      top.innerHTML = report.topProducts.length
        ? report.topProducts
            .map(
              product => `
              <tr>
                <td>${UI.escape(product.sku)}</td>
                <td>${UI.escape(product.name)}</td>
                <td class="text-right">${product.unitsSold}</td>
                <td class="text-right">$${UI.formatCurrency(product.revenue)}</td>
              </tr>`
            )
            .join("")
        : UI.emptyRow(4, "No products sold in this period.");
    } catch (error) {
      UI.error(error);
    }
  }

  async function loadPurchases() {
    try {
      const response = await Api.get("/api/reports/purchases" + rangeQuery());
      const report = response.data;

      document.getElementById("purchaseStats").innerHTML = [
        stat("Orders", report.totalOrders, ""),
        stat("Units received", report.totalUnitsReceived, ""),
        stat("Total spend", "$" + UI.formatCurrency(report.totalSpend), "")
      ].join("");

      const body = document.getElementById("supplierPurchaseBody");
      body.innerHTML = report.bySupplier.length
        ? report.bySupplier
            .map(
              supplier => `
              <tr>
                <td>${UI.escape(supplier.supplierName)}</td>
                <td class="text-right">${supplier.orderCount}</td>
                <td class="text-right">${supplier.unitsReceived}</td>
                <td class="text-right">$${UI.formatCurrency(supplier.totalSpend)}</td>
              </tr>`
            )
            .join("")
        : UI.emptyRow(4, "No purchases in this period.");
    } catch (error) {
      UI.error(error);
    }
  }

  async function loadValuation() {
    try {
      const response = await Api.get("/api/reports/inventory-valuation");
      const report = response.data;

      document.getElementById("valuationStats").innerHTML = [
        stat("Products", report.productCount, ""),
        stat("Total units", report.totalUnits, ""),
        stat("Cost value", "$" + UI.formatCurrency(report.totalCostValue), ""),
        stat("Retail value", "$" + UI.formatCurrency(report.totalRetailValue), "")
      ].join("");

      const body = document.getElementById("valuationBody");
      body.innerHTML = report.categories.length
        ? report.categories
            .map(
              category => `
              <tr>
                <td>${UI.escape(category.categoryName)}</td>
                <td class="text-right">${category.productCount}</td>
                <td class="text-right">${category.totalUnits}</td>
                <td class="text-right">$${UI.formatCurrency(category.costValue)}</td>
                <td class="text-right">$${UI.formatCurrency(category.retailValue)}</td>
              </tr>`
            )
            .join("")
        : UI.emptyRow(5, "No inventory data.");
    } catch (error) {
      UI.error(error);
    }
  }

  function stat(label, value, hint) {
    return `
      <div class="stat">
        <div class="label">${UI.escape(label)}</div>
        <div class="value">${UI.escape(String(value))}</div>
        ${hint ? `<div class="hint">${UI.escape(hint)}</div>` : ""}
      </div>`;
  }

  await load();
});
