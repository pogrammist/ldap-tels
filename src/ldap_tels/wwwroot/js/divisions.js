async function changeDivisionWeight(divisionId, delta) {
    try {
        const urlInput = document.getElementById('update-division-weight-url');
        if (!urlInput) {
            throw new Error('URL для обновления веса не найден');
        }
        const url = urlInput.value;

        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            throw new Error('Маркер верификации запроса не найден');
        }

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput.value
            },
            body: JSON.stringify({ id: divisionId, delta: delta })
        });

        if (!response.ok) {
            throw new Error('HTTP ' + response.status);
        }

        const result = await response.json();
        if (!result.success) {
            throw new Error(result.error || 'Ошибка при обновлении');
        }

        const weightElement = document.getElementById(`division-weight-${divisionId}`);
        if (!weightElement) return;

        weightElement.textContent = result.newWeight;

        const minusButton = weightElement.previousElementSibling;
        const plusButton = weightElement.nextElementSibling;
        if (minusButton) minusButton.disabled = result.newWeight <= 0;
        if (plusButton) plusButton.disabled = result.newWeight >= 100;
    } catch (error) {
        console.error('Ошибка обновления веса:', error);
    }
}
