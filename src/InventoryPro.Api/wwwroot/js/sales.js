/* Sales (Stock-Out) page: history + recording customer sales with line items. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("sales.html", "Sales / Stock Out");

  const state = { page: 1, pageSize: 10, search: "", from: "", to: "" };
  let products = [];

  document.getElementById("addSaleButton").addEventListener("click", openForm);
  document.getElementById("filterForm").addEventListener("submit", event => event.preventDefault());
  document.getElementById("search").addEventListener("input", debounce(applyFilters, 350));
  document.getElementById("from").addEventListener("change", applyFilters);
  document.getElementById("to").addEventListener("change", applyFilters);
  document.getElementById("clearFilters").addEventListener("click", () => {
    ["search", "from", "to"].forEach(id => (document.getElementById(id).value = ""));
    applyFilters();
  });

  function applyFilters() {
    state.page = 1;
    state.search = document.getElementById("search").value.trim();
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

  async function load() {
    try {
      const query = UI.queryString({
        page: state.page,
        pageSize: state.pageSize,
        search: state.search,
        from: state.from,
        to: state.to
      });
      const response = await Api.get("/api/sales" + query);
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(page) {
    const body = document.getElementById("salesBody");
    if (!page.items.length) {
      body.innerHTML = UI.emptyRow(8, "No sales found.");
    } else {
      body.innerHTML = page.items
        .map(
          sale => `
          <tr>
            <td class="nowrap">${UI.escape(sale.saleNumber)}</td>
            <td>${UI.escape(sale.customerName || "-")}</td>
            <td>${UI.escape(sale.userName)}</td>
            <td class="nowrap">${UI.formatDate(sale.saleDate)}</td>
            <td class="text-right">${sale.totalQuantity}</td>
            <td class="text-right">$${UI.formatCurrency(sale.totalAmount)}</td>
            <td>${UI.statusBadge(sale.status)}</td>
            <td class="text-right"><button class="btn-link" data-view="${sale.id}">View</button></td>
          </tr>`
        )
        .join("");

      body.querySelectorAll("button[data-view]").forEach(button => {
        button.addEventListener("click", async () => {
          try {
            const response = await Api.get(`/api/sales/${button.dataset.view}`);
            openDetail(response.data);
          } catch (error) {
            UI.error(error);
          }
        });
      });
    }

    UI.renderPagination(document.getElementById("pagination"), page, nextPage => {
      state.page = nextPage;
      load();
    });
  }

  function openDetail(sale) {
    const rows = sale.items
      .map(
        item => `
        <tr>
          <td>${UI.escape(item.sku)}</td>
          <td>${UI.escape(item.productName)}</td>
          <td class="text-right">${item.quantity}</td>
          <td class="text-right">$${UI.formatCurrency(item.unitPrice)}</td>
          <td class="text-right">$${UI.formatCurrency(item.lineTotal)}</td>
        </tr>`
      )
      .join("");

    const body = `
      <p class="muted">
        ${UI.escape(sale.saleNumber)} &middot; ${UI.escape(sale.customerName || "Walk-in customer")} &middot;
        ${UI.formatDate(sale.saleDate)} &middot; by ${UI.escape(sale.userName)}
      </p>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>SKU</th><th>Product</th><th class="text-right">Qty</th>
              <th class="text-right">Unit price</th><th class="text-right">Line total</th>
            </tr>
          </thead>
          <tbody>${rows}</tbody>
        </table>
      </div>
      <p class="text-right" style="margin-top:12px"><strong>Total: $${UI.formatCurrency(sale.totalAmount)}</strong></p>
      ${sale.notes ? `<p class="muted">Notes: ${UI.escape(sale.notes)}</p>` : ""}`;

    UI.showModal({ title: "Sale details", body, wide: true });
  }

  async function openForm() {
    products = await fetchAllProducts();
    if (!products.length) {
      UI.toast("Create a product first.", "warning");
      return;
    }

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field">
          <label>Customer name</label>
          <input name="customerName" maxlength="150" placeholder="Optional / walk-in" />
        </div>
        <div class="field">
          <label>Sale date</label>
          <input name="saleDate" type="date" />
        </div>
        <div class="field full">
          <label>Notes</label>
          <input name="notes" maxlength="500" placeholder="Optional remarks" />
        </div>
      </div>

      <h4 style="margin:16px 0 6px">Line items</h4>
      <table class="line-items">
        <thead>
          <tr>
            <th style="width:40%">Product</th>
            <th style="width:15%">Available</th>
            <th style="width:12%">Qty</th>
            <th style="width:18%">Unit price</th>
            <th style="width:15%" class="text-right">Line</th>
            <th style="width:5%"></th>
          </tr>
        </thead>
        <tbody id="lineItemsBody"></tbody>
      </table>
      <button type="button" class="btn secondary small" id="addLine" style="margin-top:8px">+ Add line</button>
      <p class="text-right" style="margin-top:12px">Grand total: <strong id="grandTotal">$0.00</strong></p>

      <div class="form-actions">
        <button type="button" class="btn secondary" data-cancel>Cancel</button>
        <button type="submit" class="btn">Record sale</button>
      </div>`;

    const modal = UI.showModal({ title: "Record sale", body: form, wide: true });
    const lineBody = form.querySelector("#lineItemsBody");
    const grandTotal = form.querySelector("#grandTotal");
    form.querySelector("[data-cancel]").addEventListener("click", modal.close);

    function recalc() {
      let total = 0;
      lineBody.querySelectorAll("tr").forEach(row => {
        const qty = Number(row.querySelector("[data-qty]").value) || 0;
        const price = Number(row.querySelector("[data-price]").value) || 0;
        const line = qty * price;
        row.querySelector("[data-line]").textContent = "$" + UI.formatCurrency(line);
        total += line;
      });
      grandTotal.textContent = "$" + UI.formatCurrency(total);
    }

    function addLine() {
      const row = document.createElement("tr");
      row.innerHTML = `
        <td>
          <select data-product required>
            ${products.map(p => `<option value="${p.id}">${UI.escape(p.sku)} - ${UI.escape(p.name)}</option>`).join("")}
          </select>
        </td>
        <td class="text-right" data-available>-</td>
        <td><input data-qty type="number" min="1" step="1" value="1" required /></td>
        <td><input data-price type="number" min="0" step="0.01" value="0" required /></td>
        <td class="text-right" data-line>$0.00</td>
        <td><button type="button" class="btn-link danger" data-remove>&times;</button></td>`;

      const productSelect = row.querySelector("[data-product]");
      const priceInput = row.querySelector("[data-price]");
      const availableCell = row.querySelector("[data-available]");

      function syncProduct() {
        const product = products.find(p => p.id === Number(productSelect.value));
        if (product) {
          priceInput.value = product.unitPrice;
          availableCell.textContent = product.quantityInStock;
        }
        recalc();
      }

      productSelect.addEventListener("change", syncProduct);
      row.querySelector("[data-qty]").addEventListener("input", recalc);
      priceInput.addEventListener("input", recalc);
      row.querySelector("[data-remove]").addEventListener("click", () => {
        if (lineBody.children.length > 1) {
          row.remove();
          recalc();
        } else {
          UI.toast("At least one line item is required.", "warning");
        }
      });

      lineBody.appendChild(row);
      syncProduct();
    }

    form.querySelector("#addLine").addEventListener("click", addLine);
    addLine();

    form.addEventListener("submit", async event => {
      event.preventDefault();
      const formData = new FormData(form);
      const items = [];
      lineBody.querySelectorAll("tr").forEach(row => {
        items.push({
          productId: Number(row.querySelector("[data-product]").value),
          quantity: Number(row.querySelector("[data-qty]").value),
          unitPrice: Number(row.querySelector("[data-price]").value)
        });
      });

      const payload = {
        customerName: String(formData.get("customerName") || "").trim() || null,
        saleDate: formData.get("saleDate") || null,
        notes: String(formData.get("notes") || "").trim() || null,
        items
      };

      if (!items.length) {
        UI.toast("Add at least one line item.", "error");
        return;
      }

      try {
        const response = await Api.post("/api/sales", payload);
        UI.toast(`Sale ${response.data.saleNumber} recorded.`, "success");
        modal.close();
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

  await load();
});
