/**
 * Frontend configuration.
 *
 * The API and this static site are served from the same ASP.NET Core port,
 * so relative URLs ("/api/...") just work. If you ever host the frontend on a
 * different origin (for example Live Server on :5500), change API_BASE_URL to
 * the full API address, e.g. "http://localhost:5231".
 */
window.CONFIG = {
  API_BASE_URL:
    window.location.protocol === "http:" || window.location.protocol === "https:"
      ? window.location.origin
      : "http://localhost:5231",
  TOKEN_KEY: "inventorypro_token",
  USER_KEY: "inventorypro_user"
};
