/* Suppliers page (admin only): list + CRUD. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAdmin()) {
    return;
  }

  Auth.renderLayout("suppliers.html", "Suppliers");
  document.getElementById("addSupplierButton").addEventListener("click", () => openForm(null));

  async function load() {
    try {
      const response = await Api.get("/api/suppliers?includeInactive=true");
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(suppliers) {
    const body = document.getElementById("suppliersBody");
    if (!suppliers.length) {
      body.innerHTML = UI.emptyRow(6, "No suppliers yet.");
      return;
    }
    body.innerHTML = suppliers
      .map(
        supplier => `
        <tr>
          <td>${UI.escape(supplier.name)}</td>
          <td>${UI.escape(supplier.email || "-")}</td>
          <td>${UI.escape(supplier.phone || "-")}</td>
          <td>${UI.escape(supplier.address || "-")}</td>
          <td>${supplier.isActive
            ? '<span class="badge success">Active</span>'
            : '<span class="badge neutral">Inactive</span>'}</td>
          <td class="text-right nowrap">
            <button class="btn-link" data-edit="${supplier.id}">Edit</button>
            <button class="btn-link danger" data-delete="${supplier.id}">Delete</button>
          </td>
        </tr>`
      )
      .join("");

    body.querySelectorAll("button[data-edit]").forEach(button => {
      button.addEventListener("click", () => {
        openForm(suppliers.find(s => s.id === Number(button.dataset.edit)));
      });
    });
    body.querySelectorAll("button[data-delete]").forEach(button => {
      button.addEventListener("click", () => remove(Number(button.dataset.delete)));
    });
  }

  function openForm(supplier) {
    const isEdit = Boolean(supplier);
    const value = supplier || { name: "", email: "", phone: "", address: "", isActive: true };

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field full">
          <label>Name</label>
          <input name="name" required maxlength="150" value="${UI.escape(value.name)}" />
        </div>
        <div class="field">
          <label>Email</label>
          <input name="email" type="email" maxlength="256" value="${UI.escape(value.email || "")}" />
        </div>
        <div class="field">
          <label>Phone</label>
          <input name="phone" maxlength="30" value="${UI.escape(value.phone || "")}" />
        </div>
        <div class="field full">
          <label>Address</label>
          <input name="address" maxlength="300" value="${UI.escape(value.address || "")}" />
        </div>
        ${isEdit ? `
        <div class="field">
          <label>Active</label>
          <select name="isActive">
            <option value="true" ${value.isActive ? "selected" : ""}>Yes</option>
            <option value="false" ${value.isActive ? "" : "selected"}>No</option>
          </select>
        </div>` : ""}
      </div>
      <div class="form-actions">
        <button type="button" class="btn secondary" data-cancel>Cancel</button>
        <button type="submit" class="btn">${isEdit ? "Save changes" : "Create supplier"}</button>
      </div>`;

    const modal = UI.showModal({ title: isEdit ? "Edit supplier" : "New supplier", body: form });
    form.querySelector("[data-cancel]").addEventListener("click", modal.close);

    form.addEventListener("submit", async event => {
      event.preventDefault();
      const formData = new FormData(form);
      const payload = {
        name: String(formData.get("name")).trim(),
        email: String(formData.get("email") || "").trim() || null,
        phone: String(formData.get("phone") || "").trim() || null,
        address: String(formData.get("address") || "").trim() || null
      };
      if (isEdit) {
        payload.isActive = formData.get("isActive") === "true";
      }

      try {
        if (isEdit) {
          await Api.put(`/api/suppliers/${supplier.id}`, payload);
          UI.toast("Supplier updated.", "success");
        } else {
          await Api.post("/api/suppliers", payload);
          UI.toast("Supplier created.", "success");
        }
        modal.close();
        load();
      } catch (error) {
        UI.error(error);
      }
    });
  }

  async function remove(id) {
    if (!UI.confirm("Delete this supplier?")) {
      return;
    }
    try {
      await Api.delete(`/api/suppliers/${id}`);
      UI.toast("Supplier deleted.", "success");
      load();
    } catch (error) {
      UI.error(error);
    }
  }

  await load();
});
