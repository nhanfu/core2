import { GridView } from './gridView.js';
import { Utils } from './utils/utils.js';

export class CompareGridView extends GridView {
    constructor(containerId, options) {
        super(containerId, options);
        this.compareFields = options.compareFields || [];
        this.comparing = false;
    }

    init() {
        super.init();
        this.setupCompareMode();
    }

    setupCompareMode() {
        if (this.options.enableCompare) {
            this.addEventListener('onToolbarReady', (toolbar) => {
                const compareBtn = this.createCompareButton();
                toolbar.addItem(compareBtn);
            });
        }
    }

    createCompareButton() {
        return {
            id: 'btnCompare',
            text: Utils.translate('So sánh'),
            icon: 'fa fa-balance-scale',
            onClick: () => this.toggleCompare()
        };
    }

    toggleCompare() {
        this.comparing = !this.comparing;
        if (this.comparing) {
            this.enterCompareMode();
        } else {
            this.exitCompareMode();
        }
    }

    enterCompareMode() {
        this.originalRows = [...this.rows];
        this.rows.forEach((row, idx) => {
            row._compareIdx = idx;
        });
        this.comparing = true;
        this.render();
    }

    exitCompareMode() {
        this.comparing = false;
        this.render();
    }

    render() {
        super.render();
        if (this.comparing) {
            this.highlightCompareColumns();
        }
    }

    highlightCompareColumns() {
        if (!this.compareFields || !this.compareFields.length) return;
        
        this.compareFields.forEach(field => {
            const colIdx = this.columns.findIndex(c => c.field === field);
            if (colIdx >= 0) {
                // Highlight logic would be implemented here
            }
        });
    }
}