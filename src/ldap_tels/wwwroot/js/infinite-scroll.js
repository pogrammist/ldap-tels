class InfiniteScroll {
    constructor(containerSelector, options = {}) {
        this.container = document.querySelector(containerSelector);
        this.options = {
            pageSize: 50,
            threshold: 100, // пиксели до конца для начала загрузки
            loadingText: 'Загрузка...',
            noMoreText: 'Больше контактов нет',
            ...options
        };
        
        this.currentPage = 2; // первая страница уже отрендерена сервером
        this.isLoading = false;
        this.hasMore = true;
        this.department = null;
        this.division = null;
        this.title = null;
        
        this.init();
    }
    
    init() {
        if (!this.container) return;
        
        // Получаем параметры фильтрации из URL
        const urlParams = new URLSearchParams(window.location.search);
        this.department = urlParams.get('department');
        this.division = urlParams.get('division');
        this.title = urlParams.get('title');
        
        // Добавляем обработчик прокрутки
        window.addEventListener('scroll', this.handleScroll.bind(this));
        
        // Добавляем индикатор загрузки
        this.addLoadingIndicator();
    }
    
    handleScroll() {
        if (this.isLoading || !this.hasMore) return;
        
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight;
        
        if (scrollTop + windowHeight >= documentHeight - this.options.threshold) {
            this.loadMore();
        }
    }
    
    async loadMore() {
        if (this.isLoading) return;
        
        this.isLoading = true;
        this.showLoading();
        
        try {
            const response = await fetch(this.buildApiUrl());
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (typeof data.rows === 'string' && data.rows.trim().length > 0) {
                this.appendRowsHtml(data.rows);
                this.currentPage = data.currentPage + 1;
                this.hasMore = data.hasMore;
            } else {
                this.hasMore = false;
            }
            
            this.updateLoadingIndicator();
            
        } catch (error) {
            console.error('Ошибка при загрузке контактов:', error);
            this.showError('Ошибка при загрузке контактов');
        } finally {
            this.isLoading = false;
            this.hideLoading();
        }
    }
    
    buildApiUrl() {
        const url = new URL('/api/contacts', window.location.origin);
        url.searchParams.set('page', this.currentPage);
        url.searchParams.set('pageSize', this.options.pageSize);
        
        if (this.department) {
            url.searchParams.set('department', this.department);
        }
        if (this.division) {
            url.searchParams.set('division', this.division);
        }
        if (this.title) {
            url.searchParams.set('title', this.title);
        }
        
        return url.toString();
    }
    
    appendRowsHtml(rowsHtml) {
        // Находим последний видимый tbody последней таблицы внутри контейнера
        const tables = this.container.querySelectorAll('table');
        if (!tables || tables.length === 0) return;
        const lastTable = tables[tables.length - 1];
        const tableBody = lastTable.querySelector('tbody');
        if (!tableBody) return;
        const temp = document.createElement('tbody');
        temp.innerHTML = rowsHtml;
        // Переносим только <tr> внутрь текущего tbody
        while (temp.firstChild) {
            tableBody.appendChild(temp.firstChild);
        }
    }
    
    addLoadingIndicator() {
        const indicator = document.createElement('div');
        indicator.id = 'loading-indicator';
        indicator.className = 'text-center py-3 d-none';
        indicator.innerHTML = `
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">${this.options.loadingText}</span>
            </div>
            <div class="mt-2">${this.options.loadingText}</div>
        `;
        
        this.container.appendChild(indicator);
    }
    
    showLoading() {
        const indicator = document.getElementById('loading-indicator');
        if (indicator) {
            indicator.classList.remove('d-none');
        }
    }
    
    hideLoading() {
        const indicator = document.getElementById('loading-indicator');
        if (indicator) {
            indicator.classList.add('d-none');
        }
    }
    
    updateLoadingIndicator() {
        const indicator = document.getElementById('loading-indicator');
        if (indicator) {
            if (!this.hasMore) {
                indicator.innerHTML = `
                    <div class="text-muted">
                        <i class="bi bi-check-circle"></i>
                        ${this.options.noMoreText}
                    </div>
                `;
            }
        }
    }
    
    showError(message) {
        const indicator = document.getElementById('loading-indicator');
        if (indicator) {
            indicator.innerHTML = `
                <div class="text-danger">
                    <i class="bi bi-exclamation-triangle"></i>
                    ${message}
                </div>
            `;
        }
    }
}

// Инициализация бесконечной прокрутки при загрузке страницы
document.addEventListener('DOMContentLoaded', function() {
    const contactsContainer = document.querySelector('#contacts-container');
    if (contactsContainer) {
        new InfiniteScroll('#contacts-container', {
            pageSize: 50,
            threshold: 200
        });
    }
});