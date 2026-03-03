(() => {
  const els = {
    userLabel: document.getElementById("userLabel"),
    username: document.getElementById("username"),
    password: document.getElementById("password"),
    btnLogin: document.getElementById("btnLogin"),
    btnLogout: document.getElementById("btnLogout"),
    authMsg: document.getElementById("authMsg"),

    productsGrid: document.getElementById("productsGrid"),
    btnReloadProducts: document.getElementById("btnReloadProducts"),

    cartCountBadge: document.getElementById("cartCountBadge"),
    cartItemsContainer: document.getElementById("cartItemsContainer"),
    cartTotal: document.getElementById("cartTotal"),
    btnCartClear: document.getElementById("btnCartClear"),
    btnCartCheckout: document.getElementById("btnCartCheckout"),

    checkoutShipping: document.getElementById("checkoutShipping"),
    checkoutPayment: document.getElementById("checkoutPayment"),
    checkoutSubtotalLabel: document.getElementById("checkoutSubtotalLabel"),
    btnConfirmCheckout: document.getElementById("btnConfirmCheckout"),

    orderMsg: document.getElementById("orderMsg"),
    orderSection: document.getElementById("orderSection"),
    orderSummaryEmpty: document.getElementById("orderSummaryEmpty"),
    orderSummaryContent: document.getElementById("orderSummaryContent"),
    summaryOrderId: document.getElementById("summaryOrderId"),
    summaryOrderDate: document.getElementById("summaryOrderDate"),
    summaryTotal: document.getElementById("summaryTotal"),
    summaryPayment: document.getElementById("summaryPayment"),
    invoiceItems: document.getElementById("invoiceItems"),
  };

  const state = {
    token: localStorage.getItem("authToken") || "",
    products: [],
    cart: new Map(),
  };

  function setMsg(el, text, type) {
    if (!el) return;
    el.className = `mt-2 small ${type === "error" ? "text-danger" : type === "ok" ? "text-success" : "text-muted"}`;
    el.textContent = text || "";
  }

  function base64UrlDecode(str) {
    const pad = "=".repeat((4 - (str.length % 4)) % 4);
    const b64 = (str + pad).replace(/-/g, "+").replace(/_/g, "/");
    const bytes = Uint8Array.from(atob(b64), c => c.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  }

  function getJwtPayload(token) {
    try {
      const parts = token.split(".");
      if (parts.length !== 3) return null;
      return JSON.parse(base64UrlDecode(parts[1]));
    } catch {
      return null;
    }
  }

  function updateUserLabel() {
    if (!els.userLabel) return;
    if (!state.token) {
      els.userLabel.textContent = "Đăng nhập";
      return;
    }
    const payload = getJwtPayload(state.token);
    const username = payload?.unique_name || payload?.sub || "User";
    els.userLabel.textContent = username;
  }

  async function apiFetch(path, options = {}) {
    const headers = { ...(options.headers || {}) };
    headers["Content-Type"] = headers["Content-Type"] || "application/json";
    if (state.token) headers["Authorization"] = `Bearer ${state.token}`;
    const res = await fetch(path, { ...options, headers });
    const contentType = res.headers.get("content-type") || "";
    const data = contentType.includes("application/json") ? await res.json() : await res.text();
    if (!res.ok) {
      const msg = typeof data === "string" ? data : (data?.message || JSON.stringify(data));
      throw new Error(msg || `HTTP ${res.status}`);
    }
    return data;
  }

  function money(v) {
    const n = Number(v);
    if (Number.isNaN(n)) return "0 ₫";
    return n.toLocaleString("vi-VN", { style: "currency", currency: "VND" });
  }

  function escapeHtml(s) {
    return String(s ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  function getProp(obj, pascal, camel) {
    if (obj == null) return undefined;
    const v = obj[pascal];
    if (v !== undefined && v !== null) return v;
    return obj[camel];
  }

  function renderProducts() {
    if (!els.productsGrid) return;
    const cards = state.products.map(p => {
      const productId = p.ProductId ?? 0;
      const name = p.Name ?? "";
      const description = p.Description ?? "";
      const price = p.Price;
      const stockNum = typeof p.Stock === "number" ? p.Stock : parseInt(p.Stock, 10);
      const stockValue = Number.isNaN(stockNum) ? 0 : stockNum;
      const isOutOfStock = stockValue === 0;
      const disabled = isOutOfStock ? "disabled" : "";
      const stockClass = isOutOfStock ? "bg-secondary" : "bg-success";
      const stockText = isOutOfStock ? "Hết hàng" : `Còn ${stockValue}`;

      return `
        <div class="col-sm-6 col-lg-4 col-xl-3">
          <div class="card product-card h-100 shadow-sm">
            <div class="card-body d-flex flex-column">
              <h6 class="card-title mb-2">${escapeHtml(name)}</h6>
              <p class="card-text small text-muted flex-grow-1 mb-3" style="min-height: 2.5rem;">${escapeHtml(description.slice(0, 80))}${description.length > 80 ? "…" : ""}</p>
              <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                <div>
                  <div class="price">${money(price)}</div>
                  <span class="badge ${stockClass} stock-badge">${stockText}</span>
                </div>
                <button class="btn btn-primary btn-sm btn-add-cart" data-add="${productId}" ${disabled}>
                  <i class="bi bi-cart-plus me-1"></i>Thêm vào giỏ hàng
                </button>
              </div>
            </div>
          </div>
        </div>`;
    }).join("");

    els.productsGrid.innerHTML = cards || `<div class="col-12 text-center text-muted py-5">Chưa có sản phẩm.</div>`;
  }

  function cartTotal() {
    let total = 0;
    for (const { product, quantity } of state.cart.values()) {
      total += Number(product.Price) * Number(quantity);
    }
    return total;
  }

  function renderCart() {
    const items = Array.from(state.cart.values());
    const count = items.reduce((acc, x) => acc + Number(x.quantity || 0), 0);

    if (els.cartCountBadge) {
      els.cartCountBadge.textContent = count;
      els.cartCountBadge.classList.toggle("d-none", count === 0);
    }

    if (!items.length) {
      if (els.cartItemsContainer) {
        els.cartItemsContainer.innerHTML = `<div class="text-center text-muted py-5">Giỏ hàng trống</div>`;
      }
      if (els.cartTotal) els.cartTotal.textContent = "0 ₫";
      return;
    }

    const html = items.map(({ product, quantity }) => {
      const qty = Number(quantity);
      const subtotal = Number(product.Price) * qty;
      return `
        <div class="cart-item" data-product-id="${product.ProductId}">
          <div class="d-flex justify-content-between align-items-start">
            <div class="me-2 flex-grow-1">
              <div class="fw-semibold small">${escapeHtml(product.Name)}</div>
              <div class="small text-muted">${money(product.Price)} / sp</div>
            </div>
            <button class="btn btn-link text-danger p-0 btn-sm" data-remove="${product.ProductId}" title="Xóa">
              <i class="bi bi-x-lg"></i>
            </button>
          </div>
          <div class="d-flex justify-content-between align-items-center mt-2">
            <div class="qty-control">
              <button type="button" data-dec="${product.ProductId}" ${qty <= 1 ? "disabled" : ""}><i class="bi bi-dash"></i></button>
              <span data-qty-val="${product.ProductId}">${qty}</span>
              <button type="button" data-inc="${product.ProductId}" ${qty >= (product.Stock || 999) ? "disabled" : ""}><i class="bi bi-plus"></i></button>
            </div>
            <span class="fw-semibold text-success">${money(subtotal)}</span>
          </div>
        </div>`;
    }).join("");

    if (els.cartItemsContainer) els.cartItemsContainer.innerHTML = html;
    if (els.cartTotal) els.cartTotal.textContent = money(cartTotal());
  }

  function normalizeProduct(p) {
    if (!p || typeof p !== "object") return null;
    const priceVal = getProp(p, "Price", "price");
    const stockVal = getProp(p, "Stock", "stock");
    return {
      ProductId: getProp(p, "ProductId", "productId") ?? 0,
      Name: getProp(p, "Name", "name") ?? "",
      Description: getProp(p, "Description", "description") ?? "",
      Price: typeof priceVal === "number" ? priceVal : (parseFloat(priceVal) || 0),
      Stock: typeof stockVal === "number" ? stockVal : (parseInt(stockVal, 10) || 0),
    };
  }

  async function loadProducts() {
    if (!els.productsGrid) return;
    els.productsGrid.innerHTML = `<div class="col-12 text-center text-muted py-5">Đang tải sản phẩm từ API...</div>`;
    try {
      const products = await apiFetch("/api/Products", { method: "GET" });
      const list = Array.isArray(products) ? products : [];
      state.products = list.map(normalizeProduct).filter(Boolean);
      renderProducts();
    } catch (e) {
      els.productsGrid.innerHTML = `<div class="col-12 text-center text-danger py-5">Lỗi: ${escapeHtml(e.message)}</div>`;
    }
  }

  async function login() {
    setMsg(els.authMsg, "Đang đăng nhập...", "info");
    const username = els.username?.value.trim();
    const password = els.password?.value;
    if (!username || !password) {
      setMsg(els.authMsg, "Vui lòng nhập username và password.", "error");
      return;
    }
    const data = await apiFetch("/api/Auth/login", {
      method: "POST",
      body: JSON.stringify({ Username: username, Password: password }),
    });
    const token = data?.Token || data?.token || "";
    if (!token) throw new Error("Không nhận được token.");
    state.token = token;
    localStorage.setItem("authToken", token);
    updateUserLabel();
    setMsg(els.authMsg, "Đăng nhập thành công.", "ok");
  }

  function logout() {
    state.token = "";
    localStorage.removeItem("authToken");
    updateUserLabel();
    setMsg(els.authMsg, "Đã đăng xuất.", "info");
  }

  async function placeOrder() {
    setMsg(els.orderMsg, "", "info");
    if (!state.token) {
      setMsg(els.orderMsg, "Vui lòng đăng nhập trước khi đặt hàng.", "error");
      return;
    }
    const items = Array.from(state.cart.values());
    if (!items.length) {
      setMsg(els.orderMsg, "Giỏ hàng trống.", "error");
      return;
    }

    const dto = {
      ShippingAddress: els.checkoutShipping?.value.trim() || "",
      PaymentMethod: els.checkoutPayment?.value || "vnpay",
      OrderItems: items.map(({ product, quantity }) => ({
        ProductId: product.ProductId,
        Quantity: Number(quantity),
        Price: Number(product.Price),
      })),
    };

    setMsg(els.orderMsg, "Đang xử lý qua OrderFacade...", "info");
    const result = await apiFetch("/api/Orders/checkout", { method: "POST", body: JSON.stringify(dto) });

    const order = result.Order || result.order || {};
    const orderId = order.OrderId ?? order.orderId ?? "?";
    const total = order.TotalPrice ?? order.totalPrice ?? 0;
    const orderDate = order.OrderDate ?? order.orderDate;
    const paymentMsg = result.PaymentMessage || result.paymentMessage || "VNPay (Adapter)";

    setMsg(els.orderMsg, "Đặt hàng thành công!", "ok");

    const productNames = new Map();
    for (const { product } of state.cart.values()) {
      productNames.set(product.ProductId, product.Name);
    }

    if (els.orderSection) els.orderSection.classList.remove("d-none");
    if (els.orderSummaryEmpty) els.orderSummaryEmpty.classList.add("d-none");
    if (els.orderSummaryContent) els.orderSummaryContent.classList.remove("d-none");

    if (els.summaryOrderId) els.summaryOrderId.textContent = "#" + orderId;
    if (els.summaryOrderDate) {
      els.summaryOrderDate.textContent = orderDate
        ? new Date(orderDate).toLocaleString("vi-VN")
        : new Date().toLocaleString("vi-VN");
    }
    if (els.summaryTotal) els.summaryTotal.textContent = money(total);
    if (els.summaryPayment) els.summaryPayment.textContent = paymentMsg;

    if (els.invoiceItems && order.OrderItems) {
      const itemsHtml = (order.OrderItems || []).map(oi => {
        const pid = oi.ProductId ?? oi.productId;
        const name = productNames.get(pid) || `Sản phẩm #${pid}`;
        const qty = oi.Quantity ?? oi.quantity ?? 0;
        const price = oi.Price ?? oi.price ?? 0;
        const sub = Number(qty) * Number(price);
        return `<div class="d-flex justify-content-between py-1"><span>${escapeHtml(name)} x ${qty}</span><span>${money(sub)}</span></div>`;
      }).join("");
      els.invoiceItems.innerHTML = itemsHtml || "<div class='text-muted'>—</div>";
    }

    state.cart.clear();
    renderCart();
    await loadProducts();

    const modalEl = document.getElementById("checkoutModal");
    const offcanvasEl = document.getElementById("cartOffcanvas");
    const Bootstrap = window.bootstrap;
    if (Bootstrap) {
      const modal = Bootstrap.Modal.getInstance(modalEl);
      if (modal) modal.hide();
      const offcanvas = Bootstrap.Offcanvas.getInstance(offcanvasEl);
      if (offcanvas) offcanvas.hide();
    }
    els.orderSection?.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  // Events
  els.btnReloadProducts?.addEventListener("click", () => loadProducts());
  els.btnLogin?.addEventListener("click", () => login().catch(e => setMsg(els.authMsg, e.message, "error")));
  els.btnLogout?.addEventListener("click", () => logout());
  els.btnCartClear?.addEventListener("click", () => {
    state.cart.clear();
    renderCart();
  });

  els.btnCartCheckout?.addEventListener("click", () => {
    if (!state.cart.size) {
      setMsg(els.orderMsg, "Giỏ hàng trống.", "error");
      return;
    }
    if (els.checkoutSubtotalLabel) els.checkoutSubtotalLabel.textContent = money(cartTotal());
    const modalEl = document.getElementById("checkoutModal");
    if (modalEl && window.bootstrap) {
      new window.bootstrap.Modal(modalEl).show();
    }
  });

  els.btnConfirmCheckout?.addEventListener("click", () =>
    placeOrder().catch(e => setMsg(els.orderMsg, e.message, "error"))
  );

  els.productsGrid?.addEventListener("click", (ev) => {
    const btn = ev.target.closest("[data-add]");
    if (!btn) return;
    const id = Number(btn.getAttribute("data-add"));
    const p = state.products.find(x => Number(x.ProductId) === id);
    if (!p) return;
    const existing = state.cart.get(id);
    const nextQty = existing ? existing.quantity + 1 : 1;
    state.cart.set(id, { product: p, quantity: Math.min(nextQty, p.Stock || 999) });
    renderCart();
  });

  els.cartItemsContainer?.addEventListener("click", (ev) => {
    const remove = ev.target.closest("[data-remove]");
    if (remove) {
      const id = Number(remove.getAttribute("data-remove"));
      state.cart.delete(id);
      renderCart();
      return;
    }
    const inc = ev.target.closest("[data-inc]");
    if (inc) {
      const id = Number(inc.getAttribute("data-inc"));
      const entry = state.cart.get(id);
      if (!entry) return;
      const max = entry.product.Stock || 999;
      entry.quantity = Math.min((entry.quantity || 0) + 1, max);
      state.cart.set(id, entry);
      renderCart();
      return;
    }
    const dec = ev.target.closest("[data-dec]");
    if (dec) {
      const id = Number(dec.getAttribute("data-dec"));
      const entry = state.cart.get(id);
      if (!entry) return;
      entry.quantity = Math.max(1, (entry.quantity || 1) - 1);
      state.cart.set(id, entry);
      renderCart();
    }
  });

  // Init
  updateUserLabel();
  loadProducts();
})();
