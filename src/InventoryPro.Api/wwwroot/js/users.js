/* Users page (admin only): change role and activate/deactivate accounts. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAdmin()) {
    return;
  }

  Auth.renderLayout("users.html", "Users");
  const currentUser = Auth.getUser();

  async function load() {
    try {
      const response = await Api.get("/api/users");
      render(response.data);
    } catch (error) {
      UI.error(error);
    }
  }

  function render(users) {
    const body = document.getElementById("usersBody");
    if (!users.length) {
      body.innerHTML = UI.emptyRow(7, "No users found.");
      return;
    }

    body.innerHTML = users
      .map(user => {
        const isSelf = user.id === currentUser.id;
        const nextRole = user.role === "Admin" ? "Staff" : "Admin";
        return `
          <tr>
            <td>${UI.escape(user.fullName)}${isSelf ? ' <span class="muted">(you)</span>' : ""}</td>
            <td>${UI.escape(user.email)}</td>
            <td><span class="badge ${user.role === "Admin" ? "info" : "neutral"}">${UI.escape(user.role)}</span></td>
            <td>${user.isActive
              ? '<span class="badge success">Active</span>'
              : '<span class="badge danger">Inactive</span>'}</td>
            <td>${UI.formatDateOnly(user.createdAt)}</td>
            <td>${UI.formatDate(user.lastLoginAt)}</td>
            <td class="text-right nowrap">
              <button class="btn-link" data-role="${user.id}" data-next="${nextRole}" ${isSelf ? "disabled" : ""}>
                Make ${nextRole}
              </button>
              <button class="btn-link ${user.isActive ? "danger" : ""}" data-status="${user.id}"
                      data-next="${user.isActive ? "false" : "true"}" ${isSelf ? "disabled" : ""}>
                ${user.isActive ? "Deactivate" : "Activate"}
              </button>
            </td>
          </tr>`;
      })
      .join("");

    body.querySelectorAll("button[data-role]").forEach(button => {
      button.addEventListener("click", () => updateRole(Number(button.dataset.role), button.dataset.next));
    });
    body.querySelectorAll("button[data-status]").forEach(button => {
      button.addEventListener("click", () => updateStatus(Number(button.dataset.status), button.dataset.next === "true"));
    });
  }

  async function updateRole(id, role) {
    try {
      await Api.put(`/api/users/${id}/role`, { role });
      UI.toast(`User role changed to ${role}.`, "success");
      load();
    } catch (error) {
      UI.error(error);
    }
  }

  async function updateStatus(id, isActive) {
    try {
      await Api.put(`/api/users/${id}/status`, { isActive });
      UI.toast(`User ${isActive ? "activated" : "deactivated"}.`, "success");
      load();
    } catch (error) {
      UI.error(error);
    }
  }

  await load();
});
