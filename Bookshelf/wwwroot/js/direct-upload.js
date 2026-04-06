/**
 * Direct Upload
 *
 * Uploads files directly to a dedicated endpoint and stores the resulting
 * storage path in a hidden form field. The resource form then submits the
 * path reference rather than the file itself.
 *
 * Usage: Add `data-direct-upload="<url>"` to a container element.
 * The container must include:
 *   - [data-upload-input]   — file input for selecting files
 *   - [data-upload-path]    — hidden input that receives the storage path
 *   - [data-upload-preview] — element where the image preview is rendered
 *   - [data-upload-hint]    — hint label (hidden during/after upload)
 *   - [data-upload-error]   — error label (shown on failure)
 */
document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-direct-upload]").forEach(initContainer);
});

function initContainer(container) {
    const uploadUrl = container.dataset.directUpload;
    const fileInput = container.querySelector("[data-upload-input]");
    const pathInput = container.querySelector("[data-upload-path]");
    const preview = container.querySelector("[data-upload-preview]");
    const hint = container.querySelector("[data-upload-hint]");
    const error = container.querySelector("[data-upload-error]");

    if (!fileInput || !pathInput) return;

    fileInput.addEventListener("change", async () => {
        const file = fileInput.files[0];
        if (!file) return;

        clearError();
        showUploading();

        try {
            const path = await upload(uploadUrl, file, container);
            pathInput.value = path;
            showPreview(path);
        } catch (err) {
            showError(err.message || "Upload failed. Please try again.");
        } finally {
            // Reset the file input so the same file can be re-selected if needed.
            fileInput.value = "";
        }
    });

    function clearError() {
        if (error) {
            error.textContent = "";
            error.classList.add("hidden");
        }
    }

    function showError(message) {
        if (error) {
            error.textContent = message;
            error.classList.remove("hidden");
        }
        if (hint) hint.classList.remove("hidden");
        // Remove uploading state from preview
        const status = preview?.querySelector("[data-uploading-status]");
        if (status) status.remove();
    }

    function showUploading() {
        if (hint) hint.classList.add("hidden");

        if (preview) {
            // Show a loading indicator in the preview area
            preview.innerHTML = `
                <div data-uploading-status class="flex items-center gap-3 rounded-2xl bg-base-100 p-4 ring-1 ring-base-300/60">
                    <span class="loading loading-spinner loading-md"></span>
                    <span class="text-sm text-base-content/70">Uploading&hellip;</span>
                </div>`;
            preview.classList.remove("hidden");
        }
    }

    function showPreview(path) {
        if (!preview) return;

        // Build a resized preview URL through the images controller.
        const encodedPath = path
            .split("/")
            .filter(Boolean)
            .map(encodeURIComponent)
            .join("/");

        const src = `/images/${encodedPath}?w=64&h=96`;

        preview.innerHTML = `
            <div class="flex items-center gap-4 rounded-2xl bg-base-100 p-4 ring-1 ring-base-300/60">
                <img src="${src}" alt="Cover preview" class="h-24 w-16 rounded-lg object-cover shadow" />
                <div class="text-sm text-base-content/70">
                    <div class="font-semibold text-base-content">Cover uploaded</div>
                    <div>Select a new file to replace it.</div>
                </div>
            </div>`;
        preview.classList.remove("hidden");

        if (hint) hint.classList.add("hidden");
    }
}

async function upload(url, file, container) {
    const maxSize = 10 * 1024 * 1024; // 10 MB
    if (file.size > maxSize) {
        throw new Error("File size must not exceed 10 MB.");
    }

    if (!file.type.startsWith("image/")) {
        throw new Error("Only image files are accepted.");
    }

    // Grab the anti-forgery token from the enclosing form.
    const form = container.closest("form");
    const token = form?.querySelector('[name="__RequestVerificationToken"]')?.value;

    const body = new FormData();
    body.append("file", file);

    if (token) {
        body.append("__RequestVerificationToken", token);
    }

    const response = await fetch(url, { method: "POST", body });

    if (!response.ok) {
        let message = "Upload failed.";
        try {
            const json = await response.json();
            if (json.error) message = json.error;
        } catch {
            // Ignore parse errors.
        }
        throw new Error(message);
    }

    const json = await response.json();
    return json.path;
}
