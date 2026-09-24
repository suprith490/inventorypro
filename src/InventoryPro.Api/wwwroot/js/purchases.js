/* Purchases (Stock-In) page: history + recording goods received with line items. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("purchases.html", "Purchases / Stock In");

  const state = { page: 1, pageSize: 10, search: "", supplierId: "", from: "", to: "" };
  let suppliers = [];
  let products = [];

  document.getElementById("addPurchaseButton").addEventListener("click", openForm);
  document.getElementById("filterForm").addEventListener("submit", event => event.preventDefault());
  document.getElementById("search").addEventListener("input", debounce(applyFilters, 350));
  document.getElementById("supplierFilter").addEventListener("change", applyFilters);
  document.getElementById("from").addEventListener("change", applyFilters);
  document.getElementById("to").addEventListener("change", applyFilters);
  document.getElementById("clearFilters").addEventListener("click", () => {
    ["search", "supplierFilter", "from", "to"].forEach(id => (document.getElementById(id).value = ""));
    applyFilters();
  });

  function applyFilters() {
    state.page = 1;
    state.search = document.getElementById("search").value.trim();
    state.supplierId = document.getElementById("supplierFilter").value;
    state.from = document.getElementById("from").value;
    state.to = document.getElementById("to").value;
    load();
  }

  async function loadLookups() {
    const [supplierResponse, productList] = await Promise.all([
      Api.get("/api/suppliers"),
      fetchAllProducts()
    ]);
    suppliers = supplierResponse.data;
    products = productList;
    document.getElementById("supplierFilter").innerHTML =
      '<option value="">All suppliers</option>' +
      suppliers.map(s => `<option value="${s.id}">${UI.escape(s.name)}</option>`).join("");
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
        supplierId: state.supplierId,
        from: state.from,
        to: state.to
      });
      const response = await Api.get("/api/purchases" + query);
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(page) {
    const body = document.getElementById("purchasesBody");
    if (!page.items.length) {
      body.innerHTML = UI.emptyRow(8, "No purchases found.");
    } else {
      body.innerHTML = page.items
        .map(
          purchase => `
          <tr>
            <td class="nowrap">${UI.escape(purchase.purchaseNumber)}</td>
            <td>${UI.escape(purchase.supplierName)}</td>
            <td>${UI.escape(purchase.userName)}</td>
            <td class="nowrap">${UI.formatDate(purchase.purchaseDate)}</td>
            <td class="text-right">${purchase.totalQuantity}</td>
            <td class="text-right">$${UI.formatCurrency(purchase.totalAmount)}</td>
            <td>${UI.statusBadge(purchase.status)}</td>
            <td class="text-right"><button class="btn-link" data-view="${purchase.id}">View</button></td>
          </tr>`
        )
        .join("");

      body.querySelectorAll("button[data-view]").forEach(button => {
        button.addEventListener("click", async () => {
          try {
            const response = await Api.get(`/api/purchases/${button.dataset.view}`);
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

  function openDetail(purchase) {
    const rows = purchase.items
      .map(
        item => `
        <tr>
          <td>${UI.escape(item.sku)}</td>
          <td>${UI.escape(item.productName)}</td>
          <td class="text-right">${item.quantity}</td>
          <td class="text-right">$${UI.formatCurrency(item.unitCost)}</td>
          <td class="text-right">$${UI.formatCurrency(item.lineTotal)}</td>
        </tr>`
      )
      .join("");

    const body = `
      <p class="muted">
        ${UI.escape(purchase.purchaseNumber)} &middot; ${UI.escape(purchase.supplierName)} &middot;
        ${UI.formatDate(purchase.purchaseDate)} &middot; by ${UI.escape(purchase.userName)}
      </p>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>SKU</th><th>Product</th><th class="text-right">Qty</th>
              <th class="text-right">Unit cost</th><th class="text-right">Line total</th>
            </tr>
          </thead>
          <tbody>${rows}</tbody>
        </table>
      </div>
      <p class="text-right" style="margin-top:12px"><strong>Total: $${UI.formatCurrency(purchase.totalAmount)}</strong></p>
      ${purchase.notes ? `<p class="muted">Notes: ${UI.escape(purchase.notes)}</p>` : ""}`;

    UI.showModal({ title: "Purchase details", body, wide: true });
  }

  function openForm() {
    if (!suppliers.length) {
      UI.toast("Create a supplier first.", "warning");
      return;
    }
    if (!products.length) {
      UI.toast("Create a product first.", "warning");
      return;
    }

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field">
          <label>Supplier</label>
          <select name="supplierId" required>
            ${suppliers.map(s => `<option value="${s.id}">${UI.escape(s.name)}</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label>Purchase date</label>
          <input name="purchaseDate" type="date" />
        </div>
        <div class="field full">
          <label>Notes</label>
          <input name="notes" maxlength="500" placeholder="Optional reference or remarks" />
        </div>
      </div>

      <h4 style="margin:16px 0 6px">Line items</h4>
      <table class="line-items">
        <thead>
          <tr>
            <th style="width:45%">Product</th>
            <th style="width:15%">Qty</th>
            <th style="width:20%">Unit cost</th>
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
        <button type="submit" class="btn">Record purchase</button>
      </div>`;

    const modal = UI.showModal({ title: "Record purchase", body: form, wide: true });
    const lineBody = form.querySelector("#lineItemsBody");
    const grandTotal = form.querySelector("#grandTotal");
    form.querySelector("[data-cancel]").addEventListener("click", modal.close);

    function recalc() {
      let total = 0;
      lineBody.querySelectorAll("tr").forEach(row => {
        const qty = Number(row.querySelector("[data-qty]").value) || 0;
        const cost = Number(row.querySelector("[data-cost]").value) || 0;
        const line = qty * cost;
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
            ${products.map(p => `<option value="${p.id}">${UI.escape(p.sku)} - ${UI.escape(p.name)} (stock ${p.quantityInStock})</option>`).join("")}
          </select>
        </td>
        <td><input data-qty type="number" min="1" step="1" value="1" required /></td>
        <td><input data-cost type="number" min="0" step="0.01" value="0" required /></td>
        <td class="text-right" data-line>$0.00</td>
        <td><button type="button" class="btn-link danger" data-remove>&times;</button></td>`;

      const productSelect = row.querySelector("[data-product]");
      const costInput = row.querySelector("[data-cost]");
      productSelect.addEventListener("change", () => {
        const product = products.find(p => p.id === Number(productSelect.value));
        if (product) {
          costInput.value = product.costPrice;
        }
        recalc();
      });
      row.querySelector("[data-qty]").addEventListener("input", recalc);
      costInput.addEventListener("input", recalc);
      row.querySelector("[data-remove]").addEventListener("click", () => {
        if (lineBody.children.length > 1) {
          row.remove();
          recalc();
        } else {
          UI.toast("At least one line item is required.", "warning");
        }
      });

      lineBody.appendChild(row);
      const product = products.find(p => p.id === Number(productSelect.value));
      if (product) {
        costInput.value = product.costPrice;
      }
      recalc();
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
          unitCost: Number(row.querySelector("[data-cost]").value)
        });
      });

      const payload = {
        supplierId: Number(formData.get("supplierId")),
        purchaseDate: formData.get("purchaseDate") || null,
        notes: String(formData.get("notes") || "").trim() || null,
        items
      };

      if (!items.length) {
        UI.toast("Add at least one line item.", "error");
        return;
      }

      try {
        const response = await Api.post("/api/purchases", payload);
        UI.toast(`Purchase ${response.data.purchaseNumber} recorded.`, "success");
        modal.close();
        await loadLookups();
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

  await loadLookups();
  await load();
});
