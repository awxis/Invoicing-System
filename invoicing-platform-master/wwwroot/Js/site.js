function createBlobUrl(data, contentType) {
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    const url = URL.createObjectURL(blob);
    console.log(`createBlobUrl: Created Blob URL: ${url}`);
    return url;
}

function revokeBlobUrl(url) {
    URL.revokeObjectURL(url);
    console.log(`revokeBlobUrl: Revoked Blob URL: ${url}`);
}

function downloadFileFromStream(fileName, streamReference, contentType) {
    streamReference.arrayBuffer().then(buffer => {
        const blob = new Blob([buffer], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    });
}
window.ShowDeleteModal = () => {
    var modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        var modal = new bootstrap.Modal(modalEl);
        modal.show();
    } else {
        console.error("Modal element not found!");
    }
};

window.HideDeleteModal = () => {
    var modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        var modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) {
            modalInstance.hide();
        } else {
            console.error("Modal instance not found!");
        }
    }
}; 
window.createBarChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId).getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                label: 'Revenue',
                data: data.map(d => d.value),
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        }
    });
};

window.createPieChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId).getContext('2d');
    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: ['#4CAF50', '#FF9800']
            }]
        }
    });
};

