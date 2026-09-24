/**
 * Thin wrapper around fetch() that:
 *   - prefixes the API base URL,
 *   - attaches the JWT bearer token,
 *   - unwraps errors into a typed ApiError,
 *   - redirects to login when the token has expired (HTTP 401).
 *
 * Every API method returns the standard envelope:
 *   { success, message, statusCode, data, errors }
 * so callers read the payload from response.data.
 */

class ApiError extends Error {
  constructor(message, status, errors) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors || [];
  }
}

const Api = {
  async request(method, path, body) {
    const headers = { "Content-Type": "application/json" };
    const token = Auth.getToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    let response;
    try {
      response = await fetch(window.CONFIG.API_BASE_URL + path, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body)
      });
    } catch (networkError) {
      throw new ApiError(
        "Cannot reach the API. Make sure the server is running.",
        0,
        []
      );
    }

    // 204 No Content has no body.
    if (response.status === 204) {
      return { success: true, message: "Request successful", data: null };
    }

    let payload = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    if (!response.ok) {
      // A 401 means the token is missing/expired: force a fresh login.
      if (response.status === 401 && Auth.isAuthenticated()) {
        Auth.logout();
        window.location.href = "login.html?expired=1";
      }

      throw new ApiError(
        (payload && payload.message) || `Request failed (${response.status})`,
        response.status,
        (payload && payload.errors) || []
      );
    }

    return payload || { success: true, message: "Request successful", data: null };
  },

  get(path) {
    return this.request("GET", path);
  },

  post(path, body) {
    return this.request("POST", path, body);
  },

  put(path, body) {
    return this.request("PUT", path, body);
  },

  delete(path) {
    return this.request("DELETE", path);
  }
};
