const statusMessage = document.getElementById("statusMessage");
const changedCount = document.getElementById("changedCount");
const mainVersion = document.getElementById("mainVersion");
const versionGrid = document.getElementById("versionGrid");
const deleteToggle = document.getElementById("deleteToggle");
const addItemForm = document.getElementById("addItemForm");
const previewButton = document.getElementById("previewButton");
const exportButton = document.getElementById("exportButton");
const previewDialog = document.getElementById("previewDialog");
const iniPreview = document.getElementById("iniPreview");
const themeToggle = document.getElementById("themeToggle");

const saveTimers = new Map();
let draggedRow = null;

function applyTheme(theme) {
  const isDark = theme === "dark";
  document.body.classList.toggle("dark-mode", isDark);
  themeToggle.textContent = isDark ? "Modo claro" : "Modo escuro";
  themeToggle.setAttribute("aria-pressed", isDark.toString());
}

applyTheme(localStorage.getItem("versionador-theme") || "light");

themeToggle.addEventListener("click", () => {
  const nextTheme = document.body.classList.contains("dark-mode") ? "light" : "dark";
  localStorage.setItem("versionador-theme", nextTheme);
  applyTheme(nextTheme);
});

function setStatus(message, isError = false) {
  statusMessage.textContent = message;
  statusMessage.classList.toggle("error", isError);
  if (!message) {
    return;
  }

  window.clearTimeout(setStatus.timer);
  setStatus.timer = window.setTimeout(() => {
    statusMessage.textContent = "";
  }, 2600);
}

