document.addEventListener("DOMContentLoaded", () => {
  const root = document.querySelector("[data-poll-url]");
  if (root) {
    const interval = Number(root.dataset.pollInterval || 4000);
    setInterval(async () => {
      try {
        const response = await fetch(root.dataset.pollUrl);
        if (!response.ok) return;
        root.innerHTML = await response.text();
      } catch {
        /* keep the last good snapshot while the API is down */
      }
    }, interval);
  }

  const catalog = document.getElementById("order-catalog");
  if (catalog) {
    const products = JSON.parse(catalog.dataset.products);
    const shelves = JSON.parse(catalog.dataset.shelves);
    const workers = JSON.parse(catalog.dataset.workers);
    const lines = document.getElementById("order-lines");

    const productOptions = () =>
      products.map(p => `<option value="${p.id}">${p.name}</option>`).join("");
    const workerOptions = () =>
      workers.map(w => `<option value="${w.id}">${w.fullName}</option>`).join("");

    const shelfOptions = (productId) => {
      const matching = shelves.filter(s => s.productId === Number(productId) && s.available > 0);
      if (matching.length === 0) return `<option value="">No available shelves</option>`;
      return matching
        .map(s => `<option value="${s.id}">${s.code} · avail ${s.available}</option>`)
        .join("");
    };

    const bindRow = (row) => {
      const product = row.querySelector(".js-product");
      const shelf = row.querySelector(".js-shelf");
      product.addEventListener("change", () => {
        shelf.innerHTML = shelfOptions(product.value);
      });
    };

    const addRow = () => {
      const index = lines.querySelectorAll(".line-row").length;
      const row = document.createElement("div");
      row.className = "line-row";
      row.innerHTML = `
        <select name="Lines[${index}].ProductId" class="form-select js-product" required>
          <option value="">Product</option>${productOptions()}
        </select>
        <select name="Lines[${index}].ShelfId" class="form-select js-shelf" required>
          <option value="">Shelf</option>
        </select>
        <input name="Lines[${index}].QtyCartons" class="form-control" type="number" min="1" value="1" required />
        <select name="Lines[${index}].AssignedWorkerId" class="form-select" required>
          <option value="">Worker</option>${workerOptions()}
        </select>
        <button type="button" class="btn btn-outline-secondary js-remove">Remove</button>`;
      lines.appendChild(row);
      bindRow(row);
      row.querySelector(".js-remove").addEventListener("click", () => row.remove());
    };

    lines.querySelectorAll(".line-row").forEach(bindRow);
    document.getElementById("add-line")?.addEventListener("click", addRow);
  }
});
