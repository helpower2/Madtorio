// Store file reference for chunking
let currentFileElement = null;

export function initFileForChunking(fileInputId) {
    console.log(`[ChunkedUpload] Attempting to initialize file with input id: ${fileInputId}`);
    const fileInput = document.getElementById(fileInputId);
    console.log(`[ChunkedUpload] File input element:`, fileInput);

    if (!fileInput) {
        console.error(`[ChunkedUpload] File input element not found with id: ${fileInputId}`);
        return false;
    }

    console.log(`[ChunkedUpload] File input files:`, fileInput.files);
    console.log(`[ChunkedUpload] File input files length:`, fileInput.files ? fileInput.files.length : 'null');

    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        currentFileElement = fileInput.files[0];
        console.log(`[ChunkedUpload] Initialized file for chunking: ${currentFileElement.name} (${currentFileElement.size} bytes)`);
        return true;
    }
    console.error('[ChunkedUpload] Failed to initialize file - no file selected or files array is empty');
    return false;
}

export async function readChunk(chunkIndex, chunkSize) {
    if (!currentFileElement) {
        throw new Error('No file initialized. Call initFileForChunking first.');
    }

    const start = chunkIndex * chunkSize;
    const end = Math.min(start + chunkSize, currentFileElement.size);
    const chunkSizeActual = end - start;

    console.log(`[ChunkedUpload] Reading chunk ${chunkIndex + 1}: bytes ${start}-${end} (${chunkSizeActual} bytes)`);

    try {
        const blob = currentFileElement.slice(start, end);
        const arrayBuffer = await blob.arrayBuffer();
        const uint8Array = new Uint8Array(arrayBuffer);

        console.log(`[ChunkedUpload] Chunk ${chunkIndex + 1} read successfully: ${uint8Array.length} bytes`);
        return uint8Array;
    } catch (error) {
        console.error(`[ChunkedUpload] Error reading chunk ${chunkIndex + 1}:`, error);
        throw error;
    }
}

export function clearFileReference() {
    currentFileElement = null;
    console.log('[ChunkedUpload] File reference cleared');
}
