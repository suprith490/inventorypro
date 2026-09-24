/* Profile page: shows the authenticated user's details from the API. */
document.addEventListener("DOMContentLoaded", async () => {
  if (!Auth.requireAuth()) {
    return;
  }

  Auth.renderLayout("profile.html", "Profile");
  document.getElementById("refreshButton").addEventListener("click", load);

  function row(label, value) {
    return `<dt>${UI.escape(label)}</dt><dd>${UI.escape(value)}</dd>`;
  }

  async function load() {
    try {
      const response = await Auth.refreshProfile();
      const user = response;

      document.getElementById("profileList").innerHTML = [
        row("Full name", user.fullName),
        row("Email", user.email),
        row("Role", user.role),
        row("Status", user.isActive ? "Active" : "Inactive"),
        row("Member since", UI.formatDate(user.createdAt)),
        row("Last login", UI.formatDate(user.lastLoginAt))
      ].join("");

      const token = Auth.getToken() || "";
      const parts = token.split(".");
      let expires = "Unknown";
      if (parts.length === 3) {
        try {
          const payload = JSON.parse(atob(parts[1]));
          expires = UI.formatDate(new Date(payload.exp * 1000).toISOString());
        } catch {
          expires = "Unknown";
        }
      }

      document.getElementById("sessionList").innerHTML = [
        row("User id", user.id),
        row("Token type", "JWT (Bearer)"),
        row("Token expires", expires),
        row("API base URL", window.CONFIG.API_BASE_URL)
      ].join("");
    } catch (error) {
      UI.error(error);
    }
  }

  await load();
});
