/* Categories page (admin only): list + CRUD. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAdmin()) {
    return;
  }

  Auth.renderLayout("categories.html", "Categories");
  document.getElementById("addCategoryButton").addEventListener("click", () => openForm(null));

  async function load() {
    try {
      const response = await Api.get("/api/categories?includeInactive=true");
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(categories) {
    const body = document.getElementById("categoriesBody");
    if (!categories.length) {
      body.innerHTML = UI.emptyRow(5, "No categories yet.");
      return;
    }
    body.innerHTML = categories
      .map(
        category => `
        <tr>
          <td>${UI.escape(category.name)}</td>
          <td>${UI.escape(category.description || "-")}</td>
          <td>${category.isActive
            ? '<span class="badge success">Active</span>'
            : '<span class="badge neutral">Inactive</span>'}</td>
          <td>${UI.formatDateOnly(category.createdAt)}</td>
          <td class="text-right nowrap">
            <button class="btn-link" data-edit="${category.id}">Edit</button>
            <button class="btn-link danger" data-delete="${category.id}">Delete</button>
          </td>
        </tr>`
      )
      .join("");

    body.querySelectorAll("button[data-edit]").forEach(button => {
      button.addEventListener("click", () => {
        openForm(categories.find(c => c.id === Number(button.dataset.edit)));
      });
    });
    body.querySelectorAll("button[data-delete]").forEach(button => {
      button.addEventListener("click", () => remove(Number(button.dataset.delete)));
    });
  }

  function openForm(category) {
    const isEdit = Boolean(category);
    const value = category || { name: "", description: "", isActive: true };

    const form = document.createElement("form");
    form.innerHTML = `
      <div class="form-grid">
        <div class="field full">
          <label>Name</label>
          <input name="name" required maxlength="100" value="${UI.escape(value.name)}" />
        </div>
        <div class="field full">
          <label>Description</label>
          <textarea name="description" rows="3" maxlength="500">${UI.escape(value.description || "")}</textarea>
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
        <button type="submit" class="btn">${isEdit ? "Save changes" : "Create category"}</button>
      </div>`;

    const modal = UI.showModal({ title: isEdit ? "Edit category" : "New category", body: form });
    form.querySelector("[data-cancel]").addEventListener("click", modal.close);

    form.addEventListener("submit", async event => {
      event.preventDefault();
      const formData = new FormData(form);
      const payload = {
        name: String(formData.get("name")).trim(),
        description: String(formData.get("description") || "").trim() || null
      };
      if (isEdit) {
        payload.isActive = formData.get("isActive") === "true";
      }

      try {
        if (isEdit) {
          await Api.put(`/api/categories/${category.id}`, payload);
          UI.toast("Category updated.", "success");
        } else {
          await Api.post("/api/categories", payload);
          UI.toast("Category created.", "success");
        }
        modal.close();
        load();
      } catch (error) {
        UI.error(error);
      }
    });
  }

  async function remove(id) {
    if (!UI.confirm("Delete this category?")) {
      return;
    }
    try {
      await Api.delete(`/api/categories/${id}`);
      UI.toast("Category deleted.", "success");
      load();
    } catch (error) {
      UI.error(error);
    }
  }

  await load();
});
