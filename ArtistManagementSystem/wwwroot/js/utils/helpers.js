const Helpers = {
    formatDate(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleDateString();
    },

    formatDateTime(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleString();
    },

    showMessage(message, type = 'error') {
        const existingMessage = document.querySelector('.message-box');
        if (existingMessage) {
            existingMessage.remove();
        }

        const messageBox = document.createElement('div');
        messageBox.className = `message-box ${type}`;
        messageBox.textContent = message;
        
        document.body.insertBefore(messageBox, document.body.firstChild);
        
        setTimeout(() => {
            messageBox.remove();
        }, 5000);
    },

    showAlert(message) {
        alert(message);
    },

    showConfirm(message) {
        return confirm(message);
    },

    clearForm(formId) {
        document.getElementById(formId).reset();
    },

    toggleElement(elementId, show) {
        const element = document.getElementById(elementId);
        if (element) {
            element.classList.toggle('hidden', !show);
        }
    },

    setElementText(elementId, text) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = text;
        }
    }
};

window.Helpers = Helpers;