async function sendJson(url, method, body) {
  const response = await fetch(url, {
    method,
    headers: { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (!response.ok) {
    let message = "Nao foi possivel concluir a acao.";
    try {
      const payload = await response.json();
      message = payload.message || message;
    } catch {
      message = await response.text() || message;
    }

    throw new Error(message);
  }

  return response.headers.get("content-type")?.includes("application/json")
    ? response.json()
    : response.text();
}

function refreshChangedCount() {
  changedCount.textContent = document.querySelectorAll(".generation-input:checked").length.toString();
}

function markChanged(row, item) {
  const shouldGenerate = item.isChanged ?? item.IsChanged;
  row.classList.toggle("changed", shouldGenerate);
  row.dataset.categoryId = item.categoryId ?? item.CategoryId ?? row.dataset.categoryId;
  const generationInput = row.querySelector(".generation-input");
  if (generationInput) {
    generationInput.checked = shouldGenerate;
  }
  refreshChangedCount();

  if ((item.iniKey ?? item.IniKey) === "Gdoor") {
    mainVersion.textContent = item.currentVersion ?? item.CurrentVersion;
  }
}

function validateVersionInput(input) {
  const value = input.value.trim();
  const followsPattern = value === "" || /^\d+\.\d+\.\d+\.\d+$/.test(value);
  input.classList.toggle("invalid-version", !followsPattern);
}

function queueVersionSave(input, immediate = false) {
  const row = input.closest(".version-row");
  const id = row.dataset.id;
  validateVersionInput(input);

  window.clearTimeout(saveTimers.get(id));
  const save = async () => {
    try {
      setStatus("Salvando...");
      const item = await sendJson(`/api/items/${id}/version`, "POST", { version: input.value });
      input.value = item.currentVersion ?? item.CurrentVersion;
      markChanged(row, item);
      setStatus("Versao salva.");
    } catch (error) {
      setStatus(error.message, true);
    }
  };

  if (immediate) {
    save();
    return;
  }

  saveTimers.set(id, window.setTimeout(save, 650));
}

document.querySelectorAll(".version-input").forEach((input) => {
  validateVersionInput(input);
  input.addEventListener("input", () => queueVersionSave(input));
  input.addEventListener("change", () => queueVersionSave(input, true));
  input.addEventListener("blur", () => queueVersionSave(input, true));
});

document.querySelectorAll(".increment-button").forEach((button) => {
  button.addEventListener("click", async () => {
    const row = button.closest(".version-row");
    const input = row.querySelector(".version-input");
    try {
      setStatus("Incrementando...");
      const item = await sendJson(`/api/items/${row.dataset.id}/increment`, "POST", {
        amount: Number(button.dataset.amount)
      });
      input.value = item.currentVersion ?? item.CurrentVersion;
      validateVersionInput(input);
      markChanged(row, item);
      setStatus(`+${button.dataset.amount} aplicado.`);
    } catch (error) {
      setStatus(error.message, true);
    }
  });
});

document.querySelectorAll(".generation-input").forEach((input) => {
  input.addEventListener("change", async () => {
    const row = input.closest(".version-row");
    try {
      setStatus(input.checked ? "Marcando para o INI..." : "Removendo do INI...");
      const item = await sendJson(`/api/items/${row.dataset.id}/generation`, "POST", {
        isChanged: input.checked
      });
      markChanged(row, item);
      setStatus(input.checked ? "Vai sair no proximo INI." : "Nao vai sair no proximo INI.");
    } catch (error) {
      input.checked = !input.checked;
      refreshChangedCount();
      setStatus(error.message, true);
    }
  });
});

deleteToggle.addEventListener("change", () => {
  document.body.classList.toggle("delete-enabled", deleteToggle.checked);
});

document.querySelectorAll(".delete-button").forEach((button) => {
  button.addEventListener("click", async () => {
    if (!deleteToggle.checked) {
      return;
    }

    const row = button.closest(".version-row");
    const name = row.querySelector(".item-name strong").textContent;
    if (!window.confirm(`Excluir ${name}?`)) {
      return;
    }

    try {
      await sendJson(`/api/items/${row.dataset.id}`, "DELETE");
      row.remove();
      refreshChangedCount();
      setStatus("Item excluido.");
    } catch (error) {
      setStatus(error.message, true);
    }
  });
});

addItemForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(addItemForm);
  try {
    await sendJson("/api/items", "POST", {
      displayName: formData.get("displayName"),
      iniKey: formData.get("iniKey"),
      categoryId: Number(formData.get("categoryId")),
      currentVersion: formData.get("currentVersion")
    });
    setStatus("Item adicionado.");
    window.location.reload();
  } catch (error) {
    setStatus(error.message, true);
  }
});

previewButton.addEventListener("click", async () => {
  try {
    iniPreview.textContent = await fetch("/api/preview").then((response) => response.text());
    if (previewDialog.showModal) {
      previewDialog.showModal();
    } else {
      window.alert(iniPreview.textContent);
    }
  } catch (error) {
    setStatus(error.message, true);
  }
});

exportButton.addEventListener("click", async () => {
  try {
    setStatus("Gerando arquivo...");
    const response = await fetch("/api/export", { method: "POST" });
    if (!response.ok) {
      throw new Error("Nao foi possivel gerar o arquivo.");
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "versoes.ini";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);

    document.querySelectorAll(".version-row.changed").forEach((row) => row.classList.remove("changed"));
    document.querySelectorAll(".generation-input:checked").forEach((input) => {
      input.checked = false;
    });
    refreshChangedCount();
    setStatus("Arquivo gerado. Marcadores limpos.");
  } catch (error) {
    setStatus(error.message, true);
  }
});

function getRowAfterPointer(list, y) {
  const rows = [...list.querySelectorAll(".version-row:not(.dragging)")];
  return rows.reduce((closest, child) => {
    const box = child.getBoundingClientRect();
    const offset = y - box.top - box.height / 2;
    if (offset < 0 && offset > closest.offset) {
      return { offset, element: child };
    }
    return closest;
  }, { offset: Number.NEGATIVE_INFINITY, element: null }).element;
}

async function persistOrder() {
  const payload = [];
  document.querySelectorAll(".item-list").forEach((list) => {
    [...list.querySelectorAll(".version-row")].forEach((row, index) => {
      row.dataset.categoryId = list.dataset.categoryId;
      payload.push({
        id: Number(row.dataset.id),
        categoryId: Number(list.dataset.categoryId),
        sortOrder: index + 1
      });
    });
  });

  try {
    await sendJson("/api/reorder", "POST", payload);
    setStatus("Ordem salva.");
  } catch (error) {
    setStatus(error.message, true);
  }
}

versionGrid.addEventListener("dragstart", (event) => {
  const row = event.target.closest(".version-row");
  if (!row) {
    return;
  }

  draggedRow = row;
  row.classList.add("dragging");
  event.dataTransfer.effectAllowed = "move";
});

versionGrid.addEventListener("dragover", (event) => {
  if (!draggedRow) {
    return;
  }

  const list = event.target.closest(".item-list");
  if (!list) {
    return;
  }

  event.preventDefault();
  const nextRow = getRowAfterPointer(list, event.clientY);
  if (nextRow) {
    list.insertBefore(draggedRow, nextRow);
  } else {
    list.appendChild(draggedRow);
  }
});

versionGrid.addEventListener("drop", (event) => {
  if (!draggedRow) {
    return;
  }

  event.preventDefault();
  persistOrder();
});

versionGrid.addEventListener("dragend", () => {
  if (draggedRow) {
    draggedRow.classList.remove("dragging");
  }
  draggedRow = null;
});
