/* Login page controller. */
document.addEventListener("DOMContentLoaded", () => {
  Auth.redirectIfAuthenticated();

  const params = new URLSearchParams(window.location.search);
  if (params.get("expired") === "1") {
    UI.toast("Your session expired. Please sign in again.", "warning");
  }
  if (params.get("registered") === "1") {
    UI.toast("Account created. Welcome!", "success");
  }

  const form = document.getElementById("loginForm");
  const button = document.getElementById("loginButton");

  form.addEventListener("submit", async event => {
    event.preventDefault();

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    if (!email || !password) {
      UI.toast("Email and password are required.", "error");
      return;
    }

    button.disabled = true;
    button.textContent = "Signing in...";

    try {
      await Auth.login(email, password);
      window.location.href = "dashboard.html";
    } catch (error) {
      UI.error(error);
      button.disabled = false;
      button.textContent = "Sign in";
    }
  });
});
