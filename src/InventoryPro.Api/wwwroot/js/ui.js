/**
 * Small UI helpers shared by every page: toasts, modal, formatting,
 * pagination rendering and safe HTML escaping.
 */
const UI = {
  toast(message, type = "success") {
    let container = document.querySelector(".toast-container");
    if (!container) {
      container = document.createElement("div");
      container.className = "toast-container";
      document.body.appendChild(container);
    }

    const toast = document.createElement("div");
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = "0";
      toast.style.transition = "opacity 0.3s ease";
      setTimeout(() => toast.remove(), 300);
    }, 3500);
  },

  error(error) {
    const message = error && error.message ? error.message : "Something went wrong.";
    if (error && error.errors && error.errors.length) {
      this.toast(`${message} (${error.errors[0]})`, "error");
    } else {
      this.toast(message, "error");
    }
  },

  formatCurrency(value) {
    const amount = Number(value || 0);
    return amount.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  },

  formatDate(value) {
    if (!value) {
      return "-";
    }
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return "-";
    }
    return date.toLocaleDateString() + " " + date.toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit"
    });
  },

  formatDateOnly(value) {
    if (!value) {
      return "-";
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "-" : date.toLocaleDateString();
  },

  escape(value) {
    if (value === null || value === undefined) {
      return "";
    }
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  },

  queryString(params) {
    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        search.append(key, value);
      }
    });
    const text = search.toString();
    return text ? `?${text}` : "";
  },

  showModal({ title, body, wide = false }) {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop";
    backdrop.innerHTML = `
      <div class="modal ${wide ? "wide" : ""}">
        <header>
          <h3>${title}</h3>
          <button class="close" aria-label="Close">&times;</button>
        </header>
        <div class="modal-body"></div>
      </div>`;

    const bodyEl = backdrop.querySelector(".modal-body");
    if (typeof body === "string") {
      bodyEl.innerHTML = body;
    } else {
      bodyEl.appendChild(body);
    }

    const close = () => backdrop.remove();
    backdrop.querySelector(".close").addEventListener("click", close);
    backdrop.addEventListener("click", event => {
      if (event.target === backdrop) {
        close();
      }
    });

    document.body.appendChild(backdrop);
    return { element: backdrop, body: bodyEl, close };
  },

  confirm(message) {
    return window.confirm(message);
  },

  statusBadge(status) {
    const map = {
      Completed: "success",
      Pending: "warning",
      Cancelled: "danger",
      StockIn: "success",
      StockOut: "danger",
      Adjustment: "info",
      Purchase: "info",
      Sale: "warning",
      ManualAdjustment: "neutral"
    };
    return `<span class="badge ${map[status] || "neutral"}">${this.escape(status)}</span>`;
  },

  /**
   * Renders a pagination bar below a table.
   * pageInfo = { page, pageSize, totalCount, totalPages, hasPrevious, hasNext }
   */
  renderPagination(container, pageInfo, onNavigate) {
    if (!container) {
      return;
    }

    const current = pageInfo.page || 1;
    const totalPages = pageInfo.totalPages || 0;
    const start = totalPages === 0 ? 0 : (current - 1) * pageInfo.pageSize + 1;
    const end = Math.min(current * pageInfo.pageSize, pageInfo.totalCount);

    if (totalPages <= 1) {
      container.innerHTML = `<span>Showing ${pageInfo.totalCount} record(s)</span>`;
      return;
    }

    const buttons = [];
    const from = Math.max(1, current - 2);
    const to = Math.min(totalPages, current + 2);

    buttons.push(
      `<button data-page="${current - 1}" ${current <= 1 ? "disabled" : ""}>Prev</button>`
    );
    for (let page = from; page <= to; page++) {
      buttons.push(
        `<button data-page="${page}" class="${page === current ? "active" : ""}">${page}</button>`
      );
    }
    buttons.push(
      `<button data-page="${current + 1}" ${current >= totalPages ? "disabled" : ""}>Next</button>`
    );

    container.innerHTML = `
      <span>Showing ${start}-${end} of ${pageInfo.totalCount}</span>
      <div class="pages">${buttons.join("")}</div>`;

    container.querySelectorAll("button[data-page]").forEach(button => {
      button.addEventListener("click", () => {
        const page = Number(button.dataset.page);
        if (page >= 1 && page <= totalPages && page !== current) {
          onNavigate(page);
        }
      });
    });
  },

  emptyRow(columnCount, message = "No records found.") {
    return `<tr><td class="empty-state" colspan="${columnCount}">${message}</td></tr>`;
  }
};
