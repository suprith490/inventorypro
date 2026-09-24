/* Products page: list with search/filter/sort/pagination + admin CRUD. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("products.html", "Products");

  let state = {
    page: 1,
    pageSize: 10,
    search: "",
    categoryId: "",
    supplierId: "",
    sortBy: "name",
    sortDescending: false,
    lowStockOnly: false
  };

  let categories = [];
  let suppliers = [];

  if (Auth.isAdmin()) {
    document.getElementById("addProductButton").style.display = "inline-flex";
    document.getElementById("addProductButton").addEventListener("click", () => openForm(null));
  }

  document.getElementById("filterForm").addEventListener("submit", event => event.preventDefault());
  document.getElementById("search").addEventListener("input", debounce(() => applyFilters(), 350));
  document.getElementById("categoryFilter").addEventListener("change", applyFilters);
  document.getElementById("supplierFilter").addEventListener("change", applyFilters);
  document.getElementById("sortBy").addEventListener("change", applyFilters);
  document.getElementById("sortDir").addEventListener("change", applyFilters);
  document.getElementById("lowStockOnly").addEventListener("change", applyFilters);
  document.getElementById("clearFilters").addEventListener("click", () => {
    document.getElementById("search").value = "";
    document.getElementById("categoryFilter").value = "";
    document.getElementById("supplierFilter").value = "";
    document.getElementById("sortBy").value = "name";
    document.getElementById("sortDir").value = "asc";
    document.getElementById("lowStockOnly").checked = false;
    applyFilters();
  });

  function applyFilters() {
    state.page = 1;
    state.search = document.getElementById("search").value.trim();
    state.categoryId = document.getElementById("categoryFilter").value;
    state.supplierId = document.getElementById("supplierFilter").value;
    state.sortBy = document.getElementById("sortBy").value;
    state.sortDescending = document.getElementById("sortDir").value === "desc";
    state.lowStockOnly = document.getElementById("lowStockOnly").checked;
    load();
  }

  async function loadLookups() {
    const [categoryResponse, supplierResponse] = await Promise.all([
      Api.get("/api/categories"),
      Api.get("/api/suppliers")
    ]);
    categories = categoryResponse.data;
    suppliers = supplierResponse.data;

    document.getElementById("categoryFilter").innerHTML =
      '<option value="">All categories</option>' +
      categories.map(c => `<option value="${c.id}">${UI.escape(c.name)}</option>`).join("");
    document.getElementById("supplierFilter").innerHTML =
      '<option value="">All suppliers</option>' +
      suppliers.map(s => `<option value="${s.id}">${UI.escape(s.name)}</option>`).join("");
  }

  async function load() {
    try {
      const query = UI.queryString({
        page: state.page,
        pageSize: state.pageSize,
        search: state.search,
        categoryId: state.categoryId,
        supplierId: state.supplierId,
        lowStockOnly: state.lowStockOnly ? true : "",
        sortBy: state.sortBy,
        sortDescending: state.sortDescending ? true : ""
      });
      const response = await Api.get("/api/products" + query);
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(page) {
    const body = document.getElementById("productsBody");
    if (!page.items.length) {
      body.innerHTML = UI.emptyRow(9, "No products match your filters.");
    } else {
      body.innerHTML = page.items.map(productRow).join("");
      if (Auth.isAdmin()) {
        body.querySelectorAll("button[data-edit]").forEach(button => {
          button.addEventListener("click", () => {
            const product = page.items.find(p => p.id === Number(button.dataset.edit));
            openForm(product);
          });
        });
        body.querySelectorAll("button[data-delete]").forEach(button => {
          button.addEventListener("click", () => remove(Number(button.dataset.delete)));
        });
      }
    }

    UI.renderPagination(document.getElementById("pagination"), page, nextPage => {
      state.page = nextPage;
      load();
    });
  }

  function productRow(product) {
    const status = !product.isActive
      ? '<span class="badge neutral">Inactive</span>'
      : product.isLowStock
        ? '<span class="badge warning">Low stock</span>'
        : '<span class="badge success">OK</span>';

    const actions = Auth.isAdmin()
      ? `<button class="btn-link" data-edit="${product.id}">Edit</button>
         <button class="btn-link danger" data-delete="${product.id}">Delete</button>`
      : '<span class="muted">-</span>';

    return `
      <tr>
        <td class="nowrap">${UI.escape(product.sku)}</td>
        <td>${UI.escape(product.name)}</td>
        <td>${UI.escape(product.categoryName)}</td>
        <td>${UI.escape(product.supplierName || "-")}</td>
        <td class="text-right">$${UI.formatCurrency(product.unitPrice)}</td>
        <td class="text-right">$${UI.formatCurrency(product.costPrice)}</td>
        <td class="text-right">${product.quantityInStock}${product.isLowStock ? " (!)" : ""}</td>
        <td>${status}</td>
        <td class="text-right nowrap">${actions}</td>
      </tr>`;
  }

  function openForm(product) {
    const isEdit = Boolean(product);
    const value = product || {
      sku: "", name: "", description: "", categoryId: categories[0] ? categories[0].id : "",
      supplierId: "", unitPrice: 0, costPrice: 0, reorderLevel: 10, isActive: true
    };

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field">
          <label>SKU</label>
          <input name="sku" required maxlength="50" value="${UI.escape(value.sku)}" />
        </div>
        <div class="field">
          <label>Name</label>
          <input name="name" required maxlength="200" value="${UI.escape(value.name)}" />
        </div>
        <div class="field full">
          <label>Description</label>
          <textarea name="description" rows="2" maxlength="1000">${UI.escape(value.description || "")}</textarea>
        </div>
        <div class="field">
          <label>Category</label>
          <select name="categoryId" required>
            ${categories.map(c => `<option value="${c.id}" ${c.id === value.categoryId ? "selected" : ""}>${UI.escape(c.name)}</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label>Supplier</label>
          <select name="supplierId">
            <option value="">None</option>
            ${suppliers.map(s => `<option value="${s.id}" ${s.id === value.supplierId ? "selected" : ""}>${UI.escape(s.name)}</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label>Unit price</label>
          <input name="unitPrice" type="number" min="0" step="0.01" value="${value.unitPrice}" />
        </div>
        <div class="field">
          <label>Cost price</label>
          <input name="costPrice" type="number" min="0" step="0.01" value="${value.costPrice}" />
        </div>
        <div class="field">
          <label>Reorder level</label>
          <input name="reorderLevel" type="number" min="0" step="1" value="${value.reorderLevel}" />
        </div>
        <div class="field">
          <label>Active</label>
          <select name="isActive">
            <option value="true" ${value.isActive ? "selected" : ""}>Yes</option>
            <option value="false" ${value.isActive ? "" : "selected"}>No</option>
          </select>
        </div>
      </div>
      <div class="form-actions">
        <button type="button" class="btn secondary" data-cancel>Cancel</button>
        <button type="submit" class="btn">${isEdit ? "Save changes" : "Create product"}</button>
      </div>`;

    const modal = UI.showModal({
      title: isEdit ? `Edit ${product.name}` : "New product",
      body: form
    });

    form.querySelector("[data-cancel]").addEventListener("click", modal.close);
    form.addEventListener("submit", async event => {
      event.preventDefault();
      const formData = new FormData(form);
      const supplierValue = formData.get("supplierId");
      const payload = {
        sku: String(formData.get("sku")).trim(),
        name: String(formData.get("name")).trim(),
        description: String(formData.get("description") || "").trim() || null,
        categoryId: Number(formData.get("categoryId")),
        supplierId: supplierValue ? Number(supplierValue) : null,
        unitPrice: Number(formData.get("unitPrice")),
        costPrice: Number(formData.get("costPrice")),
        reorderLevel: Number(formData.get("reorderLevel"))
      };

      try {
        if (isEdit) {
          payload.isActive = formData.get("isActive") === "true";
          await Api.put(`/api/products/${product.id}`, payload);
          UI.toast("Product updated.", "success");
        } else {
          await Api.post("/api/products", payload);
          UI.toast("Product created.", "success");
        }
        modal.close();
        load();
      } catch (error) {
        UI.error(error);
      }
    });
  }

  async function remove(id) {
    if (!UI.confirm("Delete this product? It will be hidden from the catalog.")) {
      return;
    }
    try {
      await Api.delete(`/api/products/${id}`);
      UI.toast("Product deleted.", "success");
      load();
    } catch (error) {
      UI.error(error);
    }
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
