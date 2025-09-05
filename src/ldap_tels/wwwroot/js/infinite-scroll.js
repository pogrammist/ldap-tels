class InfiniteList {
    constructor(containerSelector, options = {}) {
        this.container = document.querySelector(containerSelector);
        this.options = {
            pageSize: 50,
            threshold: 200,
            loadingText: 'Загрузка...',
            inputSelector: '#liveSearchInput',
            clearSelector: '#clearSearchBtn',
            ...options
        };

        this.currentPage = 1;
        this.isLoading = false;
        this.hasMore = true;

        const urlParams = new URLSearchParams(window.location.search);
        this.department = urlParams.get('department');
        this.division = urlParams.get('division');
        this.title = urlParams.get('title');
        this.query = urlParams.get('query');

        this.init();
    }

    init() {
        if (!this.container) return;

        window.addEventListener('scroll', this.handleScroll.bind(this));
        this.addLoadingIndicator();
        this.hookSearchControls();

        // Первичная загрузка
        this.reset({ replace: true });
    }

    hookSearchControls() {
        const input = document.querySelector(this.options.inputSelector);
        const clearBtn = document.querySelector(this.options.clearSelector);

        const debounce = (func, wait) => {
            let timeout; return (...args) => { clearTimeout(timeout); timeout = setTimeout(() => func.apply(this, args), wait); };
        };

        const toggleClear = () => {
            if (input && clearBtn) {
                const show = (input.value || '').length > 0;
                clearBtn.style.display = show ? '' : 'none';
                clearBtn.classList.toggle('ms-2', show);
            }
        };

        if (input) {
            input.addEventListener('input', debounce((e) => {
                this.query = e.target.value || '';
                this.reset({ replace: true });
                toggleClear();
            }, 350));
            input.addEventListener('input', toggleClear);
            window.addEventListener('DOMContentLoaded', toggleClear);
        }
        if (clearBtn && input) {
            clearBtn.addEventListener('click', (e) => {
                e.preventDefault();
                input.value = '';
                this.query = '';
                this.reset({ replace: true });
                toggleClear();
                input.focus();
            });
        }
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

    reset({ replace = false } = {}) {
        this.currentPage = 1;
        this.hasMore = true;
        if (replace) {
            this.container.innerHTML = '';
        }
        this.loadMore();
    }

    async loadMore() {
        if (this.isLoading) return;
        this.isLoading = true;
        this.showLoading();
        try {
            const response = await fetch(this.buildApiUrl());
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            if (typeof data.html === 'string' && data.html.trim().length > 0) {
                this.appendHtml(data.html);
                this.currentPage = data.currentPage + 1;
                this.hasMore = data.hasMore;
            } else {
                this.hasMore = false;
            }
            this.updateLoadingIndicator();
        } catch (err) {
            console.error('Ошибка при загрузке контактов:', err);
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
        if (this.department) url.searchParams.set('department', this.department);
        if (this.division) url.searchParams.set('division', this.division);
        if (this.title) url.searchParams.set('title', this.title);
        if ((this.query || '').length > 0) url.searchParams.set('query', this.query);
        return url.toString();
    }

    appendHtml(html) {
        const temp = document.createElement('div');
        temp.innerHTML = html;

        // Слияние с существующими разделами/отделами
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

        const incomingNone = temp.querySelector('div[data-none-wrapper="true"] tbody');
        const existingNone = this.container.querySelector('div[data-none-wrapper="true"] tbody');
        if (incomingNone && existingNone) {
            while (incomingNone.firstChild) existingNone.appendChild(incomingNone.firstChild);
            const parent = incomingNone.closest('div[data-none-wrapper="true"]');
            if (parent) parent.remove();
        }

        if (temp.children.length > 0 && this.container.lastElementChild) {
            const spacer = document.createElement('div');
            spacer.className = 'mb-4';
            this.container.appendChild(spacer);
        }
        while (temp.firstChild) this.container.appendChild(temp.firstChild);
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
        if (indicator) indicator.classList.remove('d-none');
    }
    hideLoading() {
        const indicator = document.getElementById('loading-indicator');
        if (indicator) indicator.classList.add('d-none');
    }
    updateLoadingIndicator() {
        const indicator = document.getElementById('loading-indicator');
        if (indicator && !this.hasMore) indicator.classList.add('d-none');
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

document.addEventListener('DOMContentLoaded', function() {
    const contactsContainer = document.querySelector('#contacts-container');
    if (contactsContainer) {
        new InfiniteList('#contacts-container', {
            pageSize: 50,
            threshold: 200
        });
    }
});


