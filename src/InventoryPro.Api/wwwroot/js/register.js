/* Register page controller. */
document.addEventListener("DOMContentLoaded", () => {
  Auth.redirectIfAuthenticated();

  const form = document.getElementById("registerForm");
  const button = document.getElementById("registerButton");

  form.addEventListener("submit", async event => {
    event.preventDefault();

    const fullName = document.getElementById("fullName").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;

    if (!fullName || !email || !password) {
      UI.toast("All fields are required.", "error");
      return;
    }
    if (password.length < 6) {
      UI.toast("Password must be at least 6 characters long.", "error");
      return;
    }
    if (password !== confirmPassword) {
      UI.toast("Passwords do not match.", "error");
      return;
    }

    button.disabled = true;
    button.textContent = "Creating account...";

    try {
      await Auth.register(fullName, email, password);
      window.location.href = "dashboard.html";
    } catch (error) {
      UI.error(error);
      button.disabled = false;
      button.textContent = "Create account";
    }
  });
});
