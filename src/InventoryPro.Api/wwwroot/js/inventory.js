/* Inventory page: low-stock alerts, transaction ledger and manual adjustments. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("inventory.html", "Inventory");

  const state = { page: 1, pageSize: 10, search: "", transactionType: "", from: "", to: "" };
  let products = [];

  document.getElementById("adjustButton").addEventListener("click", openAdjustForm);
  document.getElementById("filterForm").addEventListener("submit", event => event.preventDefault());
  document.getElementById("search").addEventListener("input", debounce(applyFilters, 350));
  document.getElementById("transactionType").addEventListener("change", applyFilters);
  document.getElementById("from").addEventListener("change", applyFilters);
  document.getElementById("to").addEventListener("change", applyFilters);
  document.getElementById("clearFilters").addEventListener("click", () => {
    ["search", "transactionType", "from", "to"].forEach(id => (document.getElementById(id).value = ""));
    applyFilters();
  });

  function applyFilters() {
    state.page = 1;
    state.search = document.getElementById("search").value.trim();
    state.transactionType = document.getElementById("transactionType").value;
    state.from = document.getElementById("from").value;
    state.to = document.getElementById("to").value;
    load();
  }

  async function fetchAllProducts() {
    const all = [];
    let page = 1;
    while (true) {
      const response = await Api.get(`/api/products?page=${page}&pageSize=100&sortBy=name`);
      all.push(...response.data.items);
      if (page >= response.data.totalPages || response.data.totalPages === 0) {
        break;
      }
      page++;
    }
    return all;
  }

  async function loadLowStock() {
    try {
      const response = await Api.get("/api/inventory/low-stock");
      const body = document.getElementById("lowStockBody");
      if (!response.data.length) {
        body.innerHTML = UI.emptyRow(7, "No low-stock products.");
        return;
      }
      body.innerHTML = response.data
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
    } catch (error) {
      UI.error(error);
    }
  }

  async function load() {
    try {
      const query = UI.queryString({
        page: state.page,
        pageSize: state.pageSize,
        search: state.search,
        transactionType: state.transactionType,
        from: state.from,
        to: state.to
      });
      const response = await Api.get("/api/inventory/transactions" + query);
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(page) {
    const body = document.getElementById("transactionsBody");
    if (!page.items.length) {
      body.innerHTML = UI.emptyRow(9, "No inventory transactions found.");
    } else {
      body.innerHTML = page.items
        .map(transaction => {
          const sign = transaction.quantity >= 0 ? "+" : "";
          return `
            <tr>
              <td class="nowrap">${UI.formatDate(transaction.createdAt)}</td>
              <td>${UI.escape(transaction.sku)} - ${UI.escape(transaction.productName)}</td>
              <td>${UI.statusBadge(transaction.transactionType)}</td>
              <td>${UI.statusBadge(transaction.referenceType)}${transaction.referenceId ? " #" + transaction.referenceId : ""}</td>
              <td class="text-right">${transaction.quantityBefore}</td>
              <td class="text-right"><strong>${sign}${transaction.quantity}</strong></td>
              <td class="text-right">${transaction.quantityAfter}</td>
              <td>${UI.escape(transaction.userName || "-")}</td>
              <td>${UI.escape(transaction.notes || "-")}</td>
            </tr>`;
        })
        .join("");
    }

    UI.renderPagination(document.getElementById("pagination"), page, nextPage => {
      state.page = nextPage;
      load();
    });
  }

  async function openAdjustForm() {
    products = await fetchAllProducts();
    if (!products.length) {
      UI.toast("Create a product first.", "warning");
      return;
    }

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field full">
          <label>Product</label>
          <select name="productId" required>
            ${products.map(p => `<option value="${p.id}">${UI.escape(p.sku)} - ${UI.escape(p.name)} (stock ${p.quantityInStock})</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label>Current quantity</label>
          <input id="currentQty" value="${products[0].quantityInStock}" disabled />
        </div>
        <div class="field">
          <label>New quantity (absolute count)</label>
          <input name="newQuantity" type="number" min="0" step="1" value="${products[0].quantityInStock}" required />
        </div>
        <div class="field full">
          <label>Reason</label>
          <input name="reason" maxlength="500" placeholder="Stock count correction, damage, write-off..." required />
        </div>
      </div>
      <div class="form-actions">
        <button type="button" class="btn secondary" data-cancel>Cancel</button>
        <button type="submit" class="btn">Save adjustment</button>
      </div>`;

    const modal = UI.showModal({ title: "Manual stock adjustment", body: form });
    const productSelect = form.querySelector("select[name=productId]");
    const currentQty = form.querySelector("#currentQty");
    productSelect.addEventListener("change", () => {
      const product = products.find(p => p.id === Number(productSelect.value));
      if (product) {
        currentQty.value = product.quantityInStock;
        form.querySelector("input[name=newQuantity]").value = product.quantityInStock;
      }
    });
    form.querySelector("[data-cancel]").addEventListener("click", modal.close);

    form.addEventListener("submit", async event => {
      event.preventDefault();
      const formData = new FormData(form);
      const payload = {
        productId: Number(formData.get("productId")),
        newQuantity: Number(formData.get("newQuantity")),
        reason: String(formData.get("reason")).trim()
      };
      try {
        await Api.post("/api/inventory/adjust", payload);
        UI.toast("Stock adjusted.", "success");
        modal.close();
        loadLowStock();
        load();
      } catch (error) {
        UI.error(error);
      }
    });
  }

  function debounce(fn, delay) {
    let handle;
    return (...args) => {
      clearTimeout(handle);
      handle = setTimeout(() => fn(...args), delay);
    };
  }

  await loadLowStock();
  await load();
});
