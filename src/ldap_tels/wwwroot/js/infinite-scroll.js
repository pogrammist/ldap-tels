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
            
            if (typeof data.rows === 'string' && data.rows.trim().length > 0) {
                this.renderRowsHtml(data.rows);
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
    
    renderRowsHtml(rowsHtml) {
        const temp = document.createElement('tbody');
        temp.innerHTML = rowsHtml;
        const rows = Array.from(temp.querySelectorAll('tr'));
        for (const row of rows) {
            const division = (row.getAttribute('data-division') || '').trim();
            const department = (row.getAttribute('data-department') || '').trim();

            if (division) {
                const { tbody } = this.getOrCreateDivisionTable(division);
                this.ensureDepartmentHeaderIfNeeded(tbody, division, department);
                tbody.appendChild(row);
            } else if (!division && department) {
                const tbody = this.getOrCreateDepartmentOnlyTable(department);
                tbody.appendChild(row);
            } else {
                const tbody = this.getOrCreateNoneTable();
                tbody.appendChild(row);
            }
        }
    }

    getOrCreateDivisionTable(division) {
        const tableId = `table-division-${encodeURIComponent(division)}`;
        let wrapper = this.container.querySelector(`div[data-division-wrapper="${division}"]`);
        if (!wrapper) {
            // Отступ между таблицами
            if (this.container.lastElementChild) {
                const spacer = document.createElement('div');
                spacer.className = 'mb-4';
                this.container.appendChild(spacer);
            }
            wrapper = document.createElement('div');
            wrapper.className = 'table-responsive table-card';
            wrapper.setAttribute('data-division-wrapper', division);
            const table = document.createElement('table');
            table.className = 'table table-striped table-hover';
            table.id = tableId;
            table.innerHTML = `
                <thead>
                    <tr>
                        <th colspan="4">
                            <a href="/Home/Division?division=${encodeURIComponent(division)}" class="fw-bold text-decoration-none">${division}</a>
                        </th>
                    </tr>
                </thead>
                <tbody data-last-department=""></tbody>
            `;
            wrapper.appendChild(table);
            this.container.appendChild(wrapper);
        }
        const tbody = wrapper.querySelector('tbody');
        return { wrapper, tbody };
    }

    ensureDepartmentHeaderIfNeeded(tbody, division, department) {
        if (!department) return;
        const lastDept = tbody.getAttribute('data-last-department') || '';
        if (lastDept !== department) {
            const headerRow = document.createElement('tr');
            headerRow.className = 'table-light';
            headerRow.innerHTML = `
                <td colspan="4">
                    <a href="/Home/Department?department=${encodeURIComponent(department)}" class="text-decoration-none">${department}</a>
                </td>
            `;
            tbody.appendChild(headerRow);
            tbody.setAttribute('data-last-department', department);
        }
    }

    getOrCreateDepartmentOnlyTable(department) {
        let wrapper = this.container.querySelector(`div[data-department-wrapper="${department}"]`);
        if (!wrapper) {
            if (this.container.lastElementChild) {
                const spacer = document.createElement('div');
                spacer.className = 'mb-4';
                this.container.appendChild(spacer);
            }
            wrapper = document.createElement('div');
            wrapper.className = 'table-responsive table-card';
            wrapper.setAttribute('data-department-wrapper', department);
            const table = document.createElement('table');
            table.className = 'table table-striped table-hover';
            table.innerHTML = `
                <thead>
                    <tr>
                        <th colspan="4">
                            <a href="/Home/Department?department=${encodeURIComponent(department)}" class="text-decoration-none">${department}</a>
                        </th>
                    </tr>
                </thead>
                <tbody></tbody>
            `;
            wrapper.appendChild(table);
            this.container.appendChild(wrapper);
        }
        return wrapper.querySelector('tbody');
    }

    getOrCreateNoneTable() {
        let wrapper = this.container.querySelector('div[data-none-wrapper="true"]');
        if (!wrapper) {
            if (this.container.lastElementChild) {
                const spacer = document.createElement('div');
                spacer.className = 'mb-4';
                this.container.appendChild(spacer);
            }
            wrapper = document.createElement('div');
            wrapper.className = 'table-responsive table-card';
            wrapper.setAttribute('data-none-wrapper', 'true');
            const table = document.createElement('table');
            table.className = 'table table-striped table-hover';
            table.innerHTML = `
                <thead>
                    <tr>
                        <th colspan="4">Другие</th>
                    </tr>
                </thead>
                <tbody></tbody>
            `;
            wrapper.appendChild(table);
            this.container.appendChild(wrapper);
        }
        return wrapper.querySelector('tbody');
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