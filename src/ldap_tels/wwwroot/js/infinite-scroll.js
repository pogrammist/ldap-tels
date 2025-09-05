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
        
        this.currentPage = 1; // запрашиваем первую страницу на клиенте
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

        // Загрузим первую страницу сразу
        this.loadMore();
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
            
            if (typeof data.html === 'string' && data.html.trim().length > 0) {
                this.appendHtml(data.html);
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
    
    appendHtml(html) {
        const temp = document.createElement('div');
        temp.innerHTML = html;
        
        // 1) Сливаем продолжения существующих таблиц по подразделению
        const incomingDivisionWrappers = temp.querySelectorAll('div[data-division-wrapper]');
        incomingDivisionWrappers.forEach(incoming => {
            const division = incoming.getAttribute('data-division-wrapper');
            const existing = this.container.querySelector(`div[data-division-wrapper="${CSS.escape(division)}"] tbody`);
            const incomingTbody = incoming.querySelector('tbody');
            if (existing && incomingTbody) {
                while (incomingTbody.firstChild) existing.appendChild(incomingTbody.firstChild);
                incoming.remove();
            }
        });

        // 2) Сливаем продолжения таблиц отделов без подразделения
        const incomingDeptWrappers = temp.querySelectorAll('div[data-department-wrapper]');
        incomingDeptWrappers.forEach(incoming => {
            const department = incoming.getAttribute('data-department-wrapper');
            const existing = this.container.querySelector(`div[data-department-wrapper="${CSS.escape(department)}"] tbody`);
            const incomingTbody = incoming.querySelector('tbody');
            if (existing && incomingTbody) {
                while (incomingTbody.firstChild) existing.appendChild(incomingTbody.firstChild);
                incoming.remove();
            }
        });

        // 3) Секция "Другие" (без подразделения и отдела) — всегда одна
        const incomingNone = temp.querySelector('div[data-none-wrapper="true"] tbody');
        const existingNone = this.container.querySelector('div[data-none-wrapper="true"] tbody');
        if (incomingNone && existingNone) {
            while (incomingNone.firstChild) existingNone.appendChild(incomingNone.firstChild);
            // удалить пустую обертку из temp
            const parent = incomingNone.closest('div[data-none-wrapper="true"]');
            if (parent) parent.remove();
        }

        // Добавляем отступ только если ещё осталось что добавлять и контейнер уже не пустой
        if (temp.children.length > 0 && this.container.lastElementChild) {
            const spacer = document.createElement('div');
            spacer.className = 'mb-4';
            this.container.appendChild(spacer);
        }

        // Переносим оставшиеся новые блоки внутрь контейнера
        while (temp.firstChild) this.container.appendChild(temp.firstChild);
    }

    // Все сложные клиентские построения удалены — сервер отдаёт готовую разметку
    
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
                // Ничего не показываем, просто скрываем индикатор
                indicator.classList.add('d-none');
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